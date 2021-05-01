using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace ConsoleTestApp.ApiObjects.Groups {
  public enum AlertState {
    none,
    select,
    lselect
  }
  public class GroupSetAlertStateData : IApiObject {
    [JsonIgnore]
    public string ID { get => "n/a"; }
    [JsonIgnore]
    public string Name { get => "Setting alert state to " + this.State.ToString("g") + " for group " + this.GroupToAffect.Name; }
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public AlertState State { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public LightGroup GroupToAffect { get; set; }
    public static GroupSetAlertStateData GetAlertStateData(AlertState state, LightGroup group) {
      return new GroupSetAlertStateData() { State = state, GroupToAffect = group };
    }
  }
}
