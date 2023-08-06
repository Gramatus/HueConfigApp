using ConsoleTestApp.ApiObjects.Lights;
using ConsoleTestApp.JsonConverters;
using System.Text.Json.Serialization;

namespace ConsoleTestApp.ApiObjects.Lights.State {
  public class LightStateBase : IApiObject {
    #region Instance fields
    private int? _Brightness;
    protected Light _Light;
    #endregion
    #region Instance properties
    [JsonIgnore]
    public string ID { get => "LightState has no ID"; }
    [JsonIgnore]
    public string Name { get => "Performing a state change on " + this.LightName; }

    [JsonPropertyName("on")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsOn { get; set; }

    [JsonPropertyName("bri")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Brightness {
      get { return _Brightness; }
      set { _Brightness = value; }
    }

    [JsonPropertyName("effect")]
    [JsonConverter(typeof(LightEffectsJsonConverter))]
    // [JsonConverter(typeof(JsonStringEnumConverter))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public LightEffects? Effect { get; set; }
    /// <summary>
    /// The alert effect, which is a temporary change to the bulb’s state. This can take one of the following values:
    /// “none” – The light is not performing an alert effect.
    /// “select” – The light is performing one breathe cycle.
    /// “lselect” – The light is performing breathe cycles for 15 seconds or until an "alert": "none" command is received.
    ///
    /// Note that this contains the last alert sent to the light and not its current state.
    /// i.e. After the breathe cycle has finished the bridge does not reset the alert to “none“.
    /// </summary>
    [JsonPropertyName("alert")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Alert { get; set; }
    [JsonPropertyName("mode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Mode { get; set; }
    [JsonPropertyName("reachable")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? CanBeReachedByBridge { get; set; }
    /// <summary>In 100ms, e.g. 10 = 1s, 600 = 1m.</summary>
    [JsonPropertyName("transitiontime")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? TransitionTime { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string LightName {
      get {
        if (_Light == null) return "";
        else return _Light.Name;
      }
    }
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string LightID {
      get {
        if (_Light == null) return "";
        else return _Light.ID;
      }
    }
    #endregion
    #region Instance methods
    public void ConnectToLight(Light light) {
      _Light = light;
    }
    #endregion
  }
}