using ConsoleTestApp.JsonConverters;
using System.Text.Json.Serialization;

namespace ConsoleTestApp.ApiObjects.Lights.State {
  public class LightStateXy : LightStateBase, ILightStateXy
  {
    private LightState state;
    public LightStateXy(LightState fullState)
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
    [JsonPropertyName("xy")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double[] XY_position { get { return state.XY_position; } set { state.XY_position = value; } }
    [JsonPropertyName("colormode")]
    [JsonConverter(typeof(ColorModeJsonConverter))]
    public ColorMode? colorModeCalculated { get { return ColorMode.hs; } }
  }
}