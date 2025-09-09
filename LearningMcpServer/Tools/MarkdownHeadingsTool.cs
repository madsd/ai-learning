using System.ComponentModel;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace LearningMcpServer.Tools;

/// <summary>
/// Extracts markdown headings from files or text content.
/// </summary>
[McpServerToolType]
public class MarkdownHeadingsTool
{
    private readonly ILogger<MarkdownHeadingsTool> _logger;
    private readonly string _repositoryRoot;

    public MarkdownHeadingsTool(ILogger<MarkdownHeadingsTool> logger)
    {
        _logger = logger;
        _repositoryRoot = GetRepositoryRoot();
    }

    /// <summary>
    /// Extract markdown headings from a file.
    /// </summary>
    /// <param name="filePath">Path to the markdown file to extract headings from</param>
    /// <returns>List of headings with text, level, and line numbers</returns>
    [McpServerTool]
    [Description("Extract all markdown headings (levels 1-6) from a markdown file. Returns heading text, level, and line number.")]
    public List<HeadingInfo> ExtractHeadingsFromFile([Description("Path to the markdown file to extract headings from")] string filePath)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            _logger.LogInformation("Markdown headings tool called for file: {FilePath}", filePath);
            
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

            var content = File.ReadAllText(fullPath);
            var headings = ParseHeadingsFromText(content);

            var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("Markdown headings extraction completed in {ElapsedMs}ms for file: {FilePath}, found {HeadingCount} headings", elapsedMs, normalizedPath, headings.Count);

            return headings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in markdown headings tool for file: {FilePath}", filePath);
            throw;
        }
    }

    /// <summary>
    /// Extract markdown headings from text content.
    /// </summary>
    /// <param name="content">Direct markdown content to extract headings from</param>
    /// <returns>List of headings with text, level, and line numbers</returns>
    [McpServerTool]
    [Description("Extract all markdown headings (levels 1-6) from markdown text content. Returns heading text, level, and line number.")]
    public List<HeadingInfo> ExtractHeadingsFromContent([Description("Direct markdown content to extract headings from")] string content)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            _logger.LogInformation("Markdown headings tool called for direct content ({ContentLength} characters)", content?.Length ?? 0);
            
            if (string.IsNullOrEmpty(content))
            {
                return new List<HeadingInfo>();
            }

            var headings = ParseHeadingsFromText(content);

            var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("Markdown headings extraction completed in {ElapsedMs}ms for content, found {HeadingCount} headings", elapsedMs, headings.Count);

            return headings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in markdown headings tool for content");
            throw;
        }
    }

    private List<HeadingInfo> ParseHeadingsFromText(string content)
    {
        var headings = new List<HeadingInfo>();
        var lines = content.Split('\n');
        
        // Regex for ATX-style headings (# ## ### etc.)
        var atxRegex = new Regex(@"^(\#{1,6})\s+(.+)", RegexOptions.Compiled);
        
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd();
            var match = atxRegex.Match(line);
            
            if (match.Success)
            {
                var level = match.Groups[1].Value.Length;
                var text = match.Groups[2].Value.Trim();
                
                headings.Add(new HeadingInfo
                {
                    Text = text,
                    Level = level,
                    Line = i + 1 // 1-based line numbering
                });
            }
            // Check for Setext-style headings (underlined with = or -)
            else if (i + 1 < lines.Length)
            {
                var nextLine = lines[i + 1].Trim();
                if (!string.IsNullOrWhiteSpace(line) && 
                    (nextLine.All(c => c == '=') || nextLine.All(c => c == '-')) &&
                    nextLine.Length >= 3)
                {
                    var level = nextLine.All(c => c == '=') ? 1 : 2;
                    headings.Add(new HeadingInfo
                    {
                        Text = line.Trim(),
                        Level = level,
                        Line = i + 1 // 1-based line numbering
                    });
                }
            }
        }
        
        return headings;
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

    /// <summary>
    /// Information about a markdown heading.
    /// </summary>
    public class HeadingInfo
    {
        public string Text { get; set; } = string.Empty;
        public int Level { get; set; }
        public int Line { get; set; }
    }
}
