using System.Text.Json.Serialization;

namespace ConsoleTestApp.ApiObjects.Lights.State
{
  public interface ILightStateTemp
  {
    [JsonPropertyName("ct")]
    public int? ColorTemperature { get; set; }
    [JsonPropertyName("colormode")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ColorMode? colorModeCalculated { get; }
  }
}