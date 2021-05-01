using ConsoleTestApp.ApiObjects.Rules;
using ConsoleTestApp.ApiObjects.Schedules;
using ConsoleTestApp.ApiObjects.Sensors;
using ConsoleTestApp.ApiObjects.Sensors.State;
using System.Collections.Generic;

namespace ConsoleTestApp.AppModel {
  class complexBridgeSetup {
    protected List<Rule> rules = new List<Rule>();
    #region AddConditionValueEquals
    /// <summary>Adds a value equals condition to all rules (e.g. if you only want this setup to work if a given condition is true.</summary>
    public void AddConditionValueEquals<T>(Sensor<BoolSensorState> sensorThatMustBeTrue, T sensorShouldBe) {
      foreach (var rule in this.rules) {
        rule.AddConditionValueEquals(sensorThatMustBeTrue, sensorShouldBe);
      }
    }
    #endregion
    #region ### Static methods
    protected static void AddToBridgeDictionaries(Schedule schedule) {
      // schedule.IsCreatedFromCode = true;
      // if (schedule is Timer && !Program.hueBridge.Timers.ContainsKey(schedule.Name)) Program.hueBridge.Timers.Add(schedule.Name, (Timer)schedule);
      // if (schedule is Alarm && !Program.hueBridge.Alarms.ContainsKey(schedule.Name)) Program.hueBridge.Alarms.Add(schedule.Name, (Alarm)schedule);
    }
    protected static void AddToBridgeDictionaries(Sensor sensor) {
      // sensor.IsCreatedFromCode = true;
      // if (!Program.hueBridge.Sensors.ContainsKey(sensor.Name)) Program.hueBridge.Sensors.Add(sensor.Name, sensor);
    }
    #endregion
  }
}
