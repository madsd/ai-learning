using System.Text.Json;
using System.Text.Json.Serialization;

namespace LearningMcpServer.Tools;

/// <summary>
/// Performs basic mathematical operations with input validation.
/// </summary>
public class MathTool : ITool
{
    /// <inheritdoc/>
    public string Name => "math";

    /// <inheritdoc/>
    public string Description => "Perform basic mathematical operations: add, subtract, multiply, divide. Includes input validation and prevents division by zero.";

    /// <inheritdoc/>
    public JsonElement InputSchema => JsonSerializer.SerializeToElement(new
    {
        type = "object",
        properties = new
        {
            operation = new
            {
                type = "string",
                @enum = new[] { "add", "subtract", "multiply", "divide" },
                description = "The mathematical operation to perform"
            },
            a = new
            {
                type = "number",
                description = "The first number"
            },
            b = new
            {
                type = "number",
                description = "The second number"
            }
        },
        required = new[] { "operation", "a", "b" }
    });

    /// <summary>
    /// Represents the result of a mathematical operation.
    /// </summary>
    public class MathResult
    {
        [JsonPropertyName("operation")]
        public string Operation { get; set; } = string.Empty;

        [JsonPropertyName("a")]
        public double A { get; set; }

        [JsonPropertyName("b")]
        public double B { get; set; }

        [JsonPropertyName("result")]
        public double Result { get; set; }

        [JsonPropertyName("expression")]
        public string Expression { get; set; } = string.Empty;
    }

    /// <inheritdoc/>
    public async Task<object> ExecuteAsync(JsonElement arguments)
    {
        await Task.CompletedTask; // Make async to satisfy interface

        // Validate and extract operation
        if (!arguments.TryGetProperty("operation", out var operationElement))
        {
            throw new ArgumentException("Missing required parameter 'operation'");
        }

        var operation = operationElement.GetString();
        if (string.IsNullOrWhiteSpace(operation))
        {
            throw new ArgumentException("Operation cannot be empty");
        }

        // Validate and extract first number
        if (!arguments.TryGetProperty("a", out var aElement))
        {
            throw new ArgumentException("Missing required parameter 'a'");
        }

        if (!aElement.TryGetDouble(out var a))
        {
            throw new ArgumentException("Parameter 'a' must be a valid number");
        }

        // Validate and extract second number
        if (!arguments.TryGetProperty("b", out var bElement))
        {
            throw new ArgumentException("Missing required parameter 'b'");
        }

        if (!bElement.TryGetDouble(out var b))
        {
            throw new ArgumentException("Parameter 'b' must be a valid number");
        }

        // Check for invalid numbers (NaN, Infinity)
        if (!IsValidNumber(a))
        {
            throw new ArgumentException("Parameter 'a' contains an invalid number (NaN or Infinity)");
        }

        if (!IsValidNumber(b))
        {
            throw new ArgumentException("Parameter 'b' contains an invalid number (NaN or Infinity)");
        }

        // Perform the operation
        var result = PerformOperation(operation.ToLowerInvariant(), a, b);

        return new MathResult
        {
            Operation = operation.ToLowerInvariant(),
            A = a,
            B = b,
            Result = result,
            Expression = FormatExpression(operation.ToLowerInvariant(), a, b, result)
        };
    }

    /// <summary>
    /// Checks if a number is valid (not NaN or Infinity).
    /// </summary>
    /// <param name="value">The number to check</param>
    /// <returns>True if the number is valid, false otherwise</returns>
    private static bool IsValidNumber(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }

    /// <summary>
    /// Performs the specified mathematical operation.
    /// </summary>
    /// <param name="operation">The operation to perform</param>
    /// <param name="a">The first operand</param>
    /// <param name="b">The second operand</param>
    /// <returns>The result of the operation</returns>
    private static double PerformOperation(string operation, double a, double b)
    {
        return operation switch
        {
            "add" => a + b,
            "subtract" => a - b,
            "multiply" => a * b,
            "divide" => b == 0 ? throw new DivideByZeroException("Division by zero is not allowed") : a / b,
            _ => throw new ArgumentException($"Unsupported operation: {operation}. Supported operations are: add, subtract, multiply, divide")
        };
    }

    /// <summary>
    /// Formats the mathematical expression as a string.
    /// </summary>
    /// <param name="operation">The operation performed</param>
    /// <param name="a">The first operand</param>
    /// <param name="b">The second operand</param>
    /// <param name="result">The result of the operation</param>
    /// <returns>A formatted expression string</returns>
    private static string FormatExpression(string operation, double a, double b, double result)
    {
        var operatorSymbol = operation switch
        {
            "add" => "+",
            "subtract" => "-",
            "multiply" => "*",
            "divide" => "/",
            _ => "?"
        };

        return $"{a} {operatorSymbol} {b} = {result}";
    }
}