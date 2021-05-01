using System.Text.Json.Serialization;

namespace ConsoleTestApp.ApiObjects.Sensors.State {
  public class DimmerButtonSensorState : SensorState<int> {
    [JsonPropertyName("buttonevent")]
    public override int State {
      get => (int)Button * 1000 + (int)ButtonState;
      set {
        int buttonStateValue = value % 1000;
        ButtonState = (bState)buttonStateValue;
        Button = (dButton)((value - buttonStateValue) / 1000);
      }
    }
    [JsonIgnore]
    public dButton Button { get; set; }
    [JsonIgnore]
    public bState ButtonState { get; set; }
  }
}
