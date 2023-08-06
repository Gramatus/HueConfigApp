using ConsoleTestApp.JsonConverters;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using System.Text.Json.Serialization;

namespace ConsoleTestApp.ApiObjects.Sensors.State {
  public abstract class SensorState<T> : SensorState {
    public abstract T State { get; set; }
  }
}
// Some (all?) sensor types are described here: https://github.com/ebaauw/homebridge-hue/wiki/Getting-Started
public class SensorState {
  [JsonPropertyName("lastupdated")]
  [JsonConverter(typeof(DateTimeWithNoneJsonConverter))]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public DateTime? LastUpdated { get; set; }
  [JsonExtensionData]
  public Dictionary<string, object> ExtensionData { get; set; }
}