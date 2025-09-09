using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace LearningMcpServer.Tools;

/// <summary>
/// Performs basic mathematical operations with input validation.
/// </summary>
[McpServerToolType]
public class MathTool
{
    private readonly ILogger<MathTool> _logger;

    public MathTool(ILogger<MathTool> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Add two numbers together.
    /// </summary>
    /// <param name="a">First number</param>
    /// <param name="b">Second number</param>
    /// <returns>Sum of the two numbers</returns>
    [McpServerTool]
    [Description("Add two numbers together")]
    public MathResult Add([Description("First number")] double a, [Description("Second number")] double b)
    {
        _logger.LogInformation("Math add: {A} + {B}", a, b);
        var result = a + b;
        return new MathResult
        {
            Operation = "add",
            A = a,
            B = b,
            Result = result,
            Expression = $"{a} + {b} = {result}"
        };
    }

    /// <summary>
    /// Subtract the second number from the first.
    /// </summary>
    /// <param name="a">First number</param>
    /// <param name="b">Second number</param>
    /// <returns>Difference of the two numbers</returns>
    [McpServerTool]
    [Description("Subtract the second number from the first")]
    public MathResult Subtract([Description("First number")] double a, [Description("Second number")] double b)
    {
        _logger.LogInformation("Math subtract: {A} - {B}", a, b);
        var result = a - b;
        return new MathResult
        {
            Operation = "subtract",
            A = a,
            B = b,
            Result = result,
            Expression = $"{a} - {b} = {result}"
        };
    }

    /// <summary>
    /// Multiply two numbers together.
    /// </summary>
    /// <param name="a">First number</param>
    /// <param name="b">Second number</param>
    /// <returns>Product of the two numbers</returns>
    [McpServerTool]
    [Description("Multiply two numbers together")]
    public MathResult Multiply([Description("First number")] double a, [Description("Second number")] double b)
    {
        _logger.LogInformation("Math multiply: {A} * {B}", a, b);
        var result = a * b;
        return new MathResult
        {
            Operation = "multiply",
            A = a,
            B = b,
            Result = result,
            Expression = $"{a} * {b} = {result}"
        };
    }

    /// <summary>
    /// Divide the first number by the second.
    /// </summary>
    /// <param name="a">Dividend</param>
    /// <param name="b">Divisor</param>
    /// <returns>Quotient of the division</returns>
    [McpServerTool]
    [Description("Divide the first number by the second")]
    public MathResult Divide([Description("Dividend (number to be divided)")] double a, [Description("Divisor (number to divide by)")] double b)
    {
        _logger.LogInformation("Math divide: {A} / {B}", a, b);
        
        if (Math.Abs(b) < double.Epsilon)
        {
            throw new DivideByZeroException("Division by zero is not allowed");
        }
        
        var result = a / b;
        return new MathResult
        {
            Operation = "divide",
            A = a,
            B = b,
            Result = result,
            Expression = $"{a} / {b} = {result}"
        };
    }

    /// <summary>
    /// Result of a mathematical operation.
    /// </summary>
    public class MathResult
    {
        public string Operation { get; set; } = string.Empty;
        public double A { get; set; }
        public double B { get; set; }
        public double Result { get; set; }
        public string Expression { get; set; } = string.Empty;
    }
}
