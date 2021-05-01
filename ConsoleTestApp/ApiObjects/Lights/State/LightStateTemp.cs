using ConsoleTestApp.JsonConverters;
using System.Text.Json.Serialization;

namespace ConsoleTestApp.ApiObjects.Lights.State {
  public class LightStateTemp : LightStateBase, ILightStateTemp
  {
    private LightState state;
    public LightStateTemp(LightState fullState)
    {
      state = fullState;
      Alert = state.Alert;
      Brightness = state.Brightness;
      CanBeReachedByBridge = state.CanBeReachedByBridge;
      Effect = state.Effect;
      IsOn = state.IsOn;
      Mode = state.Mode;
      TransitionTime = state.TransitionTime;
    }
    [JsonPropertyName("ct")]
    public int? ColorTemperature { get { return state.ColorTemperature; } set { state.ColorTemperature = value; } }
    [JsonPropertyName("colormode")]
    [JsonConverter(typeof(ColorModeJsonConverter))]
    public ColorMode? colorModeCalculated { get { return ColorMode.ct; } }
  }
}