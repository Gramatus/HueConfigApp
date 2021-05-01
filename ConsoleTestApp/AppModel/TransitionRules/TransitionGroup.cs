using ConsoleTestApp.ApiObjects.Groups;
using ConsoleTestApp.ApiObjects.Sensors;
using ConsoleTestApp.ApiObjects.Sensors.State;
using ConsoleTestApp.Helpers;
using System;
using System.Linq;

namespace ConsoleTestApp.AppModel {
  class TransitionGroup : complexBridgeSetup {
    #region ### Instance properties
    public LightGroup Group { get; set; }
    public string GroupID { get => this.Group.ID; }
    public string GroupName { get => this.Group.Name; }
    public Sensor<BoolSensorState> reqChange { get; set; }
    public Sensor<BoolSensorState> isOn { get; set; }
    public Sensor<BoolSensorState> noTrans { get; set; }
    #endregion
    #region ### Consctructor
    public TransitionGroup(string groupName) {
      var group = Program.hueBridge.Groups.Values.FirstOrDefault(i => i.Name == groupName);
      if (group == null) throw new ArgumentException("Group with name " + groupName + " not found!");
      this.Group = group;
      this.Setup();
    }
    public TransitionGroup(LightGroup group) {
      this.Group = group;
      this.Setup();
    }
    private void Setup() {
      #region Create a flag that will be used to indicate that the group is ready to change state
      this.reqChange = new Sensor<BoolSensorState>("reqChange");
      this.reqChange.Name = this.Group.Name.Replace(" (\"hemmelig\" systemgruppe)", "").Truncate(32 - "_reqChange".Length) + "_reqChange";
      this.reqChange.State.State = false;
      #endregion
      #region Create a flag that will be used to indicate that the group is on (and thus can change state)
      this.isOn = new Sensor<BoolSensorState>("isOn");
      this.isOn.Name = this.Group.Name.Replace(" (\"hemmelig\" systemgruppe)", "").Truncate(32 - "_isOn".Length) + "_isOn";
      this.isOn.State.State = true;
      #endregion
      #region Create a flag that will be used to indicate that the group should not follow transition changes (e.g. if I want to keep the light at a steady state even when transitions is ongoing)
      this.noTrans = new Sensor<BoolSensorState>("noTrans");
      this.noTrans.Name = "!FadeAv " + this.Group.Name.Replace(" (\"hemmelig\" systemgruppe)", "").Truncate(32 - "!FadeAv ".Length);
      this.noTrans.State.State = false;
      #endregion
      TransitionGroup.AddToBridgeDictionaries(this.reqChange);
      TransitionGroup.AddToBridgeDictionaries(this.isOn);
      TransitionGroup.AddToBridgeDictionaries(this.noTrans);
    }
    #endregion
    #region ### Instance methods
    #region SaveToBridge
    public void SaveToBridge(bool printInfo, bool pauseBeforeUpdating) {
      this.reqChange.CreateIfMissingHijackIfExisting(printInfo, pauseBeforeUpdating, leaveExisting: true);
      this.isOn.CreateIfMissingHijackIfExisting(printInfo, pauseBeforeUpdating, leaveExisting: true);
      this.noTrans.CreateIfMissingHijackIfExisting(printInfo, pauseBeforeUpdating, leaveExisting: true);
    }
    #endregion
    #endregion
  }
}
