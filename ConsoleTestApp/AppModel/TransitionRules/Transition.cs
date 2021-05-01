using ConsoleTestApp.ApiObjects;
using ConsoleTestApp.ApiObjects.Rules;
using ConsoleTestApp.ApiObjects.Rules.Actions;
using ConsoleTestApp.ApiObjects.Schedules;
using ConsoleTestApp.ApiObjects.Sensors;
using ConsoleTestApp.ApiObjects.Sensors.State;
using System;
using System.Collections.Generic;

namespace ConsoleTestApp.AppModel.TransitionRules {
  class Transition : complexBridgeSetup {
    #region ### Instance fields
    private Alarm Trigger;
    // private Sensor<BoolSensorState> isTransStarting;
    private Timer nextSceneTrigger;
    #endregion
    #region ### Constructor
    public Transition(SceneDefinition def, string name, List<TransitionGroup> groups, Sensor<BoolSensorState> houseStatus, Sensor<IntSensorState> currentSceneTracker, Sensor<IntSensorState> currentTransRule, int transRuleID, int currentOrder, bool isFinalRule, bool stayAtFinalState, bool printInfo, bool pauseBeforeUpdating, TimeSpan? triggerTime = null, WeekDays days = WeekDays.Monday | WeekDays.Tuesday | WeekDays.Wednesday | WeekDays.Thursday | WeekDays.Friday) {
      def.Name = name;
      def.SaveSceneDefinitionToBridge(printInfo, printInfo, pauseBeforeUpdating);
      #region Create one rule for each group
      foreach (var group in groups) {
        var rule = Rule.GetTransitionRule(name, group.Group.Name);
        rule.AddConditionTrigger(group.reqChange, true);
        // 7.6.20: Deleted "|| triggerTime != null" from the two if's below - I cannot see any reason to have them, rather they are creating som serious issues
        if (!(def.IgnoreIsOff ?? false)) rule.AddConditionValueEquals(group.isOn, true);
        else rule.AddActionSetBoolSensorValue(group.isOn, true);
        if (!(def.IgnoreIsOff ?? false)) rule.AddConditionValueEquals(group.noTrans, false);
        else rule.AddActionSetBoolSensorValue(group.noTrans, false);
        rule.AddConditionValueEquals(houseStatus, true);
        rule.AddConditionValueEquals(currentSceneTracker, currentOrder);
        if (currentTransRule != null) rule.AddConditionValueEquals(currentTransRule, transRuleID);
        // Note: this will fail if the scene in the definiton has not been created in the bridge! Make sure that happens first...
        rule.AddActionSceneRecall(group.Group.Name, def.Name);
        rule.AddActionSetBoolSensorValue(group.reqChange, false);
        rules.Add(rule);
      }
      #endregion
      #region Create a timer to trigger the next scene
      if (!isFinalRule) {
        nextSceneTrigger = new Timer(name + "_Next", "Triggers the next scene after the transition time is finished", TimeSpan.FromMilliseconds(def.TransitionTime.Value * 100));
        nextSceneTrigger.Action = new IntSensorAction(currentSceneTracker, currentOrder + 1, true);
        nextSceneTrigger.Status = EnabledState.disabled;
        AddToBridgeDictionaries(nextSceneTrigger);
      }
      // If we are not staying at the final state, reset counter to 0 after the whole run is done
      if (isFinalRule && !stayAtFinalState) {
        nextSceneTrigger = new Timer(name + "_Next", "Triggers the next scene after the transition time is finished", TimeSpan.FromMilliseconds(def.TransitionTime.Value * 100));
        nextSceneTrigger.Action = new IntSensorAction(currentSceneTracker, 0, true);
        nextSceneTrigger.Status = EnabledState.disabled;
        AddToBridgeDictionaries(nextSceneTrigger);
      }
      #endregion
      #region Create a rule to trigger all the other rules
      var startRule = Rule.GetTransitionRule(name, ">trigger");
      startRule.AddConditionTrigger(currentSceneTracker, currentOrder);
      // When triggering the first time, also tell the system that this transition rule is now at work (the latest to start will override those that have started before)
      // Note: this MUST come before all other rules, or the other rules will not trigger correctly...
      if (triggerTime != null && currentTransRule != null) startRule.AddActionSetIntSensorValue(currentTransRule, transRuleID);
      foreach (var group in groups) {
        startRule.AddActionSetBoolSensorValue(group.reqChange, true);
      }
      // TODO: Confirm that this logic doesn't blow up in my face! (was: if(!isFinalRule)...)
      if (nextSceneTrigger != null) startRule.AddActionStartTimer(nextSceneTrigger);
      rules.Add(startRule);
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
        Trigger = new Alarm(name.Substring(0, name.LastIndexOf(' ')), "Starts a chain of events to change the light state over time", triggerTime.Value, days);
        Trigger.Action = new IntSensorAction(currentSceneTracker, currentOrder, true);
        AddToBridgeDictionaries(Trigger);
        #endregion
      }
      #endregion
    }
    #endregion
    #region ### Instance methods
    #region SaveToBridge
    public void SaveToBridge(bool printInfo, bool pauseBeforeUpdating) {
      // this.isTransStarting.CreateIfMissingHijackIfExisting(printInfo, pauseBeforeUpdating);
      Trigger?.CreateIfMissingHijackIfExisting(printInfo, pauseBeforeUpdating);
      nextSceneTrigger?.CreateIfMissingHijackIfExisting(printInfo, pauseBeforeUpdating);
      foreach (var rule in rules) {
        // If there already exists a rule in the bridge with this name, we "hijack" it (hopefully it is supposed to be the same rule - since this class has clearn naming conventions), and send an update from our new rule
        rule.CreateIfMissingHijackIfExisting(printInfo, pauseBeforeUpdating);
      }
    }
    #endregion
    #endregion
  }
}
