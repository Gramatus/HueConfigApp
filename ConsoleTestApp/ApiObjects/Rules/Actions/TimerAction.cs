using ConsoleTestApp.ApiObjects.Schedules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;

namespace ConsoleTestApp.ApiObjects.Rules.Actions {
  public class TimerAction : RuleActionBase<ActionState> {
    protected Timer _timer;
    [JsonPropertyName("address")]
    public override string AddressOfAction {
      get => (this.IsScheduleAction ? Program.userAPIroot : "") + "/schedules/" + this._timer.ID;
      set {
        string id = value.Replace((this.IsScheduleAction ? Program.userAPIroot : "") + "/schedules/", "");
        // If not, we have some mess in the bridge - but it should not affect us too much
        if (Program.hueBridge.Timers.ContainsKey(id)) this._timer = Program.hueBridge.Timers[id];
      }
    }
    public TimerAction() {
      this.Method = ActionMethod.PUT;
      this.ActionData = new ActionState();
    }
    public TimerAction(Timer timer, EnabledState state) : this() {
      // if (Program.hueBridge.Timers.Any(i => i.Value.Name == timerName)) this._timer = Program.hueBridge.Timers.First(i => i.Value.Name == timerName).Value;
      // else throw new ArgumentOutOfRangeException("No timer named " + timerName + " found!");
      this._timer = timer;
      this.ActionData = new ActionState() { Status = state };
    }
    public override void WriteActionData(ApiBody data) {
      this.ActionData.Status = this.GetEnabledStateFromDataDict("status", data).Value;
    }
    protected override ApiBody ReadActionData() {
      this.SafeAddToDataDict("status", this.ActionData.Status.ToString(), true);
      return this._dataDict;
    }
  }
  public class ActionState {
    [JsonPropertyName("status")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EnabledState Status { get; set; }
  }
}
