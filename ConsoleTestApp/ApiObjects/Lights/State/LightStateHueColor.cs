using ConsoleTestApp.JsonConverters;
using System.Text.Json.Serialization;

namespace ConsoleTestApp.ApiObjects.Lights.State {
  public class LightStateHueColor : LightStateBase, ILightStateHueColor
  {
    private LightState state;
    public LightStateHueColor(LightState fullState)
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
    [JsonPropertyName("hue")]
    public int? HueColor { get { return state.HueColor; } set { state.HueColor = value; } }
    [JsonPropertyName("sat")]
    public int? Saturation { get { return state.Saturation; } set { state.Saturation = value; } }
    [JsonPropertyName("colormode")]
    [JsonConverter(typeof(ColorModeJsonConverter))]
    public ColorMode? colorModeCalculated { get { return ColorMode.hs; } }
  }
}