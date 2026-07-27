using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Service.Tools;

[McpServerToolType]
public class CustomerTools
{
    [McpServerTool,
     Description("Gets the current weather for a specific city location.")]
    public async Task<string> GetWeatherAsync(
        [Description("The city name, e.g. 'New York'.")] string cityName)
    {
        await Task.Delay(10); 
        return $"The weather in {cityName} is currently Sunny and 22°C.";
    }
}