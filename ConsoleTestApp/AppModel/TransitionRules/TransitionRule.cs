using ConsoleTestApp.ApiObjects.Groups;
using ConsoleTestApp.ApiObjects.Lights;
using ConsoleTestApp.ApiObjects.Schedules;
using ConsoleTestApp.ApiObjects.Sensors;
using ConsoleTestApp.ApiObjects.Sensors.State;
using ConsoleTestApp.AppModel.SwitchConfiguration;
using ConsoleTestApp.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ConsoleTestApp.AppModel.TransitionRules {
  class TransitionRule : complexBridgeSetup {
    #region ### Instance fields
    private TimeSpan _startTime;
    // private List<Alarm> alarms;
    // private List<Timer> timers;
    private LightGroup _group;
    private string _commonDescription;
    private WeekDays _weekdays;
    // private bool? _useAlarms;
    private List<SceneDefinition> _defs;
    private List<TransitionGroup> groups = new List<TransitionGroup>();
    private Sensor<IntSensorState> sceneNumberTracker;
    private Sensor<IntSensorState> _currentTransRuleTracker;
    private Sensor<BoolSensorState> _houseState;
    private int _transRuleID;
    private List<Transition> _transitions = new List<Transition>();
    #endregion
    #region ### Constructor
    public TransitionRule(string commonDescription, TimeSpan startTime, WeekDays weekdays, List<SceneDefinition> sceneDefs, Sensor<IntSensorState> currentTransRuleTracker, Sensor<BoolSensorState> houseState, int transRuleID) {
      _commonDescription = "Fade" + commonDescription;
      _startTime = startTime;
      _weekdays = weekdays;
      // this._useAlarms = null; // Use both (i.e. start with an alarm, and continue with triggers)
      _defs = sceneDefs;
      _currentTransRuleTracker = currentTransRuleTracker;
      _houseState = houseState;
      _transRuleID = transRuleID;
    }
    #region (DEPRECATED?)
    /*public TransitionRule(string commonDescription, TimeSpan startTime, WeekDays weekdays, string groupName, bool useAlarms) {
      var group = Program.hueBridge.Groups.First(i => i.Value.Name == groupName).Value;
      if (group == null) throw new ArgumentOutOfRangeException("No group named " + groupName + " found!");
      this._group = group;
      this._commonDescription = commonDescription;
      this._startTime = startTime;
      this._weekdays = weekdays;
      this._useAlarms = useAlarms;
    }
    public TransitionRule(string commonDescription, TimeSpan startTime, WeekDays weekdays, LightGroup group, bool useAlarms) {
      this._commonDescription = commonDescription;
      this._startTime = startTime;
      this._weekdays = weekdays;
      this._group = group;
      this._useAlarms = useAlarms;
    }*/
    #endregion
    #endregion
    #region ### Instance methods
    #region CreateRulesFromSceneDefs
    private void CreateRulesFromSceneDefs(bool stayAtFinalState, bool printInfo, bool pauseBeforeUpdating, bool createJsonOnlyOnSave = false) {
      // if we have no scene ordered as #1, then the config is wrong...
      // var startScene = this._defs.Values.FirstOrDefault(i => i.Order == 1);
      // if (startScene == null) throw new ArgumentException("There should be a scene definition with order #1 in the file!");

      #region Create a sensor to keep track of how far we have come
      sceneNumberTracker = new Sensor<IntSensorState>("");
      sceneNumberTracker.Name = _commonDescription.Truncate(32 - 13) + "_CurrentScene";
      sceneNumberTracker.State.State = 0;
      AddToBridgeDictionaries(sceneNumberTracker);
      #endregion

      #region (DEPRECATED?) Create rules and other stuff to start it all off
      // if (startScene.TransitionTime == null) throw new ArgumentException("Cannot create transitions if the scene defintions have no transition time! No transition time found for " + startScene.Name + startScene.Order);
      // this.CreateRulesForOneSceneDefinition(startScene, this._commonDescription + " start", true, false, sceneNumberTracker, 1);
      // elapsedTime += startScene.TransitionTime.Value;
      #endregion
      double elapsedTime = 0;
      var scenes = _defs.OrderBy(i => i.Order).ToList();
      // var otherScenes = this._defs.Values.Where(i => i.Order != 1).ToList();
      #region Create transitions for each scenedef
      var nameList = new List<string>();
      for (int i = 0; i < scenes.Count; i++) {
        if (scenes[i].TransitionTime == null) throw new ArgumentException("Cannot create transitions if the scene defintions have no transition time! No transition time found for " + scenes[i].Name + scenes[i].Order);
        var ts = TimeSpan.FromMilliseconds(elapsedTime * 100);
        string transName = _commonDescription;
        if (transName.Length > 15) transName = transName.Replace("Fade", "Fd").Truncate(15);
        if (i == 0) transName += " !start";
        else transName += " " + (ts.TotalHours > 0 ? ts.Hours + "t" : "") + ts.Minutes + "min";
        if (nameList.Contains(transName)) transName = transName + ".";
        nameList.Add(transName);
        CreateRulesForOneSceneDefinition(scenes[i], transName, i == 0, i == scenes.Count - 1, stayAtFinalState, sceneNumberTracker, i + 1, printInfo, pauseBeforeUpdating);
        elapsedTime += scenes[i].TransitionTime.Value;
      }
      #endregion
    }
    #endregion
    #region CreateRulesForOneSceneDefinition
    private void CreateRulesForOneSceneDefinition(SceneDefinition def, string transName, bool isInitialRule, bool isFinalRule, bool stayAtFinalState, Sensor<IntSensorState> sceneNumberTracker, int currentOrder, bool printInfo, bool pauseBeforeUpdating) {

      #region Make sure all groups have the necessary sensors
      foreach (var groupID in def.GroupList) {
        // We really just need one TransitionGroup for each group, but we are still looping here in case there are groups that are only mentioned in one sceneDef
        if (groups.Any(i => i.GroupID == groupID.ToString())) continue;
        if (!Program.hueBridge.Groups.ContainsKey(groupID.ToString())) throw new ArgumentException("Group with ID " + groupID + " not found!");
        var tg = new TransitionGroup(Program.hueBridge.Groups[groupID.ToString()]);
        // TransitionRule.TestAddToBridge(true, tg.reqChange);
        // TransitionRule.TestAddToBridge(true, tg.isOn);
        // TransitionRule.TestAddToBridge(true, tg.noTrans);
        groups.Add(tg);
      }
      var defGroups = groups.Where(i => def.GroupList.Select(g => g.ToString()).Contains(i.GroupID)).ToList();
      #endregion
      if (isInitialRule) {
        // stayAtFinalState = stayAtFinalState && !(isInitialRule && isFinalRule); // If this is both the initial and final rule, always revert to 0 (why?)
        _transitions.Add(new Transition(def, transName, defGroups, _houseState, sceneNumberTracker, _currentTransRuleTracker, _transRuleID, currentOrder, isFinalRule, stayAtFinalState, printInfo, pauseBeforeUpdating, triggerTime: _startTime, days: _weekdays));
      }
      else {
        _transitions.Add(new Transition(def, transName, defGroups, _houseState, sceneNumberTracker, _currentTransRuleTracker, _transRuleID, currentOrder, isFinalRule, stayAtFinalState, printInfo, pauseBeforeUpdating));
      }
    }
    #endregion
    #region AddScene
    public void AddScene(SceneDefinition def) {
      _defs.Add(def);
    }
    #endregion
    #region UpdateScenes
    /*private void UpdateScenes(bool printInfo, bool pauseBeforeUpdating) {
      // TODO: Update scene definitions in the bridge, and create those that are missing (based on name and/or ID)
      foreach (var trans in this._transitions) {
        trans.
      }
      foreach (var def in this._defs) {
        def.Value.SaveSceneDefinitionToBridge(printInfo, pauseBeforeUpdating);
      }
    }*/
    #endregion
    #region SaveToBridge
    public void SaveToBridge(bool stayAtFinalState, bool printInfo, bool pauseBeforeUpdating, bool deleteFirst, bool scenesOnly = false, bool createJsonOnlyOnSave = false) {
      if (deleteFirst) Program.hueBridge.CleanUpResourceGroup(_commonDescription, false, true);

      // Note: scenes are updated as part of creating the transition...
      // this.UpdateScenes(printInfo, pauseBeforeUpdating);
      CreateRulesFromSceneDefs(stayAtFinalState, printInfo, pauseBeforeUpdating, createJsonOnlyOnSave);

      if (scenesOnly) return;
      sceneNumberTracker.CreateIfMissingHijackIfExisting(printInfo, pauseBeforeUpdating);

      foreach (var group in groups) {
        group.SaveToBridge(printInfo, pauseBeforeUpdating);
      }
      foreach (var rule in rules) {
        Console.WriteLine("This should not happen?");
      }
      foreach (var trans in _transitions) {
        trans.SaveToBridge(printInfo, pauseBeforeUpdating);
      }

      // if (this._useAlarms == null)
      // AddOrUpdateToBridgeWithStartAlarmAndTimer(printInfo, pauseBeforeUpdating);
      // else if (this._useAlarms.Value) this.AddOrUpdateToBridgeWithAlarms(printInfo, pauseBeforeUpdating);
      // else this.AddOrUpdateToBridgeWithTimers(printInfo, pauseBeforeUpdating);
    }
    #endregion
    #region AddOrUpdateToBridgeWithStartAlarmAndTimer
    /*public void AddOrUpdateToBridgeWithStartAlarmAndTimer(bool printInfo, bool pauseBeforeUpdating) {

    }*/
    #endregion
    #region (DEPRECATED?) AddOrUpdateToBridgeWithTimers
    /*public void AddOrUpdateToBridgeWithTimers(bool printInfo, bool pauseBeforeUpdating) {
      this.UpdateScenes(printInfo, pauseBeforeUpdating);
      // TODO: Define the necessary timers

      // TODO: Check if the timers already exists

      // TODO: Create an alarm to start everything
    }*/
    #endregion
    #region (DEPRECATED?) AddOrUpdateToBridgeWithAlarms
    /*public void AddOrUpdateToBridgeWithAlarms(bool printInfo, bool pauseBeforeUpdating) {
      this.UpdateScenes(printInfo, pauseBeforeUpdating);
      this.alarms = new List<Alarm>();
      // TODO: Define the necessary alarms
      TimeSpan nextAlarm = this._startTime;
      foreach (var sceneDef in this._defs.Values) {
        var alarm = new Alarm();
        alarm.Name = this._commonDescription + nextAlarm.ToString("HHmm");
        alarm.RecurringAlarmTime = nextAlarm;
        alarm.Action = new TriggerSceneAction(this._group, sceneDef.ID);
        this.alarms.Add(alarm);
        nextAlarm = nextAlarm.Add(TimeSpan.FromMilliseconds((sceneDef.TransitionTime * 100) ?? 4));
      }
      // TODO: Check if the alarms already exists (i.e. alarms with the same name), and create does that does not exist
      // TODO: Update bridge according to this setup
    }*/
    #endregion
    #endregion
  }
}
