using ConsoleTestApp.ApiObjects.Lights.State;
using ConsoleTestApp.ApiObjects.Rules;
using ConsoleTestApp.ApiObjects.Sensors;
using ConsoleTestApp.ApiObjects.Sensors.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConsoleTestApp.AppModel.SwitchConfiguration {
  static class SwitchConfigHelpers {
    /// <summary>Creates a "basic" rule for turning on a switch</summary>
    /// <param name="_switch">The switch to turn on</param>
    /// <param name="button">The button that should trigger turning on</param>
    /// <param name="state">The button state that should trigger it</param>
    /// <param name="ruleID1">First part of the ruleID in the rule name, that could be "BryterRoom ID1_Button State (ID2)"</param>
    /// <param name="ruleID2">Second part of the ruleID in the rule name, that could be "BryterRoom ID1_Button State (ID2)"</param>
    /// <param name="tg">The transitiongroup this is related to</param>
    /// <param name="sensorThatMustBeTrue"></param>
    /// <param name="sensorShouldBe"></param>
    /// <param name="altBtnName">If the button should be named something other than default in rulenames, etc.</param>
    /// <param name="turnOnLights">If true, this rule should actually turn on the lights (if not, it will only prepare for turning on at the next button press)</param>
    /// <param name="useTrans">If true, trigger transitions when turning on the lights</param>
    /// <returns>The rule that handles all this</returns>
    public static Rule GetBasicTurnOnRule(Sensor _switch, dButton button, bState state, string ruleID1, string ruleID2, TransitionGroup tg, Sensor<BoolSensorState> sensorThatMustBeTrue = null, bool sensorShouldBe = true, string altBtnName = null, bool turnOnLights = true, bool triggerTrans = true, bool? disableTrans = null) {
      var rule = Rule.GetButtonRule(_switch, button, state, ruleID1, ruleID2, altBtnName);
      rule.AddConditionButtonTrigger(_switch, button, state);
      if (sensorThatMustBeTrue != null) rule.AddConditionValueEquals(sensorThatMustBeTrue, sensorShouldBe);
      // TODO: Confirm if this is TOO hacky or ok... The point is that if I am using the "0"-group to trigger the scene, then the whole group should NOT be turned on ever...
      // bool isZeroGroup = ;
      if (tg.GroupID != "0") {
        if (turnOnLights) rule.AddActionSetGroupState(tg.GroupName, new LightStateChanger { IsOn = true });
        if (disableTrans != null) rule.AddActionSetBoolSensorValue(tg.noTrans, disableTrans.Value);
        if (turnOnLights && triggerTrans) rule.AddActionSetBoolSensorValue(tg.reqChange, true);
        rule.AddActionSetBoolSensorValue(tg.isOn, true);
      }
      #region Create a timer that will set _reqChange to false in a few seconds, if no other rule handles that (or is that needed?)
      // this.cycleTimeout = new Timer(safeName + "_cycleTimeout", "Gives the user some time to press the button again when cycling", new TimeSpan(0, 0, 10));
      // this.cycleTimeout.Action = new BoolSensorAction(currentlyCyclingFlag.Name, false, true);
      // this.cycleTimeout.Status = EnabledState.disabled;
      // TestAddToBridge(addToBridgeWithoutSaving, this.cycleTimeout);
      #endregion

      return rule;
    }
    public static Rule GetBasicTurnOffRule(Sensor _switch, dButton button, bState state, string ruleID1, string ruleID2, TransitionGroup tg, Sensor<BoolSensorState> sensorThatMustBeTrue = null, bool sensorShouldBe = true, string altBtnName = null, bool turnOffLights = true) {
      var rule = Rule.GetButtonRule(_switch, button, state, ruleID1, ruleID2, altBtnName);
      rule.AddConditionButtonTrigger(_switch, button, state);
      if (sensorThatMustBeTrue != null) rule.AddConditionValueEquals(sensorThatMustBeTrue, sensorShouldBe);
      if (turnOffLights) rule.AddActionSetGroupState(tg.GroupName, new LightStateChanger { IsOn = false });
      rule.AddActionSetBoolSensorValue(tg.isOn, false);
      return rule;
    }
    public static Rule GetChangeTransStateRule(Sensor _switch, dButton button, bState state, TransitionGroup tg, bool enableTrans, string ruleID1, string ruleID2, string altBtnName = null) {
      var rule = Rule.GetButtonRule(_switch, button, state, ruleID1, ruleID2, altBtnName);
      rule.AddConditionButtonTrigger(_switch, button, state);
      rule.AddConditionValueEquals(tg.isOn, true);
      rule.AddActionSetBoolSensorValue(tg.noTrans, !enableTrans);
      if (enableTrans) rule.AddActionSetBoolSensorValue(tg.reqChange, true);
      return rule;
    }
  }
}
