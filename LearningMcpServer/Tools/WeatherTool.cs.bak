using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LearningMcpServer.Tools;

/// <summary>
/// Provides deterministic mock weather data based on city name hash.
/// </summary>
public class WeatherTool : ITool
{
    /// <inheritdoc/>
    public string Name => "weather";

    /// <inheritdoc/>
    public string Description => "Get deterministic mock weather information for a city. Returns temperature, condition, humidity, wind speed, and last updated time.";

    /// <inheritdoc/>
    public JsonElement InputSchema => JsonSerializer.SerializeToElement(new
    {
        type = "object",
        properties = new
        {
            city = new
            {
                type = "string",
                description = "The name of the city to get weather for"
            }
        },
        required = new[] { "city" }
    });

    /// <summary>
    /// Represents weather data.
    /// </summary>
    public class WeatherData
    {
        [JsonPropertyName("city")]
        public string City { get; set; } = string.Empty;

        [JsonPropertyName("temperatureC")]
        public int TemperatureC { get; set; }

        [JsonPropertyName("condition")]
        public string Condition { get; set; } = string.Empty;

        [JsonPropertyName("humidityPct")]
        public int HumidityPct { get; set; }

        [JsonPropertyName("windKph")]
        public double WindKph { get; set; }

        [JsonPropertyName("lastUpdatedUtc")]
        public DateTime LastUpdatedUtc { get; set; }
    }

    /// <inheritdoc/>
    public async Task<object> ExecuteAsync(JsonElement arguments)
    {
        await Task.CompletedTask; // Make async to satisfy interface

        if (!arguments.TryGetProperty("city", out var cityElement))
        {
            throw new ArgumentException("Missing required parameter 'city'");
        }

        var city = cityElement.GetString();
        if (string.IsNullOrWhiteSpace(city))
        {
            throw new ArgumentException("City name cannot be empty");
        }

        return GenerateWeatherData(city);
    }

    /// <summary>
    /// Generates deterministic weather data based on city name hash.
    /// </summary>
    /// <param name="city">The city name</param>
    /// <returns>Weather data for the city</returns>
    private WeatherData GenerateWeatherData(string city)
    {
        // Create deterministic hash from city name
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(city.ToLowerInvariant()));
        
        // Use hash bytes to generate consistent weather data
        var temp = (hash[0] % 60) - 10; // Temperature between -10°C and 49°C
        var humidity = (hash[1] % 80) + 20; // Humidity between 20% and 99%
        var windSpeed = (hash[2] % 50) + (hash[3] / 255.0 * 10); // Wind between 0-60 kph
        
        // Determine condition based on hash
        var conditionIndex = hash[4] % 5;
        var conditions = new[] { "Sunny", "Cloudy", "Rainy", "Snowy", "Foggy" };
        var condition = conditions[conditionIndex];

        // Use a base date and add deterministic offset for consistency
        var baseDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var dayOffset = BitConverter.ToUInt16(hash, 6) % 365;
        var hourOffset = hash[8] % 24;
        var minuteOffset = hash[9] % 60;

        return new WeatherData
        {
            City = city,
            TemperatureC = temp,
            Condition = condition,
            HumidityPct = humidity,
            WindKph = Math.Round(windSpeed, 1),
            LastUpdatedUtc = baseDate.AddDays(dayOffset).AddHours(hourOffset).AddMinutes(minuteOffset)
        };
    }
}