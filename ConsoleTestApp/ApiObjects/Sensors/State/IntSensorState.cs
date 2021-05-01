using System.Text.Json.Serialization;

namespace ConsoleTestApp.ApiObjects.Sensors.State {
  public class IntSensorState : SensorState<int> {
    [JsonPropertyName("status")]
    public override int State { get; set; }
  }
}
