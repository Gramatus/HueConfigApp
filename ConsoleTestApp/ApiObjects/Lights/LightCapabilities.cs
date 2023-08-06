using System.Text.Json.Serialization;

namespace ConsoleTestApp.ApiObjects.Lights
{
  public class LightCapabilities
  {
    [JsonPropertyName("certified")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsCertified { get; set; }
    [JsonPropertyName("control")]
    public LightControlCapabilities ControlCapabilities { get; set; }
    [JsonPropertyName("streaming")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public LightStreamingCapabilities Streaming { get; set; }
  }
}
