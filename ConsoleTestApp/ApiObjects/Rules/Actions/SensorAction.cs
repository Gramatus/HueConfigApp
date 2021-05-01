using ConsoleTestApp.ApiObjects.Sensors;
using ConsoleTestApp.ApiObjects.Sensors.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;

namespace ConsoleTestApp.ApiObjects.Rules.Actions {
  public class IntSensorAction : SensorAction<IntSensorState> {
    // public IntSensorState State { get { return (IntSensorState)this._sensor.State; } }
    public IntSensorAction() : base() { }
    public IntSensorAction(Sensor<IntSensorState> sensor, int value, bool isScheduleAction) : base(sensor, isScheduleAction) {
      this.ActionData.State = value;
    }
    public override void WriteActionData(ApiBody data) {
      this.ActionData.State = this.GetIntFromDataDict("status", data, false).Value;
    }
    protected override ApiBody ReadActionData() {
      this.SafeAddToDataDict("status", this.ActionData.State, true);
      return this._dataDict;
    }
  }
  public class BoolSensorAction : SensorAction<BoolSensorState> {
    public BoolSensorAction() : base() { }
    public BoolSensorAction(Sensor<BoolSensorState> sensor, bool value, bool isScheduleAction) : base(sensor, isScheduleAction) {
      this.ActionData.State = value;
    }
    public override void WriteActionData(ApiBody data) {
      this.ActionData.State = this.GetBoolFromDataDict("flag", data, false).Value;
    }
    protected override ApiBody ReadActionData() {
      this.SafeAddToDataDict("flag", this.ActionData.State, true);
      return this._dataDict;
    }
  }
  public abstract class SensorAction<T> : RuleActionBase<T> where T : SensorState, new() {
    private Sensor<T> _sensor;
    [JsonPropertyName("address")]
    public override string AddressOfAction {
      get => (this.IsScheduleAction ? Program.userAPIroot : "") + "/sensors/" + _sensor.ID + "/state";
      set {
        string sensorID = value.Replace((this.IsScheduleAction ? Program.userAPIroot : "") + "/sensors/", "").Replace("/state", "");
        try {
          this._sensor = (Sensor<T>)Program.hueBridge.Sensors[sensorID];
          this.ActionData = _sensor.State;
        }
        catch (Exception ex) {
          Console.WriteLine("Could not find sensor with ID " + sensorID + ", probably an invalid rule, check bridge to make sure. Exception: " + ex.Message);
        }
      }
    }
    protected SensorAction(Sensor<T> sensor, bool isScheduleAction) : this() {
      this.IsScheduleAction = isScheduleAction;
      this._sensor = sensor;
      // if (Program.hueBridge.Sensors.Any(i => i.Value.Name == sensorName)) this._sensor = (Sensor<T>)Program.hueBridge.Sensors.First(i => i.Value.Name == sensorName).Value;
      // else throw new ArgumentOutOfRangeException("No sensor named " + sensorName + " found!");
      // this._sensor = sensor;
    }
    protected SensorAction() {
      this.ActionData = new T();
      this.Method = ActionMethod.PUT;
    }
  }
}
