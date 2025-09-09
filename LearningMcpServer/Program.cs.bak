using LearningMcpServer;
using LearningMcpServer.Tools;
using Microsoft.Extensions.Logging;

/// <summary>
/// Main entry point for the Learning MCP Server.
/// Provides stdio-based MCP (Model Context Protocol) server with tools for weather, file operations, and math.
/// </summary>
class Program
{
    /// <summary>
    /// Main entry point for the application.
    /// </summary>
    /// <param name="args">Command line arguments</param>
    static async Task Main(string[] args)
    {
        // Set up console cancellation handling for graceful shutdown
        using var cancellationTokenSource = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true; // Prevent immediate termination
            cancellationTokenSource.Cancel();
        };

        // Configure logging
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole(options =>
            {
                options.LogToStandardErrorThreshold = LogLevel.Information;
            })
            .SetMinimumLevel(LogLevel.Information);
            
            // Enable debug logging if requested
            if (args.Contains("--debug"))
            {
                builder.SetMinimumLevel(LogLevel.Debug);
            }
        });

        var logger = loggerFactory.CreateLogger<Program>();

        try
        {
            // Display startup banner
            await Console.Error.WriteLineAsync("========================================");
            await Console.Error.WriteLineAsync("    Learning MCP Server v1.0.0");
            await Console.Error.WriteLineAsync("    Model Context Protocol Server");
            await Console.Error.WriteLineAsync("========================================");
            await Console.Error.WriteLineAsync();

            // Get repository root (current directory or specified path)
            var repositoryRoot = Environment.CurrentDirectory;
            if (args.Length > 0 && !args[0].StartsWith("--"))
            {
                repositoryRoot = Path.GetFullPath(args[0]);
            }

            logger.LogInformation("Repository root: {RepositoryRoot}", repositoryRoot);

            // Initialize tool registry
            var toolRegistry = new ToolRegistry();
            
            // Register all tools
            RegisterTools(toolRegistry, repositoryRoot, logger);

            // Create and start the MCP server
            var serverLogger = loggerFactory.CreateLogger<McpServer>();
            var server = new McpServer(toolRegistry, serverLogger);
            
            // Handle graceful shutdown
            cancellationTokenSource.Token.Register(() => server.Stop());

            logger.LogInformation("Starting MCP server...");
            await server.StartAsync(cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Shutdown completed gracefully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fatal error occurred");
            Environment.ExitCode = 1;
        }
        finally
        {
            await Console.Error.WriteLineAsync("Learning MCP Server stopped.");
        }
    }

    /// <summary>
    /// Registers all available tools in the tool registry.
    /// </summary>
    /// <param name="toolRegistry">The tool registry to register tools with</param>
    /// <param name="repositoryRoot">The repository root directory for file operations</param>
    /// <param name="logger">Logger for registration messages</param>
    private static void RegisterTools(ToolRegistry toolRegistry, string repositoryRoot, ILogger logger)
    {
        logger.LogInformation("Registering tools...");

        // Register WeatherTool
        var weatherTool = new WeatherTool();
        toolRegistry.RegisterTool(weatherTool);
        logger.LogInformation("Registered tool: {ToolName}", weatherTool.Name);

        // Register FileSummaryTool
        var fileSummaryTool = new FileSummaryTool(repositoryRoot);
        toolRegistry.RegisterTool(fileSummaryTool);
        logger.LogInformation("Registered tool: {ToolName}", fileSummaryTool.Name);

        // Register MarkdownHeadingsTool
        var markdownTool = new MarkdownHeadingsTool(repositoryRoot);
        toolRegistry.RegisterTool(markdownTool);
        logger.LogInformation("Registered tool: {ToolName}", markdownTool.Name);

        // Register MathTool
        var mathTool = new MathTool();
        toolRegistry.RegisterTool(mathTool);
        logger.LogInformation("Registered tool: {ToolName}", mathTool.Name);

        logger.LogInformation("Tool registration completed. Total tools: {ToolCount}", 
            toolRegistry.GetAllTools().Count());
    }
}
