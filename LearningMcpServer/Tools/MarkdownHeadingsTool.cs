using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace LearningMcpServer.Tools;

/// <summary>
/// Extracts markdown headings from text content.
/// </summary>
public class MarkdownHeadingsTool : ITool
{
    private readonly string _repositoryRoot;

    /// <summary>
    /// Initializes a new instance of the MarkdownHeadingsTool.
    /// </summary>
    /// <param name="repositoryRoot">The root directory of the repository to restrict file access</param>
    public MarkdownHeadingsTool(string repositoryRoot)
    {
        _repositoryRoot = Path.GetFullPath(repositoryRoot);
    }

    /// <inheritdoc/>
    public string Name => "markdown_headings";

    /// <inheritdoc/>
    public string Description => "Extract all markdown headings (levels 1-6) from a markdown file or text content. Returns heading text, level, and line number.";

    /// <inheritdoc/>
    public JsonElement InputSchema => JsonSerializer.SerializeToElement(new
    {
        type = "object",
        properties = new
        {
            filePath = new
            {
                type = "string",
                description = "Path to the markdown file to extract headings from (optional if content is provided)"
            },
            content = new
            {
                type = "string",
                description = "Direct markdown content to extract headings from (optional if filePath is provided)"
            }
        }
    });

    /// <summary>
    /// Represents a markdown heading.
    /// </summary>
    public class MarkdownHeading
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("level")]
        public int Level { get; set; }

        [JsonPropertyName("line")]
        public int Line { get; set; }
    }

    /// <inheritdoc/>
    public async Task<object> ExecuteAsync(JsonElement arguments)
    {
        string content;
        
        // Check if we have direct content or need to read from file
        if (arguments.TryGetProperty("content", out var contentElement) && !string.IsNullOrWhiteSpace(contentElement.GetString()))
        {
            content = contentElement.GetString()!;
        }
        else if (arguments.TryGetProperty("filePath", out var filePathElement) && !string.IsNullOrWhiteSpace(filePathElement.GetString()))
        {
            var filePath = filePathElement.GetString()!;
            
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

            content = await File.ReadAllTextAsync(resolvedPath);
        }
        else
        {
            throw new ArgumentException("Either 'content' or 'filePath' parameter must be provided");
        }

        return ExtractHeadings(content);
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
    /// Extracts markdown headings from content.
    /// </summary>
    /// <param name="content">The markdown content to process</param>
    /// <returns>Array of heading information</returns>
    private static MarkdownHeading[] ExtractHeadings(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return Array.Empty<MarkdownHeading>();
        }

        var headings = new List<MarkdownHeading>();
        var lines = content.Split('\n');
        
        // Regex pattern for ATX headings (# ## ### etc.)
        var atxPattern = new Regex(@"^(\#{1,6})\s+(.+)$", RegexOptions.Compiled);
        
        // Regex pattern for Setext headings (underlined with = or -)
        var setextPattern = new Regex(@"^[\=\-]+\s*$", RegexOptions.Compiled);

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd();
            var lineNumber = i + 1; // 1-based line numbers
            
            // Check for ATX headings (# ## ### etc.)
            var atxMatch = atxPattern.Match(line);
            if (atxMatch.Success)
            {
                var level = atxMatch.Groups[1].Value.Length;
                var text = atxMatch.Groups[2].Value.Trim();
                
                // Remove trailing #s if present
                text = text.TrimEnd('#').Trim();
                
                headings.Add(new MarkdownHeading
                {
                    Text = text,
                    Level = level,
                    Line = lineNumber
                });
            }
            // Check for Setext headings (underlined with = or -)
            else if (i > 0 && !string.IsNullOrWhiteSpace(line) && setextPattern.IsMatch(line))
            {
                var prevLine = lines[i - 1].Trim();
                if (!string.IsNullOrWhiteSpace(prevLine))
                {
                    // = indicates level 1, - indicates level 2
                    var level = line.Contains('=') ? 1 : 2;
                    
                    headings.Add(new MarkdownHeading
                    {
                        Text = prevLine,
                        Level = level,
                        Line = i // Previous line number (i is 0-based, so i+1-1 = i)
                    });
                }
            }
        }

        return headings.ToArray();
    }
}