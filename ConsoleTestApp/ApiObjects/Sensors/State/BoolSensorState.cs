using System.Text.Json.Serialization;

namespace ConsoleTestApp.ApiObjects.Sensors.State {
  public class BoolSensorState : SensorState<bool> {
    [JsonPropertyName("flag")]
    public override bool State { get; set; }
  }
}
