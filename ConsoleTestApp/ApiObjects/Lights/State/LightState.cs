using ConsoleTestApp.JsonConverters;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Resources;
using System.Text;
using System.Text.Json.Serialization;

namespace ConsoleTestApp.ApiObjects.Lights.State {
  public class LightStateChanger : LightState {
    /// <summary>-254 to 254</summary>
    [JsonPropertyName("bri_inc")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ChangeBrightness { get; set; }
    /// <summary>-254 to 254</summary>
    [JsonPropertyName("sat_inc")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ChangeSaturation { get; set; }
    /// <summary>-65534 to 65534</summary>
    [JsonPropertyName("hue_inc")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ChangeHue { get; set; }
    /// <summary>-65534 to 65534</summary>
    [JsonPropertyName("ct_inc")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ChangeColorTemperature { get; set; }
    /// <summary>-0.5 to 0.5</summary>
    [JsonPropertyName("xy_inc")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double[]? ChangeXY { get; set; }

  }
  public class LightState : LightStateBase, ILightStateHueColor, ILightStateTemp
  {
    [JsonPropertyName("hue")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? HueColor { get; set; }
    [JsonPropertyName("sat")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Saturation { get; set; }
    [JsonPropertyName("xy")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double[] XY_position { get; set; }
    [JsonPropertyName("ct")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ColorTemperature { get; set; }
    private ColorMode? _colorMode;
    [JsonPropertyName("colormode")]
    [JsonConverter(typeof(ColorModeJsonConverter))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ColorMode? ColorMode {
      get { return _colorMode; }
      set { _colorMode = value; }
    }
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public ColorMode? colorModeCalculated {
      get {
        if (_colorMode != null) return _colorMode;
        else if (HueColor != null) return State.ColorMode.hs;
        else if (XY_position != null && XY_position.Length == 2) return State.ColorMode.xy;
        else if (ColorTemperature != null) return State.ColorMode.ct;
        else return null;
      }
    }
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public ColorMode? ColorCapabilities {
      get {
        if (_Light.Capabilities.ControlCapabilities.ColorGamutType != null) return State.ColorMode.hs;
        else if (_Light.Capabilities.ControlCapabilities.ColorTemperatureRange != null) return State.ColorMode.ct;
        else return null;
      }
    }
  }
}