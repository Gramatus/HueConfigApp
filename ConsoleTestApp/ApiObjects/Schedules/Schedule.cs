using ConsoleTestApp.ApiObjects.Rules.Actions;
using ConsoleTestApp.JsonConverters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace ConsoleTestApp.ApiObjects.Schedules {
  // Because the custom converter needs to use a standard converter on the schedule class, we need this extra class layer
  [JsonConverter(typeof(ScheduleJsonConverter))]
  public class ConvertedSchedule : Schedule { }
  // Difference between Timer and Alarm is that a timer has a "timer pattern" in the "time" attribute
  // "time": "PT00:01:00" means a timer for one minute
  // "time": "2014-06-23T13:52:00" means an alarm at the given time
  // See: https://developers.meethue.com/develop/hue-api/datatypes-and-time-patterns/
  // NB: PT is actually from the ISO standard, P = Period, T = division between date and time (and with no date, the T comes right after the P)
  public class Schedule : IApiObject {
    private string description;
    #region ### Instance properties
    [JsonPropertyName("status")]
    [JsonConverter(typeof(EnabledStateJsonConverter))]
    // 8.6.20: Changed to nullable - hopefully that won't break anything...
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EnabledState? Status { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("description")]
    public string Description { get => description; set => description = value.Length > 64 ? value.Substring(0, 64) : value; }
    [JsonIgnore]
    public string ID { get; set; }
    [JsonPropertyName("command")]
    public RuleActionBase Action { get; set; }
    #region strTime
    [JsonPropertyName("localtime")]
    public string StrTime {
      get { return this.WeekdayPart + (this.IsRecurringTimer ? "R" + (this.RecurTimes == null ? "" : this.RecurTimes.ToString()) + "/" : "") + (this.IsTimer ? "P" : "") + this.TimePart + this.RandomPart; }
      set {
        this.WeekdayPart = (value.Contains("/") && value.StartsWith("W")) ? value.Substring(0, 5) : null;
        int RandomPartPos = value.IndexOf('A');
        this.RandomPart = RandomPartPos == -1 ? null : value.Substring(RandomPartPos);
        this.TimePart = value;
        if (this.WeekdayPart != null) this.TimePart = this.TimePart.Replace(this.WeekdayPart, "");
        if (this.RandomPart != null) this.TimePart = this.TimePart.Replace(this.RandomPart, "");
        // TODO: Confirm recurring logic
        if (this.TimePart.StartsWith("R")) {
          this.IsRecurringTimer = true;
          int slashPos = this.TimePart.IndexOf('/');
          this.RecurTimes = (slashPos > 1) ? (int?)Int32.Parse(this.TimePart.Substring(1, slashPos)) : null;
          this.TimePart = this.TimePart.Substring(slashPos + 1);
        }
        if (this.TimePart.StartsWith('P')) {
          this.IsTimer = true;
          this.TimePart = this.TimePart.Substring(1);
        }
      }
    }
    #endregion
    [JsonIgnore]
    public bool IsTimer { get; set; }
    protected bool IsRecurringTimer { get; set; }
    protected int? RecurTimes { get; set; }
    [JsonIgnore]
    public string WeekdayPart { get; set; }
    protected string TimePart { get; set; }
    // TODO: Add logic to handle randomized times
    protected string RandomPart { get; set; }
    [JsonPropertyName("recycle")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? CanBeAutoRecycled { get; set; }
    [JsonPropertyName("autodelete")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? DeleteAfterRunning { get; set; }
    [JsonPropertyName("created")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? CreatedTime { get; set; }
    /// <summary>UTC time that the timer was started (last time it was started). Only provided for timers.</summary>
    [JsonPropertyName("starttime")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? StartTime { get; set; }
    [JsonIgnore]
    public bool IsCreatedFromCode { get; set; }
    #endregion
    #region ### Instance methods
    #region CreateIfMissingHijackIfExisting
    public void CreateIfMissingHijackIfExisting(bool printInfo, bool pauseBeforeUpdating) {
      Schedule oldSchedule;
      if (this.IsTimer) oldSchedule = Program.hueBridge.Timers.Values.FirstOrDefault(i => i.Name == this.Name && !i.IsCreatedFromCode);
      else oldSchedule = Program.hueBridge.Alarms.Values.FirstOrDefault(i => i.Name == this.Name && !i.IsCreatedFromCode);
      // If there already exists a schedule in the bridge with this name, we "hijack" it and send an update from our new schedule
      if (oldSchedule != null) {
        this.ID = oldSchedule.ID;
        this.Update(printInfo, pauseBeforeUpdating);
        if (this is Timer) Program.hueBridge.Timers[this.ID] = (Timer)this;
        else if (this is Alarm) Program.hueBridge.Alarms[this.ID] = (Alarm)this;
        // else: we have created this from a parent object and cannot add it to the bridge - no biggie
      }
      else {
        this.Create(printInfo, pauseBeforeUpdating);
        this.IsCreatedFromCode = false;
      }
    }
    #endregion
    #region Update
    public void Update(bool printInfo, bool pauseBeforeUpdating) {
      var recycle = this.CanBeAutoRecycled;
      this.CanBeAutoRecycled = null;
      Program.hueBridge.UpdateBridge("/schedules/" + ID + "/", this, printInfo, printInfo, pauseBeforeUpdating);
      this.CanBeAutoRecycled = recycle;
    }
    #endregion
    #region ### Create
    public void Create(bool printInfo, bool pauseBeforeUpdating) {
      if (this is Alarm) this.DeleteAfterRunning = null;
      if (this is Timer && this.DeleteAfterRunning == null) this.DeleteAfterRunning = false;
      var created = this.CreatedTime;
      this.CreatedTime = null;
      if (this.ID != null) throw new Exception("Schedule already created!");
      string id = Program.hueBridge.AddToBridge("/schedules/", this, printInfo, printInfo, pauseBeforeUpdating);
      this.ID = id;
      this.CreatedTime = created;
      if (this is Timer && Program.hueBridge.Timers.ContainsKey(this.ID)) Program.hueBridge.Timers[this.ID] = (Timer)this;
      else if (this is Alarm && Program.hueBridge.Alarms.ContainsKey(this.ID)) Program.hueBridge.Alarms[this.ID] = (Alarm)this;
      else if (this is Timer) Program.hueBridge.Timers.Add(this.ID, (Timer)this);
      else if (this is Alarm) Program.hueBridge.Alarms.Add(this.ID, (Alarm)this);
    }
    #endregion
    #region CopyFromOtherSchedule
    public void CopyFromOtherSchedule(Schedule source) {
      this.Action = source.Action;
      this.CanBeAutoRecycled = source.CanBeAutoRecycled;
      this.CreatedTime = source.CreatedTime;
      this.DeleteAfterRunning = source.DeleteAfterRunning;
      this.Description = source.Description;
      this.ID = source.ID;
      this.Name = source.Name;
      this.StartTime = source.StartTime;
      this.Status = source.Status;
      this.StrTime = source.StrTime;
    }
    #endregion
    #endregion
  }
}