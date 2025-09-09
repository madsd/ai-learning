using System.Collections.Concurrent;

namespace LearningMcpServer;

/// <summary>
/// Central registry for managing all available tools.
/// </summary>
public class ToolRegistry
{
    private readonly ConcurrentDictionary<string, ITool> _tools = new();

    /// <summary>
    /// Registers a tool in the registry.
    /// </summary>
    /// <param name="tool">The tool to register</param>
    public void RegisterTool(ITool tool)
    {
        _tools[tool.Name] = tool;
    }

    /// <summary>
    /// Gets a tool by name.
    /// </summary>
    /// <param name="name">The name of the tool</param>
    /// <returns>The tool if found, null otherwise</returns>
    public ITool? GetTool(string name)
    {
        _tools.TryGetValue(name, out var tool);
        return tool;
    }

    /// <summary>
    /// Gets all registered tools.
    /// </summary>
    /// <returns>Collection of all registered tools</returns>
    public IEnumerable<ITool> GetAllTools()
    {
        return _tools.Values;
    }

    /// <summary>
    /// Gets information about all registered tools.
    /// </summary>
    /// <returns>Collection of tool information objects</returns>
    public IEnumerable<ToolInfo> GetAllToolInfos()
    {
        return _tools.Values.Select(tool => new ToolInfo
        {
            Name = tool.Name,
            Description = tool.Description,
            InputSchema = tool.InputSchema
        });
    }
}