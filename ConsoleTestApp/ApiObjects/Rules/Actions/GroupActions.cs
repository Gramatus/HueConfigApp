using ConsoleTestApp.ApiObjects.Groups;
using ConsoleTestApp.ApiObjects.Lights.State;
using ConsoleTestApp.ApiObjects.Scenes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace ConsoleTestApp.ApiObjects.Rules.Actions {
  #region TriggerStateAction
  public class TriggerAlertAction : GroupActionBase<GroupSetAlertStateData> {
    public TriggerAlertAction() : base((LightGroup)null) { }
    public TriggerAlertAction(string groupName, AlertState state) : base(groupName) {
      this.ActionData = GroupSetAlertStateData.GetAlertStateData(state, this._group);
    }
    public TriggerAlertAction(LightGroup group, AlertState state) : base(group) {
      this.ActionData = GroupSetAlertStateData.GetAlertStateData(state, this._group);
    }
    public override void WriteActionData(ApiBody data) {
      var state = this.GetAlertStateFromDataDict("alert", data, AlertState.none);
      if (this.ActionData == null) this.ActionData = GroupSetAlertStateData.GetAlertStateData(state, this._group);
      else this.ActionData.State = state;
    }
    protected override ApiBody ReadActionData() {
      this.SafeAddToDataDict("alert", this.ActionData.State.ToString("g"), true);
      return this._dataDict;
    }
  }
  #endregion
  #region TriggerStateAction
  public class TriggerStateAction : GroupActionBase<LightStateChanger> {
    public TriggerStateAction() : base((LightGroup)null) { }
    public TriggerStateAction(string groupName, LightStateChanger state) : base(groupName) {
      this.ActionData = state;
    }
    public TriggerStateAction(LightGroup group, LightStateChanger state) : base(group) {
      this.ActionData = state;
    }
    public override void WriteActionData(ApiBody data) {
      if (this.ActionData == null) this.ActionData = new LightStateChanger();
      this.ActionData.ColorTemperature = this.GetIntFromDataDict("ct", data, true);
      this.ActionData.Brightness = this.GetIntFromDataDict("bri", data, true);
      this.ActionData.IsOn = this.GetBoolFromDataDict("on", data, null);
      // TODO: Add handling of changing values ("bri_inc", etc.)!
      this.ActionData.ChangeBrightness = this.GetIntFromDataDict("bri_inc", data, true);
      this.ActionData.ChangeColorTemperature = this.GetIntFromDataDict("ct_inc", data, true);
      this.ActionData.ChangeHue = this.GetIntFromDataDict("hue_inc", data, true);
      this.ActionData.ChangeSaturation = this.GetIntFromDataDict("sat_inc", data, true);
      // this.ActionData.ChangeXY = this.GetIntFromDataDict("xy_inc", data, true);
    }
    protected override ApiBody ReadActionData() {
      this.SafeAddToDataDict("ct", this.ActionData.ColorTemperature, false);
      this.SafeAddToDataDict("bri", this.ActionData.Brightness, false);
      this.SafeAddToDataDict("on", this.ActionData.IsOn, false);
      this.SafeAddToDataDict("bri_inc", this.ActionData.ChangeBrightness, false);
      this.SafeAddToDataDict("ct_inc", this.ActionData.ChangeColorTemperature, false);
      this.SafeAddToDataDict("hue_inc", this.ActionData.ChangeHue, false);
      this.SafeAddToDataDict("sat_inc", this.ActionData.ChangeSaturation, false);
      return this._dataDict;
    }
  }
  #endregion
  #region TriggerSceneAction
  public class TriggerSceneAction : GroupActionBase<GroupRecallSceneData> {
    public TriggerSceneAction() : base((LightGroup)null) { }
    public TriggerSceneAction(string groupName, string sceneName) : base(groupName) {
      // if (Program.hueBridge.Groups.Any(i => i.Value.Name == groupName)) this._group = Program.hueBridge.Groups.First(i => i.Value.Name == groupName).Value;
      // else throw new ArgumentOutOfRangeException("No group named " + groupName + " found!");
      var groupScene = Program.hueBridge.Scenes.Values.FirstOrDefault(i => i.Name == sceneName && i.GroupID == this._group.ID);
      var groupZeroScene = Program.hueBridge.Scenes.Values.FirstOrDefault(i => i.Name == sceneName && i.GroupID == "0");
      var lightScene = Program.hueBridge.Scenes.Values.FirstOrDefault(i => i.Name == sceneName && i.SceneType == "LightScene");
      var scene = groupScene ?? groupZeroScene ?? lightScene;
      if (scene == null && this._group.ID == "0") {
        // If we are targeting the "all lights" group, and no scene is found in that group that matches (and no matching Lightscene is found), look for the FIRST scene with a matching name at all (this will only work reliably if there are only one scene with that name)
        scene = Program.hueBridge.Scenes.Values.FirstOrDefault(i => i.Name == sceneName);
        if (scene != null) Console.WriteLine("Found no exact match, however since we are targeting \"all lights\", an extra search found a scene named \"" + scene.Name + "\", targeting group \"" + scene.GroupName + "\", will try to use that");
      }
      if (scene != null) this.ActionData = GroupRecallSceneData.GetRecallSceneData(scene, this._group);
      else throw new ArgumentOutOfRangeException("No scene named " + sceneName + " found that can trigger lights in group " + groupName + "!");
    }
    public TriggerSceneAction(string groupName, Scene scene) : base(groupName) {
      this.ActionData = GroupRecallSceneData.GetRecallSceneData(scene, this._group);
    }
    public TriggerSceneAction(LightGroup group, Scene scene) : base(group) {
      this.ActionData = GroupRecallSceneData.GetRecallSceneData(scene, this._group);
    }
    public TriggerSceneAction(LightGroup group, string sceneID) : base(group) {
      if (Program.hueBridge.Scenes.ContainsKey(sceneID)) this.ActionData = GroupRecallSceneData.GetRecallSceneData(Program.hueBridge.Scenes[sceneID], this._group);
      else throw new ArgumentOutOfRangeException("No scene with ID " + sceneID + " found!");
    }
    public override void WriteActionData(ApiBody data) {
      if (this.ActionData == null) this.ActionData = new GroupRecallSceneData();
      this.ActionData.Scene = this.GetStringFromDataDict("scene", data);
    }
    protected override ApiBody ReadActionData() {
      // if (this._dataDict.ContainsKey("scene")) this._dataDict["scene"] = this.ActionData.Scene;
      // else this._dataDict.Add("scene", this.ActionData.Scene);
      this.SafeAddToDataDict("scene", this.ActionData.Scene, true);
      return this._dataDict;
    }
  }
  #endregion
  #region GroupAction
  public abstract class GroupActionBase<T> : RuleActionBase<T> {
    protected LightGroup _group;
    [JsonPropertyName("address")]
    public override string AddressOfAction {
      get => (this.IsScheduleAction ? Program.userAPIroot : "") + "/groups/" + _group.ID + "/action";
      set => _group = Program.hueBridge.Groups[value.Replace((this.IsScheduleAction ? Program.userAPIroot : "") + "/groups/", "").Replace("/action", "")];
    }
    protected GroupActionBase(string groupName) {
      this.Method = ActionMethod.PUT;
      if (Program.hueBridge.Groups.Any(i => i.Value.Name == groupName)) this._group = Program.hueBridge.Groups.First(i => i.Value.Name == groupName).Value;
      else throw new ArgumentOutOfRangeException("No group named " + groupName + " found!");
    }
    protected GroupActionBase(LightGroup group) {
      this.Method = ActionMethod.PUT;
      this._group = group;
    }
  }
  #endregion
}
