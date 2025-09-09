using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using LearningMcpServer.Tools;
using LearningMcpServer;

/// <summary>
/// Main entry point for the Learning MCP Server.
/// Provides stdio-based MCP (Model Context Protocol) server with tools for weather, file operations, and math.
/// Now using the ModelContextProtocol .NET SDK for improved maintainability and protocol compliance.
/// </summary>
class Program
{
    /// <summary>
    /// Main entry point for the application.
    /// </summary>
    /// <param name="args">Command line arguments. Use --debug for verbose logging.</param>
    static async Task Main(string[] args)
    {
        // Print startup banner
        Console.WriteLine("========================================");
        Console.WriteLine("    Learning MCP Server v1.0.0");
        Console.WriteLine("    Model Context Protocol Server");
        Console.WriteLine("    (Using ModelContextProtocol .NET SDK)");
        Console.WriteLine("========================================");
        Console.WriteLine();

        try
        {
            // Create host builder with proper MCP server configuration
            var builder = Host.CreateApplicationBuilder(args);
            
            // Configure logging to stderr as recommended by MCP protocol
            builder.Logging.AddConsole(consoleLogOptions =>
            {
                // Configure all logs to go to stderr per MCP spec
                consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
            });
            
            // Configure logging level based on debug flag
            if (args.Contains("--debug"))
            {
                builder.Logging.SetMinimumLevel(LogLevel.Debug);
            }
            else
            {
                builder.Logging.SetMinimumLevel(LogLevel.Information);
            }

            // Add MCP server with stdio transport and tools
            builder.Services
                .AddMcpServer()
                .WithStdioServerTransport()  // This is crucial for stdio communication
                .WithTools<WeatherTool>()    // Migrated weather tool
                .WithTools<MathTool>();      // Migrated math tool

            // Build and run the host
            var host = builder.Build();
            
            Console.WriteLine("Learning MCP Server v1.0.0 - Ready for connections");
            
            // Start the MCP server using the SDK hosting model
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting MCP server: {ex.Message}");
            Environment.Exit(1);
        }
    }
}
