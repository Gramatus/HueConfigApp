using ConsoleTestApp.ApiObjects.Sensors;
using ConsoleTestApp.ApiObjects.Sensors.State;
using ConsoleTestApp.JsonConverters;
using System;
using System.Linq;
using System.Text.Json.Serialization;

namespace ConsoleTestApp.ApiObjects.Rules {
  public class RuleCondition {
    private bool _isTimeCondition;
    [JsonIgnore]
    public Sensor Sensor { get; set; }
    #region AddressOfSensor
    [JsonPropertyName("address")]
    public string AddressOfSensor {
      get {
        if (this.Operator == RuleConditionOperator.dx || this.Operator == RuleConditionOperator.ddx) return "/sensors/" + this.Sensor.ID + "/state/lastupdated";
        else if (this._isTimeCondition) return "/config/localtime";
        else if (this.Sensor == null) return null;
        else if (this.Sensor is Sensor<BoolSensorState>) return "/sensors/" + this.Sensor.ID + "/state/flag";
        else if (this.Sensor is Sensor<IntSensorState>) return "/sensors/" + this.Sensor.ID + "/state/status";
        else if (this.Sensor is Sensor<DimmerButtonSensorState>) return "/sensors/" + this.Sensor.ID + "/state/buttonevent";
        else if (this.Sensor is Sensor<TemperatureSensorState>) return "/sensors/" + this.Sensor.ID + "/state/temperature";
        else throw new NotImplementedException();
      }
      set {
        if (value.Contains("/config/")) {
          this._isTimeCondition = true;
        }
        else {
          this._isTimeCondition = false;
          string v = value.Replace("/sensors/", "");
          string id = v.Substring(0, v.IndexOf('/'));
          // If not, there is really some mess with the config - but it is not that much of an issue so let it be
          if (Program.hueBridge.Sensors.ContainsKey(id)) this.Sensor = Program.hueBridge.Sensors[id];
        }
      }
    }
    #endregion
    [JsonPropertyName("operator")]
    [JsonConverter(typeof(RuleConditionOperatorJsonConverter))]
    public RuleConditionOperator Operator { get; set; }
    [JsonPropertyName("value")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Value { get; set; }
    public RuleCondition() { }
    public RuleCondition(Sensor sensor) {
      this.Sensor = sensor;
      // if (Program.hueBridge.Sensors.Any(i => i.Value.Name == sensorname)) this.Sensor = Program.hueBridge.Sensors.First(i => i.Value.Name == sensorname).Value;
      // else throw new ArgumentOutOfRangeException("No sensor named " + sensorname + " found!");
    }
  }
}
/*
			{
				"address": "/sensors/43/state/buttonevent",
				"operator": "eq",
				"value": "3001"
			}
*/
