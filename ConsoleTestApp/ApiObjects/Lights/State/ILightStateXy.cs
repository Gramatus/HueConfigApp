using System.Text.Json.Serialization;

namespace ConsoleTestApp.ApiObjects.Lights.State
{
  public interface ILightStateXy {
    [JsonPropertyName("xy")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double[] XY_position { get; set; }
    [JsonPropertyName("colormode")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ColorMode? colorModeCalculated { get; }

  }
}