using ConsoleTestApp.ApiObjects.Rules.Actions;
using System;
using System.Text.Json.Serialization;

namespace ConsoleTestApp.ApiObjects.Schedules {
  public class Timer : ConvertedSchedule {
    [JsonIgnore]
    public TimeSpan Duration { get => TimeSpan.Parse(this.TimePart); set => this.TimePart = value.ToString(@"\Thh\:mm\:ss"); }
    #region ### Static methods
    public Timer() {
      this.IsTimer = true;
    }
    public Timer(string name, string description, TimeSpan duration) : this() {
      this.Name = name;
      this.Description = description;
      this.Duration = duration;
    }
    public static Timer Create(string name, string description, string strTime, RuleActionBase action, bool printInfo, bool pauseBeforeUpdating) {
      var timer = new Timer();
      timer.Name = name;
      timer.Description = description;
      timer.StrTime = strTime;
      timer.Action = action;
      timer.Action.IsScheduleAction = true;
      timer.Create(printInfo, pauseBeforeUpdating);
      Program.hueBridge.Timers.Add(timer.ID, timer);
      return timer;
    }
    #endregion
  }
}