using System.Text.Json.Serialization;

namespace ConsoleTestApp.ApiObjects.Lights
{
  public class LightCapabilities
  {
    [JsonPropertyName("certified")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenNull)]
    public bool? IsCertified { get; set; }
    [JsonPropertyName("control")]
    public LightControlCapabilities ControlCapabilities { get; set; }
    [JsonPropertyName("streaming")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenNull)]
    public LightStreamingCapabilities Streaming { get; set; }
  }
}
