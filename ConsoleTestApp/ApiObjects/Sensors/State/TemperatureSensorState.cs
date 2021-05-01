using System.Text.Json.Serialization;

namespace ConsoleTestApp.ApiObjects.Sensors.State {
  public class TemperatureSensorState : SensorState<int> {
    [JsonPropertyName("temperature")]
    public override int State { get; set; }
  }
}
