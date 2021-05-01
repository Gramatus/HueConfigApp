using ConsoleTestApp.ApiObjects;
using ConsoleTestApp.ApiObjects.Lights.State;
using ConsoleTestApp.ApiObjects.Rules;
using ConsoleTestApp.ApiObjects.Rules.Actions;
using ConsoleTestApp.ApiObjects.Scenes;
using ConsoleTestApp.ApiObjects.Schedules;
using ConsoleTestApp.ApiObjects.Sensors;
using ConsoleTestApp.ApiObjects.Sensors.State;
using ConsoleTestApp.Helpers;
using System;
using System.Collections.Generic;

namespace ConsoleTestApp.AppModel.SwitchConfiguration {
  class SimpleChangeOptions {
    public dButton? Button { get; set; }
    public bState? State { get; set; }
    public bool OnlyIfLightsIsOn { get; set; }
    public bool TurnOn { get; set; }
  }
  class SceneMultiSetup : complexBridgeSetup {
    #region ### Instance fields
    private Timer timeout;
    private List<Sensor<IntSensorState>> sceneNumberTrackers;
    // private Rule initialRule;
    #endregion
    #region ### Constructor
    public static SceneMultiSetup Create(Sensor _switch, dButton button, bState state, TransitionGroup tg, Sensor<IntSensorState> complexTracker, int trackNum, sceneButton[] options, bool turnOn, int timeout, string altBtnName = null, SimpleChangeOptions simpleChange = null, string multiID = "") {
      return new SceneMultiSetup(_switch, button, state, tg, complexTracker, trackNum, options, turnOn, timeout, altBtnName, true, null, simpleChange, multiID);
    }
    /// <summary>A SceneMultiSetup is primarily a way to trigger multiple scenes after pressing a given button. That is:
    /// 1. You press the given button (optionally this will only trigger if a given sensor has a given value, e.g. only if the lights are already on or not on)
    ///    - Options for this intial trigger:
    ///      - turnOn: if true, the lights will be turned on when the button is pressed (if false, no state change will happen). A false value will typically be used if the scenes are raelly "turn off scenes", as we will then prefer to have the lights in their current state
    ///      - triggerTransOnMainButton: if true, the sensor !FadeAv will be set to true and (if turnOn is also true) the sensor _reqChange will be set to true (thus triggering the transition that should have been triggered if the lights were on)
    ///    - You can also specify to have an alternate trigger that will *NOT* trigger fading. If this is specified, you cannot set triggerTransOnMainButton to false as the second button/state combination will have that responsibility
    /// 2. </summary>
    /// <param name="_switch">Name of the switch that has this setup</param>
    /// <param name="button">Button to trigger cycling</param>
    /// <param name="state">Buttonstate to trigger cycling</param>
    /// <param name="tg">A transitiongroup representing the group to trigger the cycling for</param>
    /// <param name="complexTracker">A sensor that keeps track of wheter the parent sensor is currently in a complex state where normal rules should not apply.</param>
    /// <param name="trackNum">The value of the complex tracker that tells the bridge that this setup is in control.</param>
    /// <param name="options">A collection of buttons / buttonstates and what scenes they should trigger when this setup is in control.</param>
    /// <param name="turnOn"></param>
    /// <param name="timeout">How long before cycling is no longer available.</param>
    /// <param name="triggerTransOnMainButton"></param>
    /// <param name="altBtnName">If the button should be identified by another name in rules</param>
    /// <param name="buttonForSimpleChange"></param>
    /// <param name="stateForSimpleChange"></param>
    /// <param name="onlySimpleChangeIfLightsIsOn"></param>
    /// <param name="multiID"></param>
    public static SceneMultiSetup Create(Sensor _switch, dButton button, bState state, TransitionGroup tg, Sensor<IntSensorState> complexTracker, int trackNum, sceneButton[] options, bool turnOn, int timeout, string altBtnName = null, bool triggerTransOnMainButton = true, bool? disableTrans = null, string multiID = "") {
      return new SceneMultiSetup(_switch, button, state, tg, complexTracker, trackNum, options, turnOn, timeout, altBtnName, triggerTransOnMainButton, disableTrans, null, multiID);
    }
    private SceneMultiSetup(Sensor _switch, dButton button, bState state, TransitionGroup tg, Sensor<IntSensorState> complexTracker, int trackNum, sceneButton[] options, bool turnOn, int timeout, string altBtnName, bool triggerTransOnMainButton, bool? disableTrans, SimpleChangeOptions simpleChange, string multiID) {
      // if (cycleState == null) cycleState = state;
      this.sceneNumberTrackers = new List<Sensor<IntSensorState>>();
      string safeName = _switch.Name.Truncate(32 - 15).FixNorwegianChars() + "_" + tg.GroupName.Truncate(32 - 15 - _switch.Name.Length).FixNorwegianChars();
      AddToBridgeDictionaries(complexTracker);
      #region Create a timer that will give the user some seconds to press again
      this.timeout = new Timer(safeName + "_Timeout", "Gives the user some time to select the wanted scene", new TimeSpan(0, 0, timeout)) {
        Action = new IntSensorAction(complexTracker, 0, true),
        Status = EnabledState.disabled
      };
      AddToBridgeDictionaries(this.timeout);
      #endregion
      #region Create an initial trigger
      Rule initialRule;
      if (turnOn) {
        initialRule = SwitchConfigHelpers.GetBasicTurnOnRule(
          _switch,
          button,
          state,
          ruleID1: "Multi" + multiID + "!",
          ruleID2: triggerTransOnMainButton ? "TrFade" : "",
          tg,
          altBtnName: altBtnName,
          turnOnLights: true,
          triggerTrans: triggerTransOnMainButton,
          disableTrans: simpleChange == null ? disableTrans : false,
          // If we have a separate button for simple changes, only trigger this button press if the lights are off
          sensorThatMustBeTrue: (simpleChange != null && simpleChange.OnlyIfLightsIsOn ? tg.isOn : null),
          sensorShouldBe: false
        );
      }
      else {
        initialRule = Rule.GetButtonRule(_switch, button, state, ruleID1: "Multi" + multiID + "!", ruleID2: "", altBtnName);
        initialRule.AddConditionButtonTrigger(_switch, button, state);
      }
      initialRule.AddConditionValueEquals(complexTracker, 0);
      initialRule.AddActionSetIntSensorValue(complexTracker, trackNum);
      initialRule.AddActionStartTimer(this.timeout);
      rules.Add(initialRule);
      #endregion
      #region Create an alternate initial trigger
      Rule initialRuleSimple = null;
      if (simpleChange != null) {
        initialRuleSimple = SwitchConfigHelpers.GetBasicTurnOnRule(
          _switch,
          simpleChange.Button.Value,
          simpleChange.State ?? bState.short_release,
          ruleID1: "Multi" + multiID + "!",
          ruleID2: "TrStay",
          tg,
          altBtnName: altBtnName,
          turnOnLights: true,
          triggerTrans: false,
          disableTrans: true
        );
        initialRuleSimple.AddConditionValueEquals(complexTracker, 0);
        initialRuleSimple.AddActionSetIntSensorValue(complexTracker, trackNum);
        initialRuleSimple.AddActionStartTimer(this.timeout);
        rules.Add(initialRuleSimple);
      }
      #endregion
      #region Add rules for all scene changes
      for (int i = 0; i < options.Length; i++) {
        if (options[i].scenes.Length == 0) { // This option does not use scene recall - then the actual action must be already stored in the rule, and the rule must actually exist
          if ((options[i].rules?.Length ?? 0) == 0) throw new ArgumentOutOfRangeException();
          if (options[i].rules.Length == 1) {
            this.addStandardConditionsAndActions(
              options[i].rules[0], complexTracker, trackNum
            );
            rules.Add(options[i].rules[0]);
          }
          else {
            var numberTracker = this.createNumerTracker(safeName, options[i].button, initialRule, initialRuleSimple);
            for (int ii = 0; ii < options[i].rules.Length; ii++) {
              this.addStandardConditionsAndActions(
                options[i].rules[ii], complexTracker, trackNum,
                numberTracker: numberTracker, currentNumber: ii + 1, maxNumber: options[i].rules.Length
              );
              rules.Add(options[i].rules[ii]);
            }
          }
        }
        else if (options[i].scenes.Length == 1) {
          if (string.IsNullOrEmpty(options[i].scenes[0])) continue; // Then this is added in "MySwitchConfigs" as "reserved for future use", and no rule should be created
          Rule sceneRule;
          if (options[i].rules == null || options[i].rules.Length == 0) {
            sceneRule = Rule.GetButtonRule(_switch, options[i].button, options[i].state ?? bState.short_release, ruleID1: "Multi" + multiID + "_", ruleID2: (options[i].ruleID2 ?? ""), altBtnName);
            sceneRule.AddConditionButtonTrigger(_switch, options[i].button, options[i].state ?? bState.short_release);
          }
          else {
            sceneRule = options[i].rules[0];
          }
          this.addStandardConditionsAndActions(sceneRule, complexTracker, trackNum, groupName: tg.GroupName, sceneName: options[i].scenes[0]);
          rules.Add(sceneRule);
        }
        else {
          var numberTracker = this.createNumerTracker(safeName, options[i].button, initialRule, initialRuleSimple);
          for (int ii = 0; ii < options[i].scenes.Length; ii++) {
            Rule sceneRule;
            if (options[i].rules == null || options[i].rules.Length <= ii) {
              sceneRule = Rule.GetButtonRule(_switch, options[i].button, options[i].state ?? bState.short_release, ruleID1: "Multi" + multiID + "_", ruleID2: (options[i].ruleID2 ?? "Scene") + "#" + (ii + 1), altBtnName);
              sceneRule.AddConditionButtonTrigger(_switch, options[i].button, options[i].state ?? bState.short_release);
            }
            else {
              sceneRule = options[i].rules[ii];
            }
            // sceneRule.AddConditionValueEquals(numberTracker, ii + 1);
            this.addStandardConditionsAndActions(
              sceneRule, complexTracker, trackNum,
              groupName: tg.GroupName, sceneName: options[i].scenes[ii],
              numberTracker: numberTracker, currentNumber: ii + 1, maxNumber: options[i].scenes.Length
            );
            // sceneRule.AddConditionValueEquals(complexTracker, trackNum);
            // sceneRule.AddActionSceneRecall(tg.GroupName, options[i].scenes[ii]);
            // sceneRule.AddActionStartTimer(this.timeout);
            // sceneRule.AddActionSetIntSensorValue(numberTracker, ii == options[i].scenes.Length - 1 ? 1 : ii + 2);
            // iii = ii+1
            // (iii) == options[i].scenes.Length ? 1 : iii + 1
            rules.Add(sceneRule);
          }
        }
      }
      #endregion
    }
    #endregion
    #region ### Instance methods
    #region addStandardConditionsAndActions
    private void addStandardConditionsAndActions(Rule rule, Sensor<IntSensorState> complexTracker, int trackNum, string groupName = null, string sceneName = null, Sensor<IntSensorState> numberTracker = null, int currentNumber = 0, int maxNumber = 0) {
      rule.AddConditionValueEquals(complexTracker, trackNum);
      if (numberTracker != null) rule.AddConditionValueEquals(numberTracker, currentNumber);
      if (groupName != null && sceneName != null) rule.AddActionSceneRecall(groupName, sceneName);
      rule.AddActionStartTimer(this.timeout);
      if (numberTracker != null) rule.AddActionSetIntSensorValue(numberTracker, currentNumber == maxNumber ? 1 : currentNumber + 1);
    }
    #endregion
    #region createNumerTracker
    private Sensor<IntSensorState> createNumerTracker(string safeName, dButton button, Rule initialRule, Rule initialRuleSimple) {
      #region Create a tracker that will tell us what should be the next scene to cycle through
      var numberTracker = new Sensor<IntSensorState>("");
      numberTracker.Name = safeName.Truncate(32 - (button.GetDescription().Length + 9)) + "_" + button.GetDescription() + "_Current";
      numberTracker.State.State = 0;
      AddToBridgeDictionaries(numberTracker);
      this.sceneNumberTrackers.Add(numberTracker);
      initialRule.AddActionSetIntSensorValue(numberTracker, 1);
      if (initialRuleSimple != null) initialRuleSimple.AddActionSetIntSensorValue(numberTracker, 1);
      return numberTracker;
      #endregion

    }
    #endregion
    #region SaveToBridge
    public List<Rule> SaveToBridge(Sensor<IntSensorState> complexStateTracker, int trackNum, bool printInfo, bool pauseBeforeUpdating) {
      foreach (var sceneNumberTracker in this.sceneNumberTrackers) {
        sceneNumberTracker.CreateIfMissingHijackIfExisting(printInfo, pauseBeforeUpdating);
      }
      this.timeout.CreateIfMissingHijackIfExisting(printInfo, pauseBeforeUpdating);
      // Only trigger the complex setup if the tracker is 0
      // if (complexStateTracker != null) this.initialRule.AddConditionValueEquals(complexStateTracker, 0);
      // this.initialRule.CreateIfMissingHijackIfExisting(printInfo, pauseBeforeUpdating);
      foreach (var rule in rules) {
        // If there already exists a rule in the bridge with this name, we "hijack" it (hopefully it is supposed to be the same rule - since this class has clear naming conventions), and send an update from our new rule
        // rule.CreateIfMissingHijackIfExisting(printInfo, pauseBeforeUpdating);
      }
      return this.rules;
    }
    #endregion
    #endregion
  }
}
