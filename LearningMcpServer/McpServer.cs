using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace LearningMcpServer;

/// <summary>
/// MCP (Model Context Protocol) server implementation.
/// </summary>
public class McpServer
{
    private readonly ToolRegistry _toolRegistry;
    private readonly ILogger<McpServer> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private volatile bool _isRunning = true;

    /// <summary>
    /// Initializes a new instance of the McpServer.
    /// </summary>
    /// <param name="toolRegistry">The tool registry containing available tools</param>
    /// <param name="logger">Logger for structured logging</param>
    public McpServer(ToolRegistry toolRegistry, ILogger<McpServer> logger)
    {
        _toolRegistry = toolRegistry;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <summary>
    /// Starts the MCP server and processes requests from stdin.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Learning MCP Server starting up...");
        _logger.LogInformation("Available tools: {ToolCount}", _toolRegistry.GetAllTools().Count());
        
        foreach (var tool in _toolRegistry.GetAllTools())
        {
            _logger.LogInformation("  - {ToolName}: {ToolDescription}", tool.Name, tool.Description);
        }

        Console.WriteLine("Learning MCP Server v1.0.0 - Ready for connections");
        
        try
        {
            while (_isRunning && !cancellationToken.IsCancellationRequested)
            {
                var line = await Console.In.ReadLineAsync(cancellationToken);
                if (line == null)
                {
                    _logger.LogInformation("End of input detected, shutting down server");
                    break;
                }

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                await ProcessRequestAsync(line, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Server shutdown requested");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in server main loop");
        }

        _logger.LogInformation("Learning MCP Server shutting down");
    }

    /// <summary>
    /// Stops the MCP server gracefully.
    /// </summary>
    public void Stop()
    {
        _logger.LogInformation("Server stop requested");
        _isRunning = false;
    }

    /// <summary>
    /// Processes a single MCP request.
    /// </summary>
    /// <param name="requestJson">The JSON request string</param>
    /// <param name="cancellationToken">Cancellation token</param>
    private async Task ProcessRequestAsync(string requestJson, CancellationToken cancellationToken)
    {
        McpResponse response;
        string requestId = Guid.NewGuid().ToString();

        try
        {
            _logger.LogDebug("Processing request: {Request}", requestJson);

            var request = JsonSerializer.Deserialize<McpRequest>(requestJson, _jsonOptions);
            if (request == null)
            {
                throw new InvalidOperationException("Failed to deserialize request");
            }

            requestId = request.Id ?? requestId;

            response = await HandleRequestAsync(request, cancellationToken);
            response.Id = requestId;

            _logger.LogDebug("Request {RequestId} processed successfully", requestId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing request {RequestId}: {Error}", requestId, ex.Message);
            
            response = new McpResponse
            {
                Id = requestId,
                Error = new McpError
                {
                    Code = -32603, // Internal error
                    Message = ex.Message,
                    Data = new { type = ex.GetType().Name }
                }
            };
        }

        // Send response
        var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
        await Console.Out.WriteLineAsync(responseJson);
        await Console.Out.FlushAsync();
        
        _logger.LogDebug("Response sent for request {RequestId}", requestId);
    }

    /// <summary>
    /// Handles a specific MCP request based on its method.
    /// </summary>
    /// <param name="request">The MCP request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The MCP response</returns>
    private async Task<McpResponse> HandleRequestAsync(McpRequest request, CancellationToken cancellationToken)
    {
        return request.Method switch
        {
            "initialize" => HandleInitialize(request),
            "tools/list" => HandleToolsList(),
            "tools/call" => await HandleToolCallAsync(request, cancellationToken),
            _ => new McpResponse
            {
                Error = new McpError
                {
                    Code = -32601, // Method not found
                    Message = $"Method not found: {request.Method}"
                }
            }
        };
    }

    /// <summary>
    /// Handles the initialize method.
    /// </summary>
    /// <param name="request">The initialize request</param>
    /// <returns>The initialize response</returns>
    private McpResponse HandleInitialize(McpRequest request)
    {
        _logger.LogInformation("Client initialized connection");
        
        return new McpResponse
        {
            Result = new
            {
                protocolVersion = "2024-11-05",
                capabilities = new
                {
                    tools = new { }
                },
                serverInfo = new
                {
                    name = "LearningMcpServer",
                    version = "1.0.0"
                }
            }
        };
    }

    /// <summary>
    /// Handles the tools/list method.
    /// </summary>
    /// <returns>The tools list response</returns>
    private McpResponse HandleToolsList()
    {
        _logger.LogDebug("Listing available tools");
        
        var tools = _toolRegistry.GetAllToolInfos().ToArray();
        
        return new McpResponse
        {
            Result = new { tools }
        };
    }

    /// <summary>
    /// Handles the tools/call method.
    /// </summary>
    /// <param name="request">The tool call request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The tool call response</returns>
    private async Task<McpResponse> HandleToolCallAsync(McpRequest request, CancellationToken cancellationToken)
    {
        if (request.Params == null)
        {
            return new McpResponse
            {
                Error = new McpError
                {
                    Code = -32602, // Invalid params
                    Message = "Missing parameters for tool call"
                }
            };
        }

        try
        {
            var paramsJson = JsonSerializer.SerializeToElement(request.Params, _jsonOptions);
            var toolCallRequest = JsonSerializer.Deserialize<CallToolRequest>(paramsJson, _jsonOptions);
            
            if (toolCallRequest == null || string.IsNullOrWhiteSpace(toolCallRequest.Name))
            {
                return new McpResponse
                {
                    Error = new McpError
                    {
                        Code = -32602, // Invalid params
                        Message = "Invalid tool call parameters"
                    }
                };
            }

            _logger.LogInformation("Calling tool: {ToolName}", toolCallRequest.Name);

            var tool = _toolRegistry.GetTool(toolCallRequest.Name);
            if (tool == null)
            {
                return new McpResponse
                {
                    Error = new McpError
                    {
                        Code = -32601, // Method not found
                        Message = $"Tool not found: {toolCallRequest.Name}"
                    }
                };
            }

            var argumentsElement = toolCallRequest.Arguments != null 
                ? JsonSerializer.SerializeToElement(toolCallRequest.Arguments, _jsonOptions)
                : new JsonElement();

            var startTime = DateTime.UtcNow;
            var result = await tool.ExecuteAsync(argumentsElement);
            var executionTime = DateTime.UtcNow - startTime;

            _logger.LogInformation("Tool {ToolName} executed in {ExecutionTime}ms", 
                toolCallRequest.Name, executionTime.TotalMilliseconds);

            return new McpResponse
            {
                Result = new
                {
                    content = new[]
                    {
                        new
                        {
                            type = "text",
                            text = JsonSerializer.Serialize(result, _jsonOptions)
                        }
                    }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool call");
            
            return new McpResponse
            {
                Error = new McpError
                {
                    Code = -32603, // Internal error
                    Message = ex.Message,
                    Data = new { type = ex.GetType().Name }
                }
            };
        }
    }
}