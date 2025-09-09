using System.ComponentModel;
using System.Text;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace LearningMcpServer.Tools;

/// <summary>
/// Summarizes text file contents with size and binary content validation.
/// </summary>
[McpServerToolType]
public class FileSummaryTool
{
    private readonly ILogger<FileSummaryTool> _logger;
    private readonly string _repositoryRoot;
    private const long MaxFileSizeBytes = 200 * 1024; // 200KB

    public FileSummaryTool(ILogger<FileSummaryTool> logger)
    {
        _logger = logger;
        _repositoryRoot = GetRepositoryRoot();
    }

    /// <summary>
    /// Summarize the contents of a text file.
    /// </summary>
    /// <param name="filePath">Path to the file to summarize (relative to repository root)</param>
    /// <returns>File summary with statistics</returns>
    [McpServerTool]
    [Description("Summarize the contents of a text file. Files must be under 200KB and contain text content only.")]
    public FileSummaryResult SummarizeFile([Description("Path to the file to summarize (relative to repository root)")] string filePath)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            _logger.LogInformation("File summary tool called for file: {FilePath}", filePath);
            
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path cannot be empty", nameof(filePath));
            }

            // Normalize and validate the file path to prevent path traversal
            var normalizedPath = NormalizePath(filePath);
            var fullPath = Path.Combine(_repositoryRoot, normalizedPath);
            
            // Ensure the file is within the repository root
            if (!IsPathWithinRepository(fullPath))
            {
                throw new UnauthorizedAccessException("Access denied: File path is outside the repository");
            }

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"File not found: {normalizedPath}");
            }

            // Check file size
            var fileInfo = new FileInfo(fullPath);
            if (fileInfo.Length > MaxFileSizeBytes)
            {
                throw new InvalidOperationException($"File is too large ({fileInfo.Length:N0} bytes). Maximum size is {MaxFileSizeBytes:N0} bytes (200KB)");
            }

            // Read and validate content
            var content = File.ReadAllText(fullPath, Encoding.UTF8);
            
            if (IsBinaryContent(content))
            {
                throw new InvalidOperationException("File appears to contain binary data. Only text files are supported");
            }

            // Generate summary
            var summary = GenerateSummary(content);
            var wordCount = CountWords(content);
            var lineCount = content.Split('\n').Length;

            var result = new FileSummaryResult
            {
                FilePath = normalizedPath,
                Summary = summary,
                WordCount = wordCount,
                LineCount = lineCount,
                CharacterCount = content.Length
            };

            var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("File summary completed in {ElapsedMs}ms for file: {FilePath}", elapsedMs, normalizedPath);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in file summary tool for file: {FilePath}", filePath);
            throw;
        }
    }

    private static string GetRepositoryRoot()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var dir = new DirectoryInfo(currentDir);
        
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, ".git")))
        {
            dir = dir.Parent;
        }
        
        return dir?.FullName ?? currentDir;
    }

    private string NormalizePath(string path)
    {
        // Remove leading slashes and normalize separators
        path = path.TrimStart('/', '\\');
        path = path.Replace('\\', '/');
        
        // Resolve relative path components and prevent traversal
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var normalizedParts = new List<string>();
        
        foreach (var part in parts)
        {
            if (part == "." || string.IsNullOrWhiteSpace(part))
            {
                continue;
            }
            if (part == "..")
            {
                if (normalizedParts.Count > 0)
                {
                    normalizedParts.RemoveAt(normalizedParts.Count - 1);
                }
            }
            else
            {
                normalizedParts.Add(part);
            }
        }
        
        return string.Join(Path.DirectorySeparatorChar, normalizedParts);
    }

    private bool IsPathWithinRepository(string fullPath)
    {
        try
        {
            var repositoryPath = Path.GetFullPath(_repositoryRoot);
            var resolvedPath = Path.GetFullPath(fullPath);
            return resolvedPath.StartsWith(repositoryPath, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsBinaryContent(string content)
    {
        // Simple heuristic: if content contains null bytes or has a high ratio of non-printable characters
        var sampleSize = Math.Min(8192, content.Length);
        var sample = content.AsSpan(0, sampleSize);
        
        var nonPrintableCount = 0;
        foreach (var c in sample)
        {
            if (c == '\0') return true; // Null byte indicates binary
            if (char.IsControl(c) && c != '\r' && c != '\n' && c != '\t')
            {
                nonPrintableCount++;
            }
        }
        
        // If more than 5% of characters are non-printable control characters, consider it binary
        return (double)nonPrintableCount / sampleSize > 0.05;
    }

    private static string GenerateSummary(string content)
    {
        // Simple extractive summary - take first few sentences and key information
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var sentences = new List<string>();
        var wordCount = 0;
        const int targetWords = 200;
        
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine)) continue;
            
            // Split by sentence-ending punctuation
            var lineSentences = trimmedLine.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var sentence in lineSentences)
            {
                var cleanSentence = sentence.Trim();
                if (string.IsNullOrEmpty(cleanSentence)) continue;
                
                var sentenceWords = cleanSentence.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                
                if (wordCount + sentenceWords <= targetWords)
                {
                    sentences.Add(cleanSentence + ".");
                    wordCount += sentenceWords;
                }
                else
                {
                    break;
                }
            }
            
            if (wordCount >= targetWords) break;
        }
        
        return sentences.Count > 0 ? string.Join(" ", sentences) : "File appears to be empty or contains no readable text.";
    }

    private static int CountWords(string content)
    {
        return content.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    /// <summary>
    /// Result of file summarization.
    /// </summary>
    public class FileSummaryResult
    {
        public string FilePath { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public int WordCount { get; set; }
        public int LineCount { get; set; }
        public int CharacterCount { get; set; }
    }
}
