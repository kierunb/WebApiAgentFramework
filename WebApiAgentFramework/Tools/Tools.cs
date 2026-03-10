using System.ComponentModel;

namespace WebApiAgentFramework.Tools;

public static class Tools
{
    [Description("Get the weather for a given location.")]
    public static string GetWeather(
        [Description("The location to get the weather for.")] string location
    ) => $"The weather in {location} is cloudy with a high of 15°C.";

    [Description("Calculate the sum of two numbers.")]
    public static double Add(
        [Description("The first number.")] double a,
        [Description("The second number.")] double b
    ) => a + b;

    [Description("Get the current time.")]
    public static string GetCurrentTime() => DateTime.Now.ToString("HH:mm:ss");
}
