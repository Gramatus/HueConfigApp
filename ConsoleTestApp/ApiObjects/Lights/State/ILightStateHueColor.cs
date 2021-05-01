using System.Text.Json.Serialization;

namespace ConsoleTestApp.ApiObjects.Lights.State
{
  public interface ILightStateHueColor
  {
    [JsonPropertyName("hue")]
    public int? HueColor { get; set; }
    [JsonPropertyName("sat")]
    public int? Saturation { get; set; }
    [JsonPropertyName("colormode")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ColorMode? colorModeCalculated { get; }

  }
}