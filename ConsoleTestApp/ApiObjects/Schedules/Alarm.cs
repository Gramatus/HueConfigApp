using ConsoleTestApp.ApiObjects.Rules.Actions;
using System;
using System.Text.Json.Serialization;

namespace ConsoleTestApp.ApiObjects.Schedules {
  public class Alarm : ConvertedSchedule {
    #region ### Instance properties
    #region WeekDayBitmask
    private WeekdayBitmask _weekDayBitmask;
    [JsonIgnore]
    public WeekdayBitmask WeekDayBitmask {
      // The format is W, then a decimal value and then /. The decimal value equals a bitmask (8 bits) on the format 0MTWTFSS
      get {
        if (this.WeekdayPart != null) {
          if (this._weekDayBitmask == null) this._weekDayBitmask = new WeekdayBitmask(this);
          return this._weekDayBitmask;
        }
        else {
          return null;
        }
      }
    }
    #endregion
    #region RecurringAlarmTime 
    [JsonIgnore]
    public TimeSpan? RecurringAlarmTime {
      get => (this.WeekdayPart != null) ? (TimeSpan?)TimeSpan.Parse(this.TimePart) : null;
      set {
        if (this.WeekDayBitmask == null) this._weekDayBitmask.SelectedDays = 0;
        this.TimePart = (value == null) ? null : value.Value.ToString(@"\Thh\:mm\:ss");
      }
    }
    #endregion
    #region SingleAlarmTime 
    [JsonIgnore]
    public DateTime? SingleAlarmTime {
      get => (this.WeekdayPart != null) ? null : (DateTime?)DateTime.Parse(this.TimePart);
      set {
        if (value == null) this.TimePart = null;
        else {
          this.TimePart = value.Value.ToString("s");
          this.WeekdayPart = null;
        }
      }
    }
    #endregion
    #endregion
    #region ### Constructor
    public Alarm() {
      this.DeleteAfterRunning = null;
      this._weekDayBitmask = new WeekdayBitmask(this);
    }
    public Alarm(string name, string description, TimeSpan startTime, WeekDays days) : this() {
      this.Name = name;
      this.Description = description;
      this.RecurringAlarmTime = startTime;
      this.WeekDayBitmask.SelectedDays = days;
    }
    #endregion
    #region ### Static methods
    public static Alarm Create(string name, string description, string strTime, RuleActionBase action, bool printInfo, bool pauseBeforeUpdating) {
      var alarm = new Alarm();
      alarm.Name = name;
      alarm.Description = description;
      alarm.StrTime = strTime;
      alarm.Action = action;
      alarm.Action.IsScheduleAction = true;
      alarm.Create(printInfo, pauseBeforeUpdating);
      Program.hueBridge.Alarms.Add(alarm.ID, alarm);
      return alarm;
    }
    #endregion
  }
}