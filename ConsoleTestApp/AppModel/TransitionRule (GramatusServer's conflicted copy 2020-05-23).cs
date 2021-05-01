using ConsoleTestApp.ApiObjects.Groups;
using ConsoleTestApp.ApiObjects.Lights;
using ConsoleTestApp.ApiObjects.Rules;
using ConsoleTestApp.ApiObjects.Rules.Actions;
using ConsoleTestApp.ApiObjects.Schedules;
using ConsoleTestApp.ApiObjects.Sensors;
using ConsoleTestApp.ApiObjects.Sensors.State;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ConsoleTestApp.AppModel {
  class TransitionRule : complexBridgeSetup {
    #region ### Instance fields
    private TimeSpan _startTime;
    private List<Alarm> alarms;
    private List<Timer> timers;
    private LightGroup _group;
    private string _commonDescription;
    private WeekDays _weekdays;
    private bool? _useAlarms;
    private SceneDefinitionList _defs;
    private List<TransitionGroup> groups = new List<TransitionGroup>();
    private WeekDays _days;
    private Sensor<IntSensorState> sceneNumberTracker;
    private Sensor<IntSensorState> _currentTransRuleTracker;
    private Sensor<BoolSensorState> _houseState;
    private int _transRuleID;
    private List<Transition> _transitions = new List<Transition>();
    #endregion
    #region ### Constructor
    public TransitionRule(string commonDescription, TimeSpan startTime, WeekDays weekdays, SceneDefinitionList sceneDefs, WeekDays days, Sensor<IntSensorState> currentTransRuleTracker, Sensor<BoolSensorState> houseState, int transRuleID) {
      this._commonDescription = "Fade" + commonDescription;
      this._startTime = startTime;
      this._weekdays = weekdays;
      this._useAlarms = null; // Use both (i.e. start with an alarm, and continue with triggers)
      this._defs = sceneDefs;
      this._days = days;
      this._currentTransRuleTracker = currentTransRuleTracker;
      this._houseState = houseState;
      this._transRuleID = transRuleID;
    }
    public TransitionRule(string commonDescription, TimeSpan startTime, WeekDays weekdays, string groupName, bool useAlarms) {
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
    }
    #endregion
    #region ### Instance methods
    #region CreateRulesFromSceneDefs
    private void CreateRulesFromSceneDefs(bool printInfo, bool pauseBeforeUpdating) {
      // if we have no scene ordered as #1, then the config is wrong...
      var startScene = this._defs.Values.FirstOrDefault(i => i.Order == 1);
      if (startScene == null) throw new ArgumentException("There should be a scene definition with order #1 in the file!");

      #region Create a sensor to keep track of how far we have come
      this.sceneNumberTracker = new Sensor<IntSensorState>("");
      this.sceneNumberTracker.Name = this._commonDescription + "_CurrentScene";
      this.sceneNumberTracker.State.State = 0;
      SceneCycleSetup.TestAddToBridge(true, this.sceneNumberTracker);
      #endregion


      double elapsedTime = 0;
      #region Create rules and other stuff to start it all off
      if (startScene.TransitionTime == null) throw new ArgumentException("Cannot create transitions if the scene defintions have no transition time! No transition time found for " + startScene.Name + startScene.Order);
      this.CreateRulesForOneSceneDefinition(startScene, this._commonDescription + " start", true, false, sceneNumberTracker, 1);
      elapsedTime += startScene.TransitionTime.Value;
      // elapsedTime.Add(TimeSpan.FromMilliseconds(startScene.TransitionTime.Value * 100);
      #endregion
      #region Create rules and other stuff for the remaining states
      var otherScenes = this._defs.Values.Where(i => i.Order != 1).ToList();
      for (int i = 0; i < otherScenes.Count; i++) {
        if (otherScenes[i].TransitionTime == null) throw new ArgumentException("Cannot create transitions if the scene defintions have no transition time! No transition time found for " + startScene.Name + startScene.Order);
        var ts = TimeSpan.FromMilliseconds(elapsedTime * 100);
        string transName = this._commonDescription + " +" + (ts.TotalHours > 0 ? ts.TotalHours + "t" : "") + ts.TotalMinutes + "min";
        this.CreateRulesForOneSceneDefinition(otherScenes[i], transName, false, i < (otherScenes.Count - 1), this.sceneNumberTracker, i + 2);
        elapsedTime += otherScenes[i].TransitionTime.Value;
      }
      #endregion
    }
    #endregion
    #region CreateRulesForOneSceneDefinition
    private void CreateRulesForOneSceneDefinition(SceneDefinition def, string transName, bool isInitialRule, bool isFinalRule, Sensor<IntSensorState> sceneNumberTracker, int currentOrder) {

      #region Make sure all groups have the necessary sensors
      foreach (var groupID in def.GroupList) {
        if (!Program.hueBridge.Groups.ContainsKey(groupID.ToString())) throw new ArgumentException("Group with ID " + groupID + " not found!");
        var tg = new TransitionGroup(Program.hueBridge.Groups[groupID.ToString()]);
        TransitionRule.TestAddToBridge(true, tg.reqChange);
        TransitionRule.TestAddToBridge(true, tg.isOn);
        TransitionRule.TestAddToBridge(true, tg.noTrans);
        this.groups.Add(tg);
      }
      #endregion
      if (isInitialRule) {
        // string transName = this._commonDescription + " " + this._startTime.ToString("HHmm");
        var firstTrans = new Transition(def, transName, this.groups, this._houseState, sceneNumberTracker, this._currentTransRuleTracker, this._transRuleID, currentOrder, isFinalRule, triggerTime: this._startTime, days: this._days);
      }
      else {
        var trans = new Transition(def, transName, this.groups, this._houseState, sceneNumberTracker, this._currentTransRuleTracker, this._transRuleID, currentOrder, isFinalRule);
      }
    }
    #endregion
    #region AddScene
    public void AddScene(SceneDefinition def) {
      this._defs.Add(def);
    }
    #endregion
    #region UpdateScenes
    private void UpdateScenes(bool printInfo, bool pauseBeforeUpdating) {
      // TODO: Update scene definitions in the bridge, and create those that are missing (based on name and/or ID)
      foreach (var def in this._defs) {
        def.Value.SaveSceneDefinitionToBridge(printInfo, pauseBeforeUpdating);
      }
    }
    #endregion
    #region SaveToBridge
    public void SaveToBridge(bool printInfo, bool pauseBeforeUpdating, bool deleteFirst) {
      if (deleteFirst) Program.hueBridge.CleanUpResourceGroup(this._commonDescription);
      if (this._useAlarms == null) AddOrUpdateToBridgeWithStartAlarmAndTimer(printInfo, pauseBeforeUpdating);
      else if (this._useAlarms.Value) this.AddOrUpdateToBridgeWithAlarms(printInfo, pauseBeforeUpdating);
      else this.AddOrUpdateToBridgeWithTimers(printInfo, pauseBeforeUpdating);
    }
    #endregion
    #region AddOrUpdateToBridgeWithStartAlarmAndTimer
    public void AddOrUpdateToBridgeWithStartAlarmAndTimer(bool printInfo, bool pauseBeforeUpdating) {
      this.UpdateScenes(printInfo, pauseBeforeUpdating);
      this.CreateRulesFromSceneDefs(printInfo, pauseBeforeUpdating);

      foreach (var group in this.groups) {
        group.SaveToBridge(printInfo, pauseBeforeUpdating);
      }
      foreach (var rule in this.rules) {
        Console.WriteLine("This should not happen?");
      }
      foreach(var trans in this._transitions) {
        trans.SaveToBridge();
      }

    }
    #endregion
    #region AddOrUpdateToBridgeWithTimers
    public void AddOrUpdateToBridgeWithTimers(bool printInfo, bool pauseBeforeUpdating) {
      this.UpdateScenes(printInfo, pauseBeforeUpdating);
      // TODO: Define the necessary timers

      // TODO: Check if the timers already exists

      // TODO: Create an alarm to start everything
    }
    #endregion
    #region AddOrUpdateToBridgeWithAlarms
    public void AddOrUpdateToBridgeWithAlarms(bool printInfo, bool pauseBeforeUpdating) {
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
    }
    #endregion
    #endregion
  }
  class Transition : complexBridgeSetup {
    #region ### Instance fields
    private Alarm Trigger;
    private Sensor<BoolSensorState> isTransStarting;
    private Timer nextSceneTrigger;
    #endregion
    #region ### Constructor
    public Transition(SceneDefinition def, string name, List<TransitionGroup> groups, Sensor<BoolSensorState> houseStatus, Sensor<IntSensorState> currentSceneTracker, Sensor<IntSensorState> currentTransRule, int transRuleID, int currentOrder, bool isFinalRule, TimeSpan? triggerTime = null, WeekDays days = WeekDays.Monday | WeekDays.Tuesday | WeekDays.Wednesday | WeekDays.Thursday | WeekDays.Friday) {
      #region Create one rule for each group
      foreach (var group in groups) {
        var rule = Rule.GetTransitionRule(name, group.Group.Name);
        rule.AddConditionTrigger(group.reqChange.Name, true);
        rule.AddConditionValueEquals(group.isOn.Name, true);
        rule.AddConditionValueEquals(houseStatus.Name, true);
        rule.AddConditionValueEquals(currentSceneTracker.Name, currentOrder);
        rule.AddConditionValueEquals(currentTransRule.Name, transRuleID);
        // Note: this will fail if the scene in the definiton has not been created in the bridge! Make sure that happens first...
        rule.AddActionSceneRecall(group.Group.Name, def.ID);
        rule.AddActionSetBoolSensorValue(group.reqChange.Name, false);
        this.rules.Add(rule);
      }
      #endregion
      #region Create a timer to trigger the next scene
      if (!isFinalRule) {
        this.nextSceneTrigger = new Timer(name + "_triggerNextScene", "Triggers the next scene after the transition time is finished", TimeSpan.FromMilliseconds(def.TransitionTime.Value * 100));
        this.nextSceneTrigger.Action = new IntSensorAction(currentSceneTracker.Name, currentOrder + 1);
        Transition.TestAddToBridge(true, this.nextSceneTrigger);
      }
      #endregion
      #region Create a rule to trigger all the other rules
      var startRule = Rule.GetTransitionRule(name, "!start");
      startRule.AddConditionTrigger(currentSceneTracker.Name, currentOrder);
      foreach (var group in groups) {
        startRule.AddActionSetBoolSensorValue(group.reqChange.Name, true);
      }
      startRule.AddActionStartTimer(this.nextSceneTrigger.Name);
      this.rules.Add(startRule);
      #endregion
      #region Only for the first scene in the list
      if (triggerTime != null) {
        #region (DEPRECATED?) Create a sensor that will indicate that the whole thing is starting
        // this.isTransStarting = new Sensor<BoolSensorState>("transStarter");
        // this.isTransStarting.Name = name + "_isTransStarting";
        // this.isTransStarting.State.State = false;
        // Transition.TestAddToBridge(true, this.isTransStarting);
        #endregion
        #region Create an alarm that will start the whole thing
        this.Trigger = new Alarm(name, "Starts a chain of events to change the light state over time", triggerTime.Value, days);
        this.Trigger.Action = new IntSensorAction(currentSceneTracker.Name, currentOrder);
        Transition.TestAddToBridge(true, this.Trigger);
        #endregion
      }
      #endregion
    }
    #endregion
    #region ### Instance methods
    #region SaveToBridge
    public void SaveToBridge(bool printInfo, bool pauseBeforeUpdating) {
      this.isTransStarting.CreateIfMissingHijackIfExisting(printInfo, pauseBeforeUpdating);
      this.Trigger?.CreateIfMissingHijackIfExisting(printInfo, pauseBeforeUpdating);
      this.nextSceneTrigger?.CreateIfMissingHijackIfExisting(printInfo, pauseBeforeUpdating);
    }
    #endregion
    #endregion
  }
  class TransitionGroup {
    #region ### Instance properties
    public LightGroup Group { get; set; }
    public Sensor<BoolSensorState> reqChange { get; set; }
    public Sensor<BoolSensorState> isOn { get; set; }
    public Sensor<BoolSensorState> noTrans { get; set; }
    #endregion
    #region ### Consctructor
    public TransitionGroup(LightGroup group) {
      this.Group = group;
      #region Create a flag that will be used to indicate that the group is ready to change state
      this.reqChange = new Sensor<BoolSensorState>("reqChange");
      this.reqChange.Name = this.Group.Name + "_reqChange";
      this.reqChange.State.State = false;
      #endregion
      #region Create a flag that will be used to indicate that the group is on (and thus can change state)
      this.isOn = new Sensor<BoolSensorState>("isOn");
      this.isOn.Name = this.Group.Name + "_isOn";
      this.isOn.State.State = false;
      #endregion
      #region Create a flag that will be used to indicate that the group should not follow transition changes (e.g. if I want to keep the light at a steady state even when transitions is ongoing)
      this.noTrans = new Sensor<BoolSensorState>("noTrans");
      this.noTrans.Name = this.Group.Name + " FadeAv";
      this.noTrans.State.State = false;
      #endregion
    }
    #endregion
    #region ### Instance methods
    #region SaveToBridge
    public void SaveToBridge(bool printInfo, bool pauseBeforeUpdating) {
      this.reqChange.CreateIfMissingHijackIfExisting(true, true, leaveExisting: true);
      this.isOn.CreateIfMissingHijackIfExisting(true, true, leaveExisting: true);
      this.noTrans.CreateIfMissingHijackIfExisting(true, true, leaveExisting: true);
    }
    #endregion
    #endregion
  }
}
