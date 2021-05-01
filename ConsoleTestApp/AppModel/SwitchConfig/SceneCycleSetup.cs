using ConsoleTestApp.ApiObjects;
using ConsoleTestApp.ApiObjects.Lights.State;
using ConsoleTestApp.ApiObjects.Rules;
using ConsoleTestApp.ApiObjects.Rules.Actions;
using ConsoleTestApp.ApiObjects.Schedules;
using ConsoleTestApp.ApiObjects.Sensors;
using ConsoleTestApp.ApiObjects.Sensors.State;
using ConsoleTestApp.Helpers;
using System;
using System.Collections.Generic;

namespace ConsoleTestApp.AppModel.SwitchConfiguration {
  class SceneCycleSetup : complexBridgeSetup {
    #region ### Instance fields
    private Timer cycleTimeout;
    private Timer startDelay;
    public Sensor<BoolSensorState> currentlyCyclingFlag { get; set; }
    private Sensor<IntSensorState> sceneNumberTracker;
    #endregion
    #region ### Constructor
    /// <summary></summary>
    /// <param name="switchName">Name of the switch that has this setup</param>
    /// <param name="button">Button to trigger cycling</param>
    /// <param name="state">Buttonstate to trigger cycling</param>
    /// <param name="groupName">Group to trigger the cycling for</param>
    /// <param name="groups">A reference to a parent TransitionGroup-list.</param>
    /// <param name="scenesToCycle">The names of the scenes to cycle (this requires that the scenes to cycle has unique names!)</param>
    /// <param name="setToOnFirst">If true, the first press will only set the lights to on and not recall a scene. If false, the first press will also recall the first scene in the list.</param>
    /// <param name="cycleState">State to react to for cycling. Normally this will be used if you want to start a separate cycle state for long press (e.g state to trigger the cycle is "repeat", but state to trigger next in the cycle is "short_release").</param>
    /// <param name="cycleTimeout">How long before cycling is no longer available.</param>
    /// <param name="altBtnName">If the button should be identified by another name in rules</param>
    /// <param name="delayedStart">(WILL BE DEPRECATED?) Delays availability for cycling. See notes in the method code for why that might be needed (it probably isn't).</param>
    public SceneCycleSetup(Sensor _switch, dButton button, bState state, TransitionGroup tg, string[] scenesToCycle, bool setToOnFirst, bState? cycleState = null, int cycleTimeout = 10, string altBtnName = null, bool delayedStart = false, bool triggerTrans = true, bool? disableTrans = null, string cycleID = "", bool signalCycleStart = false) {
      if (cycleState == null) cycleState = state;
      string safeName = _switch.Name.Truncate(32 - 15) + "_" + tg.GroupName.Truncate(32 - 15 - _switch.Name.Length);
      safeName = safeName.FixNorwegianChars();
      #region Create a flag that will tell us that we are currently cycling scenes
      currentlyCyclingFlag = new Sensor<BoolSensorState>("");
      currentlyCyclingFlag.Name = safeName + "_isCycling";
      currentlyCyclingFlag.State.State = false;
      AddToBridgeDictionaries(currentlyCyclingFlag);
      #endregion
      #region Create a tracker that will tell us what should be the next scene to cycle through
      sceneNumberTracker = new Sensor<IntSensorState>("");
      sceneNumberTracker.Name = safeName + "_CurrentScene";
      sceneNumberTracker.State.State = 0;
      AddToBridgeDictionaries(sceneNumberTracker);
      #endregion
      #region Create a timer that will delay the cycling of scenes for a few seconds
      // The reason is the need for a logic that makes it so that:
      // 1. Scene cycling is available a couple of seconds after lights are turned on (i.e. the first few seconds after lights are turned on, they cannot be cycled)
      // 2. When lights are turned on, a rule is triggered to set lights to a wanted state based on time of day
      // The goal is to not have scene cycling and auto triggering to current state overriding each other. However, I do not think that will be an issue the way I ended up implementing things with _reqChange
      if (delayedStart) {
        this.startDelay = new Timer(safeName + "_startDelay", "Delays cycling for a few seconds after on is pressed", new TimeSpan(0, 0, 2));
        this.startDelay.Action = new BoolSensorAction(currentlyCyclingFlag, true, true);
        this.startDelay.Status = EnabledState.disabled;
        AddToBridgeDictionaries(this.startDelay);
      }
      #endregion
      #region Create a timer that will give the user some seconds to press again
      this.cycleTimeout = new Timer(safeName + "_cycleTimeout", "Gives the user some time to press the button again when cycling", new TimeSpan(0, 0, cycleTimeout));
      this.cycleTimeout.Action = new BoolSensorAction(currentlyCyclingFlag, false, true);
      this.cycleTimeout.Status = EnabledState.disabled;
      AddToBridgeDictionaries(this.cycleTimeout);
      #endregion
      #region Create an initial trigger
      var rule = SwitchConfigHelpers.GetBasicTurnOnRule(_switch, button, state, ruleID1: "Cycle" + cycleID + "!", ruleID2: setToOnFirst ? "" : "Scene#1", tg, altBtnName: altBtnName, triggerTrans: triggerTrans, disableTrans: disableTrans);
      if (!setToOnFirst) rule.AddActionSceneRecall(tg.GroupName, scenesToCycle[0]);
      rule.AddActionSetIntSensorValue(sceneNumberTracker, setToOnFirst ? 1 : 2);
      if (!delayedStart) rule.AddActionSetBoolSensorValue(currentlyCyclingFlag, true);
      if (delayedStart) rule.AddActionStartTimer(this.startDelay);
      rule.AddActionStartTimer(this.cycleTimeout);
      if (signalCycleStart) rule.AddActionSetGroupAlertState(tg.GroupName, ApiObjects.Groups.AlertState.select);
      rules.Add(rule);
      #endregion
      #region Add rules for all scene changes
      for (int i = 0; i < scenesToCycle.Length; i++) {
        if (!setToOnFirst && i == 0) continue;
        var sceneRule = Rule.GetButtonRule(_switch, button, cycleState.Value, ruleID1: "Cycle" + cycleID + "_", ruleID2: "Scene#" + (i + 1), altBtnName);
        sceneRule.AddConditionButtonTrigger(_switch, button, cycleState.Value);
        sceneRule.AddConditionValueEquals(currentlyCyclingFlag, true);
        sceneRule.AddConditionValueEquals(sceneNumberTracker, i + 1);
        sceneRule.AddActionSceneRecall(tg.GroupName, scenesToCycle[i]);
        sceneRule.AddActionStartTimer(this.cycleTimeout);
        sceneRule.AddActionSetIntSensorValue(sceneNumberTracker, i == scenesToCycle.Length - 1 ? 1 : i + 2);
        rules.Add(sceneRule);
      }
      #endregion
    }
    #endregion
    #region ### Instance methods
    #region SaveToBridge
    public List<Rule> SaveToBridge(Sensor<IntSensorState> complexStateTracker, int trackNum, bool printInfo, bool pauseBeforeUpdating) {
      this.currentlyCyclingFlag.CreateIfMissingHijackIfExisting(printInfo, pauseBeforeUpdating);
      this.sceneNumberTracker.CreateIfMissingHijackIfExisting(printInfo, pauseBeforeUpdating);
      this.startDelay?.CreateIfMissingHijackIfExisting(printInfo, pauseBeforeUpdating);
      this.cycleTimeout.CreateIfMissingHijackIfExisting(printInfo, pauseBeforeUpdating);
      foreach (var rule in rules) {
        if (complexStateTracker != null) {
          // Then we need to keep track of things and only react to the normal buttons when we are not in a complex state situation. If the tracker for complex state is 0, then normal rules should apply.
          rule.AddConditionValueEquals(complexStateTracker, trackNum);
        }
        // If there already exists a rule in the bridge with this name, we "hijack" it (hopefully it is supposed to be the same rule - since this class has clearn naming conventions), and send an update from our new rule
        // rule.CreateIfMissingHijackIfExisting(printInfo, pauseBeforeUpdating);
      }
      return this.rules;
    }
    #endregion
    #endregion
  }
}
