using ConsoleTestApp.ApiObjects.Lights.State;
using ConsoleTestApp.ApiObjects.Rules;
using ConsoleTestApp.ApiObjects.Rules.Actions;
using ConsoleTestApp.ApiObjects.Scenes;
using ConsoleTestApp.ApiObjects.Schedules;
using ConsoleTestApp.ApiObjects.Sensors;
using ConsoleTestApp.ApiObjects.Sensors.State;
using ConsoleTestApp.AppModel.SwitchConfiguration;
using ConsoleTestApp.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace ConsoleTestApp.AppModel.Hardcoded {
  #region SwitchConfigList
  class SwitchConfigList : List<SwitchConfig> {
    public List<Rule> GroupDependencies { get; set; }
    public SwitchConfig AddDimmerSwitch(string switchName, string roomName) {
      var _switch = new SwitchConfig(switchName, roomName);
      Add(_switch);
      return _switch;
    }
    public void SaveToBridge(bool printInfo, bool pauseBeforeUpdating, bool deleteFirst, bool pauseBeforeDeleting) {
      foreach (var config in this) {
        config.SaveToBridge(printInfo, pauseBeforeUpdating, deleteFirst, pauseBeforeDeleting);
      }
      foreach (var rule in (this.GroupDependencies ?? new List<Rule>())) {
        rule.CreateIfMissingHijackIfExisting(printInfo, pauseBeforeUpdating);
      }
    }
    public void AddGroupDepencies(SwitchConfig dimmer, string controllingGroupName, params string[] dependentGroups) {
      if (this.GroupDependencies == null) this.GroupDependencies = new List<Rule>();
      var parent = dimmer.GetTransitionGroup(controllingGroupName);
      var sensors = new Sensor[] { parent.isOn, parent.noTrans, parent.reqChange };
      var values = new bool[] { true, false };
      foreach (var val in values) {
        var rule1 = new Rule(controllingGroupName.Truncate(32 - 17) + "_isOn" + "Cascade" + (val ? "T" : "F"));
        rule1.AddConditionTrigger(parent.isOn, val);
        var rule2 = new Rule(controllingGroupName.Truncate(32 - 17) + "_noTr" + "Cascade" + (val ? "T" : "F"));
        rule2.AddConditionTrigger(parent.noTrans, val);
        var rule3 = new Rule(controllingGroupName.Truncate(32 - 17) + "_rqCh" + "Cascade" + (val ? "T" : "F"));
        rule3.AddConditionTrigger(parent.reqChange, val);
        foreach (var group in dependentGroups) {
          var child = dimmer.GetTransitionGroup(group);
          rule1.AddActionSetBoolSensorValue(child.isOn, val);
          rule2.AddActionSetBoolSensorValue(child.noTrans, val);
          rule3.AddActionSetBoolSensorValue(child.reqChange, val);
        }
        this.GroupDependencies.Add(rule1);
        this.GroupDependencies.Add(rule2);
        this.GroupDependencies.Add(rule3);
      }
      // rule.AddConditionTrigger()
    }
  }
  #endregion
  static class MySwitchConfigs {
    #region GetHouseStatusSensor
    public static Sensor<BoolSensorState> GetHouseStatusSensor() {
      string sensorHouseOnName = "Status hus";
      var houseSensor = Program.hueBridge.Sensors.Values.FirstOrDefault(i => i.Name == sensorHouseOnName);
      if (houseSensor != null && houseSensor is Sensor<BoolSensorState>) {
        return (Sensor<BoolSensorState>)houseSensor;
      }
      else {
        return Sensor.CreateBoolSensor(sensorHouseOnName, "", true, true, true);
      }
    }
    #endregion
    #region GetButtonSetup
    public static SwitchConfigList GetButtonSetup() {
      // This sensor named is used in several buttons to check if the whole house is on or off, thus it is important to get the name right and it is defined here
      // var houseSensor = MySwitchConfigs.GetHouseStatusSensor();

      // TODO: Add some evening scenes to the list for each dimmer
      // string[] standardScenes = new string[] { "Koselys", "Snart kveld", "Recharge", "Vanlig lys", "Dagslys" };

      var leggTilBrytere = new SwitchConfigList();
      leggTilBrytere.Standard("BryterBad", groupName: "Bad");
      leggTilBrytere.Standard(switchName: "BryterKontor", groupName: "Kontor");
      leggTilBrytere.Standard("BryterStue", groupName: "Stue");
      var gang = leggTilBrytere.Yttergang(switchName: "BryterGang", groupName: "Gang og kontor");
      leggTilBrytere.AddGroupDepencies(gang, "Gang og kontor", new string[] { "Gang nede", "Kontor" });
      leggTilBrytere.Kjokken(switchName: "BryterKjøkken", groupName: "Kjøkken");
      leggTilBrytere.Ute("BryterUte", groupName: "Ute");
      leggTilBrytere.Soverom("BryterSoverom", groupName: "Soverom");
      leggTilBrytere.Stue2("Bryter2Stue", groupName: "Stue");
      return leggTilBrytere;
    }
    #endregion

    #region Hardcoded config for each button
    #region Kontor
    private static SwitchConfig Kontor(this SwitchConfigList list, string switchName, string groupName, string[] scenes) {
      var dimmer = list.AddDimmerSwitch(switchName, groupName);
      // dimmer.SetToOnAndThenScenes(dButton.On, bState.initial_press, scenes, delayedStart: false);
      var onButtonScenes = sceneButton.getFourButtonArray(onScene: "1. Vanlig lys", dimUpScene: "2. Dagslys", dimDownScene: "3. Koselys", offScene: "4. Snart kveld");
      dimmer.SetToReallyComplexSetup(dButton.On, bState.short_release, groupName, onButtonScenes, true, 5);
      dimmer.SetToStandardDimUp(dButton.DimUp, ruleID: "Vanlig");
      dimmer.SetToStandardDimDown(dButton.DimDown, ruleID: "Vanlig");
      dimmer.SetToTurnOff(dButton.Off, bState.initial_press);
      return dimmer;
    }
    #endregion
    #region AddStandard (not used?)
    /// <summary>
    /// Adds the following standard setup:
    /// ### ON button short press:
    /// - turns lights on to last state
    /// - requests a change to the latest transition state (this will start, but it will use the recorded transision time so it might take a while).
    /// - Also, for the next x seconds another button can be pressed to trigger one of four selected scenes. If a scene is triggered this way, the transition that was started will NOT be triggered again.
    ///   However, at the next transition trigger a transition will start to run.
    /// ### ON button long press:
    /// - turns lights on to last state
    /// - sets the group to NOT follow transitions
    /// - for the next x seconds another button can be pressed to trigger one of four selected scenes
    /// ### DIM UP/DOWN buttons:
    /// - Short press dims lights up/down a little bit, long press a bit more
    /// </summary>
    /// <param name="dimmer"></param>
    /// <param name="groupName"></param>
    /// <param name="sensorThatMustBeTrue"></param>
    /// <param name="sensorShouldBe"></param>
    private static SceneMultiSetup AddBasicStandardRules(this SwitchConfig dimmer, string groupName, string multiID, Sensor<BoolSensorState> sensorThatMustBeTrue = null, bool sensorShouldBe = true, Sensor<IntSensorState> trackerThatMustBeZero = null) {
      var setup = dimmer.SetToReallyComplexSetup(
        dButton.On,
        bState.short_release,
        groupName,
        // Vanlig lys: 2890K 100 %
        // Dagslys: 4291K 100 %
        // Koselys: 2250K 55 %
        // Snart kveld: Kveldsoransj 25 % (color 40 %)
        sceneButton.getFourButtonArray(onScene: "1. Vanlig lys", dimUpScene: "2. Dagslys", dimDownScene: "3. Koselys", offScene: "4. Snart kveld"),
        turnOn: true,
        timeout: 5,
        multiID: multiID,
        sensorThatMustBeTrue: sensorThatMustBeTrue,
        sensorShouldBe: sensorShouldBe,
        triggerTransOnMainButton: false,
        disableTrans: null
      );
      dimmer.SetToStandardDimUp(dButton.DimUp, ruleID: "Dim");
      dimmer.SetToStandardDimDown(dButton.DimDown, ruleID: "Dim");
      var rule1 = dimmer.SetToEnableTrans(dButton.DimUp, bState.short_release);
      rule1.AddConditionValueEquals(dimmer.complexStateTracker, 0);
      var rule2 = dimmer.SetToDisableTrans(dButton.DimDown, bState.short_release);
      rule2.AddConditionValueEquals(dimmer.complexStateTracker, 0);
      if (trackerThatMustBeZero != null) {
        rule1.AddConditionValueEquals(trackerThatMustBeZero, 0);
        rule2.AddConditionValueEquals(trackerThatMustBeZero, 0);
      }
      return setup;
    }
    #endregion
    #region Bad
    public static SwitchConfig Bad(this SwitchConfigList list, string switchName, string groupName) {
      var dimmer = list.AddDimmerSwitch(switchName, groupName);
      dimmer.AddBasicStandardRules(groupName, "");
      var offButtonScenes = sceneButton.getOnOffSetup(
        // onName: "GangBad", onLightList: "1,2,3,5,19",
        // dimUpName: null, dimUpLightList: "",
        groupName: groupName,
        dimmer: dimmer,
        multiID: "Av"
      );
      offButtonScenes = offButtonScenes.AddButton(null, dButton.DimDown); // Add night light setting
      dimmer.SetToReallyComplexSetup(
        dButton.Off,
        bState.short_release,
        groupName,
        offButtonScenes,
        turnOn: false, // Do not turn on lights (e.g. really, just trigger the multi setup without doing anything else - this leads to having to double click to turn off, but that is really by design)
        multiID: "Av",
        timeout: 3
      );
      return dimmer;
    }
    #endregion
    #region Standard
    public static SwitchConfig Standard(this SwitchConfigList list, string switchName, string groupName, string multiID = "") { // , string onName = null, string onLightList = null, string dimUpName = null, string dimUpLightList = null, string dimDownName = null, string dimDownLightList = null
      var dimmer = list.AddDimmerSwitch(switchName, groupName);
      dimmer.AddBasicStandardRules(groupName, multiID);
      /*var offButtonScenes = sceneButton.getOnOffSetup(
        onName: onName, onLightList: onLightList,
        dimUpName: dimUpName, dimUpLightList: dimUpLightList,
        dimDownName: dimDownName, dimDownLightList: dimDownLightList,
        groupName: groupName,
        dimmer: dimmer,
        multiID: "Av"
      );*/
      /*dimmer.SetToReallyComplexSetup(
        dButton.Off,
        bState.short_release,
        groupName,
        offButtonScenes,
        turnOn: false, // Do not turn on lights (e.g. really, just trigger the multi setup without doing anything else - this leads to having to double click to turn off, but that is really by design)
        multiID: "Av",
        timeout: 3
      );*/
      var rule = dimmer.SetToTurnOff(dButton.Off, bState.short_release);
      rule.AddConditionValueEquals(dimmer.complexStateTracker, 0);
      return dimmer;
    }
    #endregion
    #region Ute
    public static SwitchConfig Ute(this SwitchConfigList list, string switchName, string groupName) {
      var dimmer = list.AddDimmerSwitch(switchName, groupName);
      dimmer.SetToReallyComplexSetup(
        dButton.On,
        bState.short_release,
        groupName,
        // Vanlig lys: 2890K 100 %
        // Dagslys: 4291K 100 %
        // Koselys: 2250K 55 %
        // Snart kveld: Kveldsoransj 25 % (color 40 %)
        sceneButton.getFourButtonArray(onScene: "1. Vanlig lys", dimUpScene: "2. Dagslys", dimDownScene: "3. Koselys", offScene: "4. Snart kveld"),
        turnOn: true,
        timeout: 5,
        multiID: "1",
        triggerTransOnMainButton: false,
        disableTrans: null
      );
      dimmer.SetToStandardDimUp(dButton.DimUp, ruleID: "Dim");
      dimmer.SetToStandardDimDown(dButton.DimDown, ruleID: "Dim");
      dimmer.SetToReallyComplexSetup(
        dButton.DimUp,
        bState.short_release,
        groupName,
        // TODO: Define
        sceneButton.getFourButtonArray(onScene: "Solnedgang på Savannen", dimUpScene: "Tropenatt", dimDownScene: "Arktisk nordlys", offScene: "Vårblomst"),
        turnOn: true,
        timeout: 5,
        multiID: "2",
        triggerTransOnMainButton: false,
        disableTrans: null
      );
      var rule = dimmer.SetToTurnOff(dButton.Off, bState.short_release);
      rule.AddConditionValueEquals(dimmer.complexStateTracker, 0);
      dimmer.SetToTurnOn(dButton.On, bState.repeat, ruleID: "Garasjen", ruleID2: "Paa", groupName: "Garasjen");
      dimmer.SetToTurnOff(dButton.Off, bState.repeat, ruleID: "Garasjen", ruleID2: "Av", groupName: "Garasjen");
      return dimmer;
    }
    #endregion
    #region Kjokken
    public static SwitchConfig Kjokken(this SwitchConfigList list, string switchName, string groupName) {
      var dimmer = list.AddDimmerSwitch(switchName, groupName);
      var setup = dimmer.AddBasicStandardRules(groupName, "Rom");
      var currentlyCyclingFlag = dimmer.SetToScenes(
        dButton.On,
        bState.repeat,
        new string[] { "Lyst", "Dimmet", "Nattlys" },
        setToOnFirst: true,
        cycleID: "Benk",
        cycleState: bState.short_release,
        groupName: "Benkebelysning",
        triggerTrans: false,
        disableTrans: null,
        signalCycleStart: true
      );
      setup.AddConditionValueEquals(currentlyCyclingFlag, false);
      var rule = dimmer.SetToTurnOff(dButton.Off, bState.short_release);
      rule.AddConditionValueEquals(dimmer.complexStateTracker, 0);
      return dimmer;
    }
    #endregion
    #region Kjokken
    public static SwitchConfig Yttergang(this SwitchConfigList list, string switchName, string groupName) {
      var dimmer = list.AddDimmerSwitch(switchName, groupName);
      dimmer.AddBasicStandardRules(groupName, "Gang"); // , trackerThatMustBeZero: dimmer.complexStateTracker
      dimmer.SetToReallyComplexSetup(
        dButton.On,
        bState.repeat,
        "Første etasje",
        sceneButton.getFourButtonArray(onScene: "1. Vanlig lys", dimUpScene: "2. Dagslys", dimDownScene: "3. Koselys", offScene: "4. Snart kveld"),
        turnOn: true,
        timeout: 5,
        multiID: "Hus",
        triggerTransOnMainButton: false,
        disableTrans: null
      );
      var rule = dimmer.SetToTurnOff(dButton.Off, bState.short_release);
      rule.AddConditionValueEquals(dimmer.complexStateTracker, 0);
      dimmer.SetToTurnOff(dButton.Off, bState.repeat, groupName: "Første etasje");
      return dimmer;
    }
    #endregion
    #region Stue
    private static SwitchConfig Stue(this SwitchConfigList list, string switchName, string groupName) {
      var dimmer = list.AddDimmerSwitch(switchName, groupName);
      // string[] prayerScenes = new string[] { "Kveldsbønn", "Morgenbønn" };
      // var isCyclingFlag = dimmer.SetToScenes(dButton.On, bState.repeat, prayerScenes, false, cycleState: bState.short_release, ruleID: "Bonn", groupName: "Bønneplass", altBtnName: "ddBonn");
      dimmer.AddBasicStandardRules(groupName, ""); // , sensorThatMustBeTrue: isCyclingFlag, sensorShouldBe: false
      // dimmer.SetToTurnOff(dButton.Off, bState.repeat, ruleID: "Bonn", groupName: "Bønneplass", altBtnName: "ddBonn");
      var offButtonScenes = sceneButton.getOnOffSetup(
        // onName: "Salong", onLightList: "12,14,9",
        // dimUpName: "Spiseb", dimUpLightList: "22,23,24,21",
        // dimDownName: "Pynt", dimDownLightList: "4,11,16",
        groupName: groupName,
        dimmer: dimmer,
        multiID: "Av"
      );
      dimmer.SetToReallyComplexSetup(
        dButton.Off,
        bState.short_release,
        groupName,
        offButtonScenes,
        turnOn: false, // Do not turn on lights (e.g. really, just trigger the multi setup without doing anything else - this leads to having to double click to turn off, but that is really by design)
        multiID: "Av",
        timeout: 3
      );
      return dimmer;
    }
    #endregion
    #region Stue2
    private static SwitchConfig Stue2(this SwitchConfigList list, string switchName, string groupName) {
      var dimmer = list.AddDimmerSwitch(switchName, groupName);
      dimmer.SetToReallyComplexSetup(
        dButton.On,
        bState.short_release,
        "Alle lys (\"hemmelig\" systemgruppe)",
        sceneButton.getFourButtonArray(onScene: "Fargesett1", dimUpScene: "Morgenbønn", dimDownScene: "Kveldsbønn", offScene: "Midsomer Sun"),
        turnOn: false,
        timeout: 5,
        multiID: "1",
        triggerTransOnMainButton: false,
        disableTrans: null
      );
      #region ON BUTTON: Prayer light
      // Short press: turn prayer light on and cycle morning/evening light levels, Long press: turn off
      /*string[] prayerScenes = new string[] { "Kveldsbønn", "Morgenbønn" };
      var isCyclingFlag = dimmer.SetToScenes(dButton.On, bState.short_release, prayerScenes, false, cycleState: bState.short_release, ruleID: "Bonn", groupName: "Bønneplass", altBtnName: "onBonn");
      dimmer.SetToTurnOff(dButton.On, bState.repeat, ruleID: "Bonn", groupName: "Bønneplass", altBtnName: "onBonn");*/
      #endregion
      #region DIM UP BUTTON: Different states for spots
      // 1: Scenes for spots (e.g. different colors and light levels)
      // 2-4: Turn on/off each spot
      #endregion
      #region DIM DOWN BUTTON: Cool light states for the entire room
      // Create some cool scenes and then add recalls here...
      #endregion
      #region OFF BUTTON: Groups on/off
      /*var offButtonScenes = sceneButton.getOnOffSetup(
        onName: "Bonn", onLightList: "13",
        dimUpName: "Spiseb", dimUpLightList: "25,26,27,28",
        dimDownName: "Bilder", dimDownLightList: "45,46",
        offName: "Sofavg", offLightList: "22,23,24",
        groupName: groupName,
        dimmer: dimmer,
        multiID: "Grp"
      );*/
      var offButtonScenes = sceneButton.getOffSetup(
        onName: "Bonn", onLightList: "13",
        dimUpName: "Spiseb", dimUpLightList: "25,26,27,28",
        dimDownName: "Bilder", dimDownLightList: "45,46",
        offName: "Sofavg", offLightList: "22,23,24",
        groupName: groupName,
        dimmer: dimmer,
        multiID: "4"
      );
      dimmer.SetToReallyComplexSetup(
        dButton.Off,
        bState.short_release,
        "Alle lys (\"hemmelig\" systemgruppe)",
        offButtonScenes,
        turnOn: false, // Do not turn on lights (e.g. really, just trigger the multi setup without doing anything else - this leads to having to double click to turn off, but that is really by design)
        multiID: "4",
        timeout: 3
      );
      #endregion
      return dimmer;
    }
    #endregion
    #region Soverom
    private static SwitchConfig Soverom(this SwitchConfigList list, string switchName, string groupName) {
      var dimmer = list.AddDimmerSwitch(switchName, groupName);
      // TODO: Create these scenes first!
      dimmer.SetToScenes(dButton.On, bState.short_release, new string[] { "Soverom svakt lys", "Gå-på-do-lys" }, false, "Natt", groupName: "Alle lys (\"hemmelig\" systemgruppe)");
      dimmer.SetToReallyComplexSetup(
        dButton.On,
        bState.repeat,
        groupName,
        // Vanlig lys: 2890K 100 %
        // Dagslys: 4291K 100 %
        // Koselys: 2250K 55 %
        // Snart kveld: Kveldsoransj 25 % (color 40 %)
        sceneButton.getFourButtonArray(onScene: "1. Vanlig lys", dimUpScene: "2. Dagslys", dimDownScene: "3. Koselys", offScene: "4. Snart kveld"),
        turnOn: true,
        timeout: 5,
        triggerTransOnMainButton: false,
        disableTrans: null,
        multiID: "Paa"
      );
      dimmer.SetToStandardDimUp(dButton.DimUp, ruleID: "Standard");
      dimmer.SetToStandardDimDown(dButton.DimDown, ruleID: "Standard");
      dimmer.SetToTriggerTimerTransition(dButton.DimDown, bState.short_release, sensorThatMustBeTrue: dimmer.GetTransitionGroup(groupName).isOn, sensorShouldBe: false);
      dimmer.SetToTurnOff(dButton.Off, bState.short_release);
      // TODO: Possibly add some button actions above that starts timers for going to sleep, and then stopping them on off button press
      dimmer.SetToTurnOffMultipleGroupsAndTimers(dButton.Off, bState.repeat, new string[] { "Soverom", "Gang og kontor", "Bad" }, new Timer[] { }, "Natt");
      return dimmer;
    }
    #endregion
    #region Yttergang1 (Not in use)
    /*private static SwitchConfig Yttergang1(this SwitchConfigList list, string switchName, string groupName, Sensor<BoolSensorState> sensor, string[] scenes) {
      var dimmer = list.AddDimmerSwitch(switchName, groupName);
      // Turn group on if the "Hus paa" sensor is false
      dimmer.SetToTurnOn(dButton.On, bState.short_release, ruleID: "Hus paa", sensorThatMustBeTrue: sensor, sensorShouldBe: false);
      dimmer.AddSetBoolSensorValueAction(dButton.On, bState.short_release, ruleID: "Hus paa", sensor, true);
      // Turn group off if the "Hus paa" sensor is true
      dimmer.SetToTurnOff(dButton.On, bState.short_release, "Hus paa", sensor);
      dimmer.AddSetBoolSensorValueAction(dButton.On, bState.short_release, ruleID: "Hus paa", sensor, false);
      // Set to some beautiful scene if the DimUp button is pressed
      dimmer.SetToOnAndThenScenes(dButton.DimUp, bState.short_release, scenes, ruleID: "Hus paa", delayedStart: false);
      return dimmer;
    }*/
    #endregion
    #endregion
  }
}
