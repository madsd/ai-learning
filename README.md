# ai-learning

A repo for learning AI and Agent interactions with GitHub.

## Advantages of Learning AI

- AI is transforming industries by automating tasks, enhancing decision-making, and enabling new capabilities.
- Learning AI opens up opportunities in cutting-edge fields such as machine learning, natural language processing, robotics, and data science.
- AI skills are in high demand and can lead to impactful, future-proof careers.
- Understanding AI helps you build smarter applications and solve complex real-world problems.

## The Importance of GitHub in AI Learning

- GitHub is the leading platform for sharing, collaborating, and versioning code, making it essential for AI projects.
- It enables you to contribute to open-source AI frameworks, datasets, and research.
- GitHub Actions and integrations streamline the development, testing, and deployment of AI models.
- Collaboration on GitHub accelerates learning through code reviews, issue tracking, and community engagement.

## Getting Started

1. Clone this repository.
2. Explore sample projects (coming soon) focused on AI prompts, agents, and automation.
3. Open issues or discussions to suggest learning topics.

## Learning MCP Server

This repository includes a Model Context Protocol (MCP) server implementation in .NET 8 that provides various tools for AI agents to interact with. The server supports stdio transport and implements several useful tools for learning and experimentation.

### Available Tools

The Learning MCP Server provides the following tools:

#### 1. Weather Tool (`weather`)
Provides deterministic mock weather data based on city name hash.

**Parameters:**
- `city` (string, required): The name of the city to get weather for

**Response:**
- `city`: The city name
- `temperatureC`: Temperature in Celsius (-10 to 49)
- `condition`: Weather condition (Sunny, Cloudy, Rainy, Snowy, Foggy)
- `humidityPct`: Humidity percentage (20-99%)
- `windKph`: Wind speed in km/h (0-60)
- `lastUpdatedUtc`: Last updated timestamp

**Example JSON payload:**
```json
{
  "jsonrpc": "2.0",
  "id": "1",
  "method": "tools/call",
  "params": {
    "name": "weather",
    "arguments": {
      "city": "London"
    }
  }
}
```

#### 2. File Summary Tool (`file_summary`)
Summarizes text files with size and binary content validation. Files must be under 200KB and contain text content only.

**Parameters:**
- `filePath` (string, required): Path to the file to summarize (relative to repository root)

**Response:**
- `filePath`: The normalized file path
- `summary`: Text summary (~200 words)
- `wordCount`: Total word count
- `lineCount`: Total line count
- `characterCount`: Total character count

**Example JSON payload:**
```json
{
  "jsonrpc": "2.0",
  "id": "2",
  "method": "tools/call",
  "params": {
    "name": "file_summary",
    "arguments": {
      "filePath": "README.md"
    }
  }
}
```

#### 3. Markdown Headings Tool (`markdown_headings`)
Extracts markdown headings (levels 1-6) from text content, returning heading text, level, and line number.

**Parameters:**
- `filePath` (string, optional): Path to the markdown file to extract headings from
- `content` (string, optional): Direct markdown content to extract headings from

*Note: Either `filePath` or `content` must be provided.*

**Response:**
Array of heading objects with:
- `text`: The heading text
- `level`: Heading level (1-6)
- `line`: Line number in the file/content

**Example JSON payload:**
```json
{
  "jsonrpc": "2.0",
  "id": "3",
  "method": "tools/call",
  "params": {
    "name": "markdown_headings",
    "arguments": {
      "filePath": "README.md"
    }
  }
}
```

#### 4. Math Tool (`math`)
Performs basic mathematical operations with input validation and prevents division by zero.

**Parameters:**
- `operation` (string, required): The operation to perform (`add`, `subtract`, `multiply`, `divide`)
- `a` (number, required): The first number
- `b` (number, required): The second number

**Response:**
- `operation`: The operation performed
- `a`: First operand
- `b`: Second operand
- `result`: Result of the operation
- `expression`: Formatted expression string

**Example JSON payload:**
```json
{
  "jsonrpc": "2.0",
  "id": "4",
  "method": "tools/call",
  "params": {
    "name": "math",
    "arguments": {
      "operation": "multiply",
      "a": 15,
      "b": 7
    }
  }
}
```

### Running the MCP Server

1. **Prerequisites**: Ensure you have .NET 8 SDK installed
2. **Build the project**: 
   ```bash
   cd LearningMcpServer
   dotnet build
   ```
3. **Run the server**:
   ```bash
   dotnet run
   ```
4. **With debug logging**:
   ```bash
   dotnet run -- --debug
   ```

The server communicates via JSON-RPC 2.0 over stdin/stdout and supports graceful shutdown with Ctrl+C.

### VS Code Integration

The server includes VS Code MCP configuration in `.vscode/mcp.json` for auto-discovery by MCP-compatible VS Code extensions.

### Security Features

- **Path traversal prevention**: File operations are restricted to the repository root
- **File size limits**: File summary tool rejects files over 200KB
- **Binary content detection**: File operations reject binary files
- **Input validation**: All tools validate input parameters and handle errors gracefully
- **Structured error responses**: All errors return proper MCP error objects

## Contributing

Contributions are welcome. Feel free to open issues or submit pull requests with improvements or new learning modules.

## License

This project is licensed under the MIT License - see the LICENSE file for details.
