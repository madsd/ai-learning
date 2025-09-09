using System.Security.Cryptography;
using System.Text;
using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace LearningMcpServer.Tools;

/// <summary>
/// Provides deterministic mock weather data based on city name hash.
/// </summary>
[McpServerToolType]
public class WeatherTool
{
    private readonly ILogger<WeatherTool> _logger;

    public WeatherTool(ILogger<WeatherTool> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get deterministic mock weather information for a city.
    /// </summary>
    /// <param name="city">The name of the city to get weather for</param>
    /// <returns>Weather information including temperature, condition, humidity, wind speed, and last updated time</returns>
    [McpServerTool]
    [Description("Get deterministic mock weather information for a city. Returns temperature, condition, humidity, wind speed, and last updated time.")]
    public WeatherData GetWeather([Description("The name of the city to get weather for")] string city)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            _logger.LogInformation("Weather tool called for city: {City}", city);
            
            if (string.IsNullOrWhiteSpace(city))
            {
                throw new ArgumentException("City name cannot be empty", nameof(city));
            }

            var weather = GenerateWeatherData(city);

            var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation("Weather tool completed in {ElapsedMs}ms for city: {City}", elapsedMs, city);

            return weather;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in weather tool for city: {City}", city);
            throw;
        }
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

    /// <summary>
    /// Represents weather data.
    /// </summary>
    public class WeatherData
    {
        public string City { get; set; } = string.Empty;
        public int TemperatureC { get; set; }
        public string Condition { get; set; } = string.Empty;
        public int HumidityPct { get; set; }
        public double WindKph { get; set; }
        public DateTime LastUpdatedUtc { get; set; }
    }
}
