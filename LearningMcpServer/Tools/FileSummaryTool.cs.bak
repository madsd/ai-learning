using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LearningMcpServer.Tools;

/// <summary>
/// Summarizes text files with size and binary content validation.
/// </summary>
public class FileSummaryTool : ITool
{
    private const int MaxFileSizeBytes = 200 * 1024; // 200KB
    private const int TargetSummaryWords = 200;
    private readonly string _repositoryRoot;

    /// <summary>
    /// Initializes a new instance of the FileSummaryTool.
    /// </summary>
    /// <param name="repositoryRoot">The root directory of the repository to restrict file access</param>
    public FileSummaryTool(string repositoryRoot)
    {
        _repositoryRoot = Path.GetFullPath(repositoryRoot);
    }

    /// <inheritdoc/>
    public string Name => "file_summary";

    /// <inheritdoc/>
    public string Description => "Summarize the contents of a text file. Files must be under 200KB and contain text content only.";

    /// <inheritdoc/>
    public JsonElement InputSchema => JsonSerializer.SerializeToElement(new
    {
        type = "object",
        properties = new
        {
            filePath = new
            {
                type = "string",
                description = "Path to the file to summarize (relative to repository root)"
            }
        },
        required = new[] { "filePath" }
    });

    /// <summary>
    /// Represents a file summary result.
    /// </summary>
    public class FileSummaryResult
    {
        [JsonPropertyName("filePath")]
        public string FilePath { get; set; } = string.Empty;

        [JsonPropertyName("summary")]
        public string Summary { get; set; } = string.Empty;

        [JsonPropertyName("wordCount")]
        public int WordCount { get; set; }

        [JsonPropertyName("lineCount")]
        public int LineCount { get; set; }

        [JsonPropertyName("characterCount")]
        public int CharacterCount { get; set; }
    }

    /// <inheritdoc/>
    public async Task<object> ExecuteAsync(JsonElement arguments)
    {
        if (!arguments.TryGetProperty("filePath", out var filePathElement))
        {
            throw new ArgumentException("Missing required parameter 'filePath'");
        }

        var filePath = filePathElement.GetString();
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be empty");
        }

        // Normalize path and prevent path traversal
        var normalizedPath = NormalizePath(filePath);
        var fullPath = Path.Combine(_repositoryRoot, normalizedPath);
        
        // Ensure the resolved path is still under repository root
        var resolvedPath = Path.GetFullPath(fullPath);
        if (!resolvedPath.StartsWith(_repositoryRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Access denied: Path traversal attempt detected");
        }

        // Check if file exists
        if (!File.Exists(resolvedPath))
        {
            throw new FileNotFoundException($"File not found: {normalizedPath}");
        }

        // Check file size
        var fileInfo = new FileInfo(resolvedPath);
        if (fileInfo.Length > MaxFileSizeBytes)
        {
            throw new InvalidOperationException($"File too large: {fileInfo.Length} bytes exceeds {MaxFileSizeBytes} byte limit");
        }

        // Read and validate content
        var content = await File.ReadAllTextAsync(resolvedPath);
        
        // Check if content appears to be binary
        if (IsBinaryContent(content))
        {
            throw new InvalidOperationException("File appears to contain binary data and cannot be summarized");
        }

        // Generate summary
        var summary = GenerateSummary(content);
        var wordCount = CountWords(content);
        var lineCount = content.Split('\n').Length;

        return new FileSummaryResult
        {
            FilePath = normalizedPath,
            Summary = summary,
            WordCount = wordCount,
            LineCount = lineCount,
            CharacterCount = content.Length
        };
    }

    /// <summary>
    /// Normalizes a file path to prevent path traversal attacks.
    /// </summary>
    /// <param name="path">The input path</param>
    /// <returns>The normalized path</returns>
    private static string NormalizePath(string path)
    {
        // Remove any leading slashes or backslashes
        path = path.TrimStart('/', '\\');
        
        // Normalize separators
        path = path.Replace('\\', '/');
        
        // Remove any '..' components
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries)
                       .Where(part => part != ".." && part != ".")
                       .ToArray();
        
        return string.Join("/", parts);
    }

    /// <summary>
    /// Checks if content appears to be binary data.
    /// </summary>
    /// <param name="content">The content to check</param>
    /// <returns>True if content appears binary, false otherwise</returns>
    private static bool IsBinaryContent(string content)
    {
        // Simple heuristic: check for null bytes and high ratio of non-printable characters
        if (content.Contains('\0'))
            return true;

        var nonPrintableCount = content.Count(c => c < 32 && c != '\t' && c != '\n' && c != '\r');
        var ratio = (double)nonPrintableCount / content.Length;
        
        return ratio > 0.1; // If more than 10% are non-printable, consider it binary
    }

    /// <summary>
    /// Generates a summary of the text content, clamped to approximately 200 words.
    /// </summary>
    /// <param name="content">The content to summarize</param>
    /// <returns>A summary of the content</returns>
    private static string GenerateSummary(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return "File is empty or contains only whitespace.";
        }

        // Split into sentences (simple approach)
        var sentences = content.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
                              .Select(s => s.Trim())
                              .Where(s => !string.IsNullOrWhiteSpace(s))
                              .ToList();

        if (sentences.Count == 0)
        {
            // Fallback to words if no sentences found
            var words = content.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var wordCount = Math.Min(TargetSummaryWords, words.Length);
            return string.Join(" ", words.Take(wordCount)) + (words.Length > wordCount ? "..." : "");
        }

        // Build summary by adding sentences until we approach the word limit
        var summaryBuilder = new StringBuilder();
        var currentWordCount = 0;

        foreach (var sentence in sentences)
        {
            var sentenceWords = sentence.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (currentWordCount + sentenceWords.Length > TargetSummaryWords && summaryBuilder.Length > 0)
            {
                break;
            }

            summaryBuilder.Append(sentence.Trim());
            summaryBuilder.Append(". ");
            currentWordCount += sentenceWords.Length;
        }

        var result = summaryBuilder.ToString().Trim();
        return string.IsNullOrEmpty(result) ? "Content could not be summarized." : result;
    }

    /// <summary>
    /// Counts the number of words in the content.
    /// </summary>
    /// <param name="content">The content to count words in</param>
    /// <returns>The number of words</returns>
    private static int CountWords(string content)
    {
        return content.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }
}