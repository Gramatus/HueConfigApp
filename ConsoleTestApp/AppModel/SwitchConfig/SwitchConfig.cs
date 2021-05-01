using ConsoleTestApp.ApiObjects.Groups;
using ConsoleTestApp.ApiObjects.Lights.State;
using ConsoleTestApp.ApiObjects.Rules;
using ConsoleTestApp.ApiObjects.Rules.Actions;
using ConsoleTestApp.ApiObjects.Schedules;
using ConsoleTestApp.ApiObjects.Sensors;
using ConsoleTestApp.ApiObjects.Sensors.State;
using ConsoleTestApp.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ConsoleTestApp.AppModel.SwitchConfiguration {
  class SwitchConfig : Dictionary<dButton, Dictionary<bState, Dictionary<string, Rule>>> {
    #region ### Instance fields
    public Sensor _switch { get; private set; }
    private LightGroup _group;
    private Dictionary<dButton, Dictionary<bState, Dictionary<string, SceneCycleSetup>>> cycleSetups;
    private Dictionary<dButton, Dictionary<bState, Dictionary<string, SceneMultiSetup>>> multiSetups;
    private List<TransitionGroup> groups = new List<TransitionGroup>();
    public Sensor<IntSensorState> complexStateTracker { get; set; }
    private List<Rule> additionalRules = new List<Rule>();
    #endregion
    #region Constructor
    public SwitchConfig(string switchName, string groupName) : this() {
      var button = Program.hueBridge.Sensors.First(i => i.Value.Name == switchName).Value;
      if (button == null) throw new ArgumentNullException("No button with name " + switchName + " found");
      else this._switch = button;
      var group = Program.hueBridge.Groups.FirstOrDefault(i => i.Value.Name == groupName).Value;
      if (group == null) throw new ArgumentNullException("No group with name " + groupName + " found");
      else this._group = group;
      // TransitionRule.TestAddToBridge(true, tg.reqChange);
      // TransitionRule.TestAddToBridge(true, tg.isOn);
      // TransitionRule.TestAddToBridge(true, tg.noTrans);
      this.groups.Add(new TransitionGroup(this._group));
    }
    public SwitchConfig(Sensor button) : this() {
      _switch = button;
    }
    private SwitchConfig() {
      cycleSetups = new Dictionary<dButton, Dictionary<bState, Dictionary<string, SceneCycleSetup>>>();
      multiSetups = new Dictionary<dButton, Dictionary<bState, Dictionary<string, SceneMultiSetup>>>();
    }
    #endregion
    #region ### Instance methods
    #region ## Setup buttons
    #region SetToSingleScene
    public void SetToSingleScene(dButton button, bState state, string sceneName, string ruleID = "", Sensor<BoolSensorState> sensorThatMustBeTrue = null, bool sensorShouldBe = true, string groupName = null, string altBtnName = null, bool triggerTrans = true, bool? disableTrans = null) {
      var tg = this.GetTransitionGroup(groupName ?? this._group.Name);
      var rule = SwitchConfigHelpers.GetBasicTurnOnRule(this._switch, button, state, ruleID1: ruleID, ruleID2: "", tg, sensorThatMustBeTrue, sensorShouldBe, altBtnName, triggerTrans: triggerTrans, disableTrans: disableTrans);
      rule.AddActionSceneRecall(groupName ?? _group.Name, sceneName);
      AddSingleRuleButtonState(button, state, rule, ruleID);
    }
    #endregion
    #region SetToReallyComplexSetup
    public void SetToReallyComplexSetup(dButton button, bState state, string groupName, sceneButton[] options, bool turnOn, int timeout, string multiID = "", Sensor<BoolSensorState> sensorThatMustBeTrue = null, bool sensorShouldBe = true, string altBtnName = null, SimpleChangeOptions simpleChange = null) {
      var tg = this.PrepareReallyComplexSetup(groupName);
      var setup = SceneMultiSetup.Create(this._switch, button, state, tg, this.complexStateTracker, this.getNextCounter(), options, turnOn, timeout, altBtnName: altBtnName, simpleChange: simpleChange, multiID: multiID);
      this.FinishReallyComplexSetup(sensorThatMustBeTrue, sensorShouldBe, setup, button, state, multiID);
    }
    public SceneMultiSetup SetToReallyComplexSetup(dButton button, bState state, string groupName, sceneButton[] options, bool turnOn, int timeout, bool triggerTransOnMainButton, bool? disableTrans, string multiID = "", Sensor<BoolSensorState> sensorThatMustBeTrue = null, bool sensorShouldBe = true, string altBtnName = null) {
      var tg = this.PrepareReallyComplexSetup(groupName);
      var setup = SceneMultiSetup.Create(this._switch, button, state, tg, this.complexStateTracker, this.getNextCounter(), options, turnOn, timeout, triggerTransOnMainButton: triggerTransOnMainButton, disableTrans: disableTrans, altBtnName: altBtnName, multiID: multiID);
      this.FinishReallyComplexSetup(sensorThatMustBeTrue, sensorShouldBe, setup, button, state, multiID);
      return setup;
    }
    private void FinishReallyComplexSetup(Sensor<BoolSensorState> sensorThatMustBeTrue, bool sensorShouldBe, SceneMultiSetup setup, dButton button, bState state, string multiID) {
      if (sensorThatMustBeTrue != null) setup.AddConditionValueEquals(sensorThatMustBeTrue, sensorShouldBe);
      AddSceneMultiButtonState(button, state, setup, "Multi" + multiID);
    }
    private int getNextCounter() {
      return this.multiSetups.SelectMany(i => i.Value).SelectMany(i => i.Value).Count() + 1;
    }
    private TransitionGroup PrepareReallyComplexSetup(string groupName) {
      if (groupName != null) this.groups.Add(new TransitionGroup(groupName));
      if (this.complexStateTracker == null) {
        // Add a sensor to keep track of the complex state of things
        this.complexStateTracker = new Sensor<IntSensorState>("");
        string safeName = this._switch.Name.Truncate(32 - 15).FixNorwegianChars();
        this.complexStateTracker.Name = safeName + "_ComplexTracker";
        this.complexStateTracker.State.State = 0;
      }
      return this.GetTransitionGroup(groupName ?? this._group.Name);
    }
    #endregion
    #region SetToOnAndThenScenes
    public Sensor<BoolSensorState> SetToOnAndThenScenes(dButton button, bState state, string[] sceneNames, string cycleID, bState? cycleState = null, Sensor<BoolSensorState> sensorThatMustBeTrue = null, bool sensorShouldBe = true, string groupName = null, string altBtnName = null, bool delayedStart = false) {
      return this.SetToScenes(button, state, sceneNames, true, cycleID, cycleState, sensorThatMustBeTrue, sensorShouldBe, groupName, altBtnName, delayedStart);
      /*if (groupName != null) this.groups.Add(new TransitionGroup(groupName));
      var setup = new SceneCycleSetup(_switch.Name, button, state, groupName ?? _group.Name, this.groups, sceneNames, cycleState: cycleState, addToBridgeWithoutSaving: true, altBtnName: altBtnName, delayedStart: delayedStart);
      if (sensorThatMustBeTrue != null) setup.AddConditionValueEquals(sensorThatMustBeTrue, sensorShouldBe);
      AddSceneCycleButtonState(button, state, setup, ruleID);
      return setup.currentlyCyclingFlag;*/
    }
    #endregion
    #region SetToScenes
    public Sensor<BoolSensorState> SetToScenes(dButton button, bState state, string[] sceneNames, bool setToOnFirst, string cycleID, bState? cycleState = null, Sensor<BoolSensorState> sensorThatMustBeTrue = null, bool sensorShouldBe = true, string groupName = null, string altBtnName = null, bool delayedStart = false, bool triggerTrans = true, bool? disableTrans = null, bool signalCycleStart = false) {
      if (groupName != null) this.groups.Add(new TransitionGroup(groupName));
      var tg = this.GetTransitionGroup(groupName ?? this._group.Name);
      var setup = new SceneCycleSetup(_switch, button, state, tg, sceneNames, setToOnFirst, cycleState: cycleState, altBtnName: altBtnName, delayedStart: delayedStart, triggerTrans: triggerTrans, disableTrans: disableTrans, cycleID: cycleID, signalCycleStart: signalCycleStart);
      if (sensorThatMustBeTrue != null) setup.AddConditionValueEquals(sensorThatMustBeTrue, sensorShouldBe);
      AddSceneCycleButtonState(button, state, setup, cycleID);
      return setup.currentlyCyclingFlag;
    }
    #endregion
    #region SetToTriggerTimerTransition
    public void SetToTriggerTimerTransition(dButton button, bState state, string ruleID = "", Sensor<BoolSensorState> sensorThatMustBeTrue = null, bool sensorShouldBe = true, string groupName = null, string altBtnName = null) {
      Console.WriteLine(">>> Switch " + this._switch.Name + " uses NOT IMPLEMENTED functionality of triggering a transition timer");
    }
    #endregion
    #region SetToTurnOff
    public Rule SetToTurnOff(dButton button, bState state, Sensor<BoolSensorState> sensorThatMustBeTrue = null, bool sensorShouldBe = true, string groupName = null, string altBtnName = null, string ruleID = "Skru av", string ruleID2 = "") {
      var tg = this.GetTransitionGroup(groupName ?? this._group.Name);
      var rule = SwitchConfigHelpers.GetBasicTurnOffRule(this._switch, button, state, ruleID1: ruleID, ruleID2: ruleID2, tg, sensorThatMustBeTrue, sensorShouldBe, altBtnName);
      AddSingleRuleButtonState(button, state, rule, "Skru av");
      return rule;
    }
    #endregion
    #region SetToTurnOffMultipleGroupsAndTimers
    public void SetToTurnOffMultipleGroupsAndTimers(dButton button, bState state, string[] groupNames, Timer[] timers, string ruleID, Sensor<BoolSensorState> sensorThatMustBeTrue = null, bool sensorShouldBe = true, string altBtnName = null) {
      Rule rule = null;
      foreach (var groupName in groupNames) {
        var tg = this.GetTransitionGroup(groupName);
        if (rule == null) {
          rule = SwitchConfigHelpers.GetBasicTurnOffRule(this._switch, button, state, ruleID1: ruleID, ruleID2: "", tg, sensorThatMustBeTrue, sensorShouldBe, altBtnName);
        }
        else {
          rule.AddActionSetGroupState(tg.GroupName, new LightStateChanger { IsOn = false });
          rule.AddActionSetBoolSensorValue(tg.isOn, false);
        }
      }
      foreach (var timer in timers) {
        rule.AddActionStopTimer(timer);
      }
      AddSingleRuleButtonState(button, state, rule, ruleID);
    }
    #endregion
    #region Trans On/Off
    public Rule SetToEnableTrans(dButton button, bState state, string groupName = null) {
      var tg = this.GetTransitionGroup(groupName ?? this._group.Name);
      var rule1 = SwitchConfigHelpers.GetChangeTransStateRule(this._switch, button, state, tg, true, "Fading", "Paa");
      rule1.AddActionSetGroupAlertState(tg.GroupName, AlertState.lselect);
      var rule2 = new Rule("TransEnabled " + tg.GroupName + " ack done");
      rule2.AddConditionDelayedTrigger(tg.noTrans, false, new TimeSpan(0, 0, 1));
      rule2.AddActionSetGroupAlertState(tg.GroupName, AlertState.none);
      AddSingleRuleButtonState(button, state, rule1, "Fading");
      this.additionalRules.Add(rule2);
      return rule1;
    }
    public Rule SetToDisableTrans(dButton button, bState state, string groupName = null) {
      var tg = this.GetTransitionGroup(groupName ?? this._group.Name);
      var rule = SwitchConfigHelpers.GetChangeTransStateRule(this._switch, button, state, tg, false, "Fading", "Av");
      rule.AddActionSetGroupAlertState(tg.GroupName, AlertState.select);
      AddSingleRuleButtonState(button, state, rule, "Fading");
      return rule;
    }
    #endregion
    #region SetToTurnOn
    public void SetToTurnOn(dButton button, bState state, string ruleID = "", Sensor<BoolSensorState> sensorThatMustBeTrue = null, bool sensorShouldBe = true, string groupName = null, string altBtnName = null, bool triggerTrans = true, bool? disableTrans = null, string ruleID2 = "") {
      var tg = this.GetTransitionGroup(groupName ?? this._group.Name);
      var rule = SwitchConfigHelpers.GetBasicTurnOnRule(this._switch, button, state, ruleID1: ruleID, ruleID2: ruleID2, tg, sensorThatMustBeTrue, sensorShouldBe, altBtnName, triggerTrans: triggerTrans, disableTrans: disableTrans);
      AddSingleRuleButtonState(button, state, rule, ruleID);
    }
    #endregion
    #region SetToStandardDimUp
    public void SetToStandardDimUp(dButton button, string ruleID = "") {
      this.SetToStandardDimChange(button, true, ruleID: ruleID);
    }
    public void SetToStandardDimDown(dButton button, string ruleID = "") {
      this.SetToStandardDimChange(button, false, ruleID: ruleID);
    }
    public void SetToConditionalDimChange<T>(dButton button, bState state, bool dimUp, Sensor condition, T requiredState, string ruleID, string altBtnName = null) {
      var rule = Rule.GetButtonRule(_switch, button, state, altBtnName: altBtnName, ruleID1: ruleID);
      rule.AddConditionButtonTrigger(_switch, button, state);
      rule.AddConditionValueEquals(condition, requiredState);
      rule.AddActionSetGroupState(_group.Name, new LightStateChanger { ChangeBrightness = dimUp ? 30 : -30 });
      AddSingleRuleButtonState(button, bState.short_release, rule, ruleID);
    }
    public void SetToStandardDimChange(dButton button, bool dimUp, string ruleID, string altBtnName = null) {
      var tg = this.GetTransitionGroup(this._group.Name);
      /*var rule1 = Rule.GetButtonRule(_switch, button, bState.short_release, altBtnName: altBtnName, ruleID1: ruleID);
      rule1.AddConditionButtonTrigger(_switch, button, bState.short_release);
      rule1.AddConditionValueEquals(tg.isOn, true);
      rule1.AddActionSetGroupState(_group.Name, new LightStateChanger { ChangeBrightness = dimUp ? 30 : -30 });
      AddSingleRuleButtonState(button, bState.short_release, rule1, ruleID);*/
      var rule2 = Rule.GetButtonRule(_switch, button, bState.repeat, altBtnName: altBtnName, ruleID1: ruleID);
      rule2.AddConditionButtonTrigger(_switch, button, bState.repeat);
      rule2.AddConditionValueEquals(tg.isOn, true);
      rule2.AddActionSetGroupState(_group.Name, new LightStateChanger { ChangeBrightness = dimUp ? 56 : -56 });
      AddSingleRuleButtonState(button, bState.repeat, rule2, ruleID);
    }
    #endregion
    #region AddSetBoolSensorValueAction
    /// <summary>Used to add extra actions after a buttonconfig has been created.</summary>
    /// <param name="button"></param>
    /// <param name="state"></param>
    /// <param name="ruleID"></param>
    /// <param name="sensorName"></param>
    /// <param name="value"></param>
    public void AddSetBoolSensorValueAction(dButton button, bState state, string ruleID, Sensor<BoolSensorState> sensor, bool value) {
      if (!CheckButtonStateAdded(button, state, ruleID)) throw new ArgumentOutOfRangeException("Additional actions can only be added after the button state has been added to the config!");
      this[button][state][ruleID].Actions.Add(new BoolSensorAction(sensor, value, false));
    }
    #endregion
    #region SaveToBridge
    public void SaveToBridge(bool printInfo, bool pauseBeforeUpdating, bool deleteFirst, bool pauseBeforeDeleting) {
      if (deleteFirst) Program.hueBridge.CleanUpResourceGroup(_switch.Name, exactMatch: false, pauseBeforeDeleting: pauseBeforeDeleting);

      if (this.complexStateTracker != null) {
        this.complexStateTracker.CreateIfMissingHijackIfExisting(printInfo, pauseBeforeUpdating);
      }
      foreach (var group in this.groups) {
        group.SaveToBridge(printInfo, pauseBeforeUpdating);
      }
      var rules = Values.SelectMany(i => i.Values).SelectMany(i => i.Values);
      foreach (var rule in rules) {
        if (this.complexStateTracker != null) {
          // Then we need to keep track of things and only react to the normal buttons when we are not in a complex state situation. If the tracker for complex state is 0, then normal rules should apply.
          rule.AddConditionValueEquals(this.complexStateTracker, 0);
        }
      }
      var cycleSetups = this.cycleSetups.Values.SelectMany(i => i.Values).SelectMany(i => i.Values);
      foreach (var setup in cycleSetups) {
        rules = rules.Concat(setup.SaveToBridge(this.complexStateTracker, 0, printInfo, pauseBeforeUpdating));
      }
      var multiSetups = this.multiSetups.Values.SelectMany(i => i.Values).SelectMany(i => i.Values);
      int i = 1;
      foreach (var setup in multiSetups) {
        rules = rules.Concat(setup.SaveToBridge(this.complexStateTracker, i, printInfo, pauseBeforeUpdating));
      }
      // Handle names
      Rule.SetButtonRuleNames(rules.Where(i => i.IsButtonRule));

      // Handle additional rules (pt. used for rules that stops blinking after 1 second when transitions are enabled)
      rules = rules.Concat(this.additionalRules);

      // rules = rules.Concat(Values.SelectMany(i => i.Values).SelectMany(i => i.Values)).ToList();
      foreach (var rule in rules) {
        // If there already exists a rule in the bridge with this name, we "hijack" it (hopefully it is supposed to be the same rule - since this class has clearn naming conventions), and send an update from our new rule
        rule.CreateIfMissingHijackIfExisting(printInfo, pauseBeforeUpdating);
      }
    }
    #endregion
    #endregion
    #region ## Helpers
    public TransitionGroup GetTransitionGroup(string groupName) {
      var tg = this.groups.FirstOrDefault(i => i.GroupName == groupName);
      if (tg == null) {
        tg = new TransitionGroup(groupName);
        groups.Add(tg);
      }
      return tg;
    }
    #region CheckAndAddKeys
    private enum ruleType {
      SingleRule,
      SceneCycle,
      SceneMulti
    }
    private void CheckAndAddKeys(dButton button, bState state, string ruleID, ruleType type) {
      if (type == ruleType.SingleRule) {
        if (cycleSetups.ContainsKey(button) && cycleSetups[button].ContainsKey(state) && cycleSetups[button][state].ContainsKey(ruleID)) throw new ArgumentException("Trying to add a single rule for " + button + "/" + state + " with ID " + ruleID + ", but there already exists a cycle setup with that key combination");
        if (multiSetups.ContainsKey(button) && multiSetups[button].ContainsKey(state) && multiSetups[button][state].ContainsKey(ruleID)) throw new ArgumentException("Trying to add a single rule for " + button + "/" + state + " with ID " + ruleID + ", but there already exists a multi setup with that key combination");
        if (!ContainsKey(button)) Add(button, new Dictionary<bState, Dictionary<string, Rule>>());
        if (!this[button].ContainsKey(state)) this[button].Add(state, new Dictionary<string, Rule>());
      }
      else if (type == ruleType.SceneCycle) {
        if (ContainsKey(button) && this[button].ContainsKey(state) && this[button][state].ContainsKey(ruleID)) throw new ArgumentException("Trying to add a cycle setup for " + button + "/" + state + " with ID " + ruleID + ", but there already exists a single rule with that key combination");
        if (multiSetups.ContainsKey(button) && multiSetups[button].ContainsKey(state) && multiSetups[button][state].ContainsKey(ruleID)) throw new ArgumentException("Trying to add a cycle setup for " + button + "/" + state + " with ID " + ruleID + ", but there already exists a multi setup with that key combination");
        if (!cycleSetups.ContainsKey(button)) cycleSetups.Add(button, new Dictionary<bState, Dictionary<string, SceneCycleSetup>>());
        if (!cycleSetups[button].ContainsKey(state)) cycleSetups[button].Add(state, new Dictionary<string, SceneCycleSetup>());
      }
      else if (type == ruleType.SceneMulti) {
        if (ContainsKey(button) && this[button].ContainsKey(state) && this[button][state].ContainsKey(ruleID)) throw new ArgumentException("Trying to add a multi setup for " + button + "/" + state + " with ID " + ruleID + ", but there already exists a single rule with that key combination");
        if (cycleSetups.ContainsKey(button) && cycleSetups[button].ContainsKey(state) && cycleSetups[button][state].ContainsKey(ruleID)) throw new ArgumentException("Trying to add a multi setup for " + button + "/" + state + " with ID " + ruleID + ", but there already exists a cycle setup with that key combination");
        if (!multiSetups.ContainsKey(button)) multiSetups.Add(button, new Dictionary<bState, Dictionary<string, SceneMultiSetup>>());
        if (!multiSetups[button].ContainsKey(state)) multiSetups[button].Add(state, new Dictionary<string, SceneMultiSetup>());
      }
      else {
        throw new NotImplementedException();
      }
    }
    #endregion
    #region AddSceneMultiButtonState
    private void AddSceneMultiButtonState(dButton button, bState state, SceneMultiSetup setup, string ruleID) {
      CheckAndAddKeys(button, state, ruleID, ruleType.SceneMulti);
      if (multiSetups[button][state].ContainsKey(ruleID)) multiSetups[button][state][ruleID] = setup;
      else multiSetups[button][state].Add(ruleID, setup);
    }
    #endregion
    #region AddSceneCycleButtonState
    private void AddSceneCycleButtonState(dButton button, bState state, SceneCycleSetup setup, string ruleID) {
      CheckAndAddKeys(button, state, ruleID, ruleType.SceneCycle);
      if (cycleSetups[button][state].ContainsKey(ruleID)) cycleSetups[button][state][ruleID] = setup;
      else cycleSetups[button][state].Add(ruleID, setup);
    }
    #endregion
    #region AddSingleRuleButtonState
    private void AddSingleRuleButtonState(dButton button, bState state, Rule rule, string ruleID) {
      CheckAndAddKeys(button, state, ruleID, ruleType.SingleRule);
      if (this[button][state].ContainsKey(ruleID)) this[button][state][ruleID] = rule;
      else this[button][state].Add(ruleID, rule);
    }
    #endregion
    #region CheckButtonStateAdded
    private bool CheckButtonStateAdded(dButton button, bState state, string ruleID) {
      if (!ContainsKey(button)) return false;
      if (!this[button].ContainsKey(state)) return false;
      if (this[button][state].ContainsKey(ruleID)) return false;
      return true;
    }
    #endregion
    #endregion
    #endregion
  }
}
