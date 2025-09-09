using System.Text.Json;

namespace LearningMcpServer;

/// <summary>
/// Interface for all MCP server tools.
/// </summary>
public interface ITool
{
    /// <summary>
    /// Gets the unique name of the tool.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of what the tool does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the JSON schema for the tool's input parameters.
    /// </summary>
    JsonElement InputSchema { get; }

    /// <summary>
    /// Executes the tool with the provided arguments.
    /// </summary>
    /// <param name="arguments">The input arguments as a JSON element</param>
    /// <returns>The result of the tool execution</returns>
    Task<object> ExecuteAsync(JsonElement arguments);
}