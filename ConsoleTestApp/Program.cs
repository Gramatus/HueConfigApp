using ConsoleTestApp.ApiObjects.Lights.State;
using ConsoleTestApp.ApiObjects.Rules;
using ConsoleTestApp.ApiObjects.Rules.Actions;
using ConsoleTestApp.ApiObjects.Scenes;
using ConsoleTestApp.ApiObjects.Schedules;
using ConsoleTestApp.ApiObjects.Sensors.State;
using ConsoleTestApp.AppModel;
using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Linq;

namespace ConsoleTestApp {
  class Program {
    #region Properties and fields (and some more)
    public static readonly string userAPIroot;// = ;
    private static readonly string serverURL = "http://192.168.50.52";
    private static string RESTfulRoot { get { return serverURL + userAPIroot; } }
    public static Bridge hueBridge { get; private set; }
    public static int FieldLengthConfigInt { get { return 5; } }
    #endregion
    #region Notes on capabilities of the bridge
    // This last thing might lead to many rules (and scene switching also does that!), so do some calculations on how many rules there can be.
    // Probably, we should at least limit the number of "day zones" to every hour in the afternoon and then a couple more (again - do the maths)
    // (the below from /capabilities)
    // It seems that the bridge supports 250 rules, with a total of 1500 conditions and 1000 actions.
    // Also, max 100 schedules and max 250 sensors, and max 64 groups
    #endregion
    #region STATIC CONSTRUCTOR
    static Program() {
      var key = System.IO.File.ReadAllText("apikey.txt"); // This file is not included in the repository, but it should contain the API key for the bridge. It is however stored as a a codespace secret, so it should be available when running in codespaces (and can be exported from there if needed).
      if (!string.IsNullOrEmpty(key)) userAPIroot = "/api/" + key;
    }
    #endregion
    #region Main
    static void Main() {
      string logFolder = @"C:\Dev\HueConfigApp\ConsoleTestApp\Logs\";

      #region Setup parameteres
      bool printInfo = false;
      bool printBridgeInfo = false;
      bool pauseBeforeUpdating = false;
      bool pauseBeforeDeleting = true;
      bool deleteElementsBeforeUpdatingBridge = false;
      bool createJsonOnlyOnSave = true;

      string backupFolder = @"C:\Users\Torgeir\Dropbox\Konfigurasjonsfiler\Backup\";
      string configFolder = @"C:\Users\Torgeir\Dropbox\Konfigurasjonsfiler\HueConfig\";

      var apisToBackup = new Bridge.hueApi[] { Bridge.hueApi.scenes, Bridge.hueApi.groups, Bridge.hueApi.lights };
      string[] backupFilenames = new string[] { "Scenes", "Groups", "Lights" }; // "DefaultScenes"
      string[] scenesToBackup = new string[] { }; // { "Koselys", "Vanlig lys", "Dagslys", "Recharge" };

      string updateSwitchSceneDefsFile = "SwitchScenedefs.csv"; // "MyManualScenes.csv" or MyStandardScenes.csv or MyEveningScenedefs.csv, etc.
      string updateSwitchSceneDefsStatesFile = "SwitchSpecialLights.csv"; // MyManualScenesSpecialLights.csv

      string transitionRulesDefsFile = "TransitionScenedefs.csv"; // MyEveningScenedefs.csv or MyTestSceneDefs.csv, etc.
      string transitionRulesDefsStatesFile = "TransitionSpecialLights.csv"; // "MySpecialLights.csv"
      WeekDays standardWeekDays = WeekDays.Monday | WeekDays.Tuesday | WeekDays.Wednesday | WeekDays.Thursday | WeekDays.Friday | WeekDays.Saturday | WeekDays.Sunday;
      // var transitionRulesStartTimes = new List<TimeSpan>();
      // transitionRulesStartTimes.Add(DateTime.Now.AddMinutes(2).TimeOfDay);
      // transitionRulesStartTimes.Add(new TimeSpan(16, 0, 0));
      #endregion

      hueBridge = new Bridge(RESTfulRoot, logFolder, createJsonOnlyOnSave);
      hueBridge.Initialize();

      #region Select what to do
      bool doSomeBackups = false;
      bool updateDimmers = false;
      bool updateTransitions = false;
      bool updateTransitionScenesOnly = true;
      bool updateSwitchScenes = false;
      #endregion
      if (!doSomeBackups && !updateDimmers && !updateTransitions && !updateSwitchScenes && !updateTransitionScenesOnly) Console.WriteLine("No actions activated, application must be changed to do something :)");
      #region Cleanup tasks
      // If in need of cleaning up, simply add lines to the file mentioned below where each line is
      var cleanupFile = configFolder + "cleanup.txt";
      if (!System.IO.File.Exists(cleanupFile)) System.IO.File.WriteAllText(cleanupFile, "");
      foreach (var line in System.IO.File.ReadAllLines(cleanupFile)) {
        if (!string.IsNullOrEmpty(line)) hueBridge.CleanUpResourceGroup(line, false, true);
      }
      System.IO.File.WriteAllText(cleanupFile, "");
      #endregion
      #region Do backup
      if (doSomeBackups) {
        for (int i = 0; i < apisToBackup.Length; i++) {
          if (backupFilenames.Length > i) {
            if (apisToBackup[i] == Bridge.hueApi.scenes && scenesToBackup?.Length > 0) hueBridge.BackupSceneDefinitions(backupFolder, fileName: backupFilenames[i], sceneNames: scenesToBackup);
            else hueBridge.BackupSubApiAsCsv(backupFolder, apisToBackup[i], fileName: backupFilenames[i]);
          }
        }
      }
      #endregion

      // TODO: Finnish defining DIMMER states [and if needed some more programming, but it should mostly be done]
      // TODO: Finnish defining the actual states for TRANSITIONS (e.g. do this from 16:00 to 20:30 with transitions every 15 minutes) [and if needed some more programming, but it should mostly be done]
      // TODO: Add scenes that should be available for manual triggering (e.g. from the app)
      // TODO: Add functionality for DIMMER-rules that triggers specific scenes if _isCycling is true, AND a similar trigger when off has been pressed
      // (e.g. if I press off, then for the next 10 seconds all four buttons can trigger special states)
      // For the bathroom, this might give an opportunity to trigger "night toilet mode" (VERY low light) or "going to bed mode" (low light, but still possible to see a bit)
      // For the living room, this might give an opportunity to also turn of the corner light - or some other logic that might be useful...

      #region Update bridge
      if (updateDimmers) {
        var dimmers = AppModel.Hardcoded.MySwitchConfigs.GetButtonSetup();
        dimmers.SaveToBridge(printInfo, pauseBeforeUpdating, deleteFirst: deleteElementsBeforeUpdatingBridge, pauseBeforeDeleting: pauseBeforeDeleting);
      }
      if (updateTransitions) {
        var transitionRules = AppModel.TransitionRules.TransitionRuleList.GetTransitionRules(configFolder, transitionRulesDefsFile, transitionRulesDefsStatesFile, standardWeekDays);
        transitionRules.SaveToBridge(printInfo, pauseBeforeUpdating, deleteFirst: deleteElementsBeforeUpdatingBridge);
      }
      if (updateTransitionScenesOnly) {
        var transitionRules = AppModel.TransitionRules.TransitionRuleList.GetTransitionRules(configFolder, transitionRulesDefsFile, transitionRulesDefsStatesFile, standardWeekDays);
        transitionRules.SaveToBridge(printInfo, pauseBeforeUpdating, deleteFirst: deleteElementsBeforeUpdatingBridge && !createJsonOnlyOnSave, scenesOnly: true, createJsonOnlyOnSave: createJsonOnlyOnSave);
      }
      if (updateSwitchScenes) {
        var sceneDefs = SceneDefinitionList.GetFromDataFiles(configFolder, updateSwitchSceneDefsFile, updateSwitchSceneDefsStatesFile, "ID", Program.hueBridge.PrettyPrintIntProps, null);
        sceneDefs.UpdateSceneSet(printInfo, printBridgeInfo, pauseBeforeUpdating);
      }
      #endregion
      // *** AND THEN WE ARE DONE (with the basics)!!! ***

      Console.WriteLine();
      Console.WriteLine();
      Console.WriteLine("---------------Done---------------");
      // Console.ReadLine();
    }

    #endregion
    #region ### Helper Methods
    public static string PadString(string stringToPad) {
      return PadString(stringToPad, false);
    }
    public static string PadString(string stringToPad, bool AllowLongerValues) {
      return Program.PadString(stringToPad, AllowLongerValues, Program.FieldLengthConfigInt, ' ');
    }
    public static string PadString(string stringToPad, bool allowLongerValues, int padToLength, char PaddingCharacter) {
      if (!allowLongerValues && stringToPad.Length > padToLength) throw new ArgumentOutOfRangeException("Supplied string (" + stringToPad + ") is more than " + padToLength + ", which is not allowed!");
      string paddedString = stringToPad;
      for (int i = padToLength; i > stringToPad.Length; i--) { paddedString = PaddingCharacter + paddedString; }
      return paddedString;
    }
    #endregion
    #region Old code
    #region TestCodeFromWeek3
    private static void DoStuff3() {
      string backupFolder = @"C:\Temp\HueBackups\";
      // var kontorGroup = hueBridge.Groups["11"];
      // var testScene = hueBridge.Scenes["17ffzFbT863xw0T"];

      // var testScene1 = hueBridge.Scenes["HPEHU0akGC0P7aB"];
      // var testScene2 = hueBridge.Scenes["Wsnn0BlKUptZbHS"];

      // hueBridge.BackupSceneDefinitions(backupFolder);
      var rules = hueBridge.GetRulesFromCsv(backupFolder + @"TestData\");
      var rule1 = rules.First().Value;
      // if (rule1.ID == null || !hueBridge.Rules.ContainsKey(rule1.ID)) rule1.Create(true, true);
      // else rule1.Update(true, true);
      // rule1.addto
      // Console.WriteLine(rules.First().Key);

      var schedules = hueBridge.GetSchedulesFromCsv(backupFolder + @"TestData\");
      var schedule1 = schedules.First().Value;
      if (schedule1.ID == null || !hueBridge.Alarms.ContainsKey(schedule1.ID)) schedule1.Create(true, true);
      // else schedule1.Update(true, true);
      var sensors = hueBridge.GetSensorsFromCsv(backupFolder + @"TestData\");
      var sensor1 = sensors.First().Value;
      // if (sensor1.ID == null || !hueBridge.Sensors.ContainsKey(sensor1.ID)) sensor1.Create(true, true);
      // else sensor1.Update(true, true);

      /*
      // hueBridge.BackupScenesToCsv(backupFolder);
      // FileData.CreateCsvFiles(hueBridge.GetJsonFromBridge("/scenes/"), "scenes", @"C:\Temp\HueBackups\");
      // var tmp0 = hueBridge.GetScenesFromCsv(backupFolder);
      // TODO: Save and load rules to/from CSV
      hueBridge.BackupSubApiAsCsv(backupFolder, Bridge.hueApi.rules);
      var tmp1 = hueBridge.GetRulesFromCsv(backupFolder);
      // TODO: Save and load timers to/from CSV
      hueBridge.BackupSubApiAsCsv(backupFolder, Bridge.hueApi.schedules);
      var tmp2 = hueBridge.GetSchedulesFromCsv(backupFolder);
      // TODO: Save and load sensors to/from CSV (use methods made for scenes as a template, should be pretty quick)
      hueBridge.BackupSubApiAsCsv(backupFolder, Bridge.hueApi.sensors);
      var tmp3 = hueBridge.GetSensorsFromCsv(backupFolder);
      // Get scenedefinitions from files
      // var tmp4 = HardcodedSceneDefinitionList.GetFromDataFiles(backupFolder, "MyScenes.csv", "MySpecialLights.csv", "ID", null, null);
      */
    }
    #endregion
    #region TestCodeFromWeek2
    private static string GetSceneDefintionCode(Scene sceneWithoutDetails) {
      var scene = sceneWithoutDetails.GetDetails();
      Console.WriteLine("Saving definiton of " + scene.Name);
      var exampleColor = scene.Lights.First(i => i.ColorCapabilities == ColorMode.hs);
      var exampleTemperature = scene.Lights.First(i => i.ColorCapabilities == ColorMode.ct);
      var exampleDimOnly = scene.Lights.First(i => i.ColorCapabilities == null);
      string CreateScenesCsv = "";
      CreateScenesCsv += scene.ID + ";" + scene.Name + ";"
        + (exampleColor.HueColor == null ? "null" : exampleColor.HueColor.ToString()) + ";"
        + (exampleColor.Saturation == null ? "null" : exampleColor.Saturation.ToString()) + ";"
        + (exampleColor.ColorTemperature == null ? "null" : exampleColor.ColorTemperature.ToString()) + ";"
        + (exampleTemperature.ColorTemperature == null ? "null" : exampleTemperature.ColorTemperature.ToString()) + ";"
        + exampleColor.Brightness.ToString() + ";"
        + exampleTemperature.Brightness.ToString() + ";"
        + exampleDimOnly.Brightness.ToString() + ";"
        + (exampleColor.TransitionTime == null ? "null" : exampleColor.TransitionTime.ToString())
        + Environment.NewLine;
      /*CreateScenesCode += "list.Add(new HardcodedScene" +
        "Definition(\"" + scene.ID + "\",\"" + scene.Name + "\","
        + (exampleColor.HueColor == null ? "null" : exampleColor.HueColor.ToString()) + ","
        + (exampleColor.Saturation == null ? "null" : exampleColor.Saturation.ToString()) + ","
        + (exampleColor.ColorTemperature == null ? "null" : exampleColor.ColorTemperature.ToString()) + ","
        + (exampleTemperature.ColorTemperature == null ? "null" : exampleTemperature.ColorTemperature.ToString()) + ","
        + exampleColor.Brightness.ToString() + ","
        + exampleTemperature.Brightness.ToString() + ","
        + exampleDimOnly.Brightness.ToString() + ","
        + (exampleColor.TransitionTime == null ? "null" : exampleColor.TransitionTime.ToString()) + ","
        + ");" + Environment.NewLine;*/
      return CreateScenesCsv;
    }
    private static void DoStuff2() {
      var kontorGroup = hueBridge.Groups["11"];
      var testScene = hueBridge.Scenes["17ffzFbT863xw0T"];
      // Sensor.CreateBoolSensor("Torgeir test 1", "test1", true);
      // var myAction = new TriggerSceneAction(kontorGroup, testScene);
      // var myAction2 = new TriggerStateAction(kontorGroup, testScene.Lights.First());
      // Alarm.Create("Torgeir test 7", "Test fra Visual Studio", "W002/T11:00:00A01:55:00", myAction2);

      var rule = new Rule("Regel fra Visual Studio #1");
      // rule.AddConditionTrigger("Torgeir test 1", true);
      // rule.AddConditionButtonValue("HovedBryter", dButton.On, bState.initial_press);
      // rule.AddActionSetIntSensorValue("LeseStatus", 0);
      rule.AddActionSceneRecall("Kontor", testScene);
      // rule.Create(true, true);
      var myRule = Rule.GetByName("Regel fra Visual Studio #1");
      // myRule.Actions.Remove(myRule.Actions.First(i => i is TriggerSceneAction));
      myRule.AddActionSetGroupState("Kontor", new LightStateChanger { Brightness = 100, IsOn = true, ColorTemperature = 100 });
      myRule.Update(true, true);

      // hueBridge.BackupScenes(@"C:\temp\Scener\");
      // Scene.Create("TorgeirsDevTestScene", new[] { "6", "14" }, 150);
      // var tmp = hueBridge.Scenes["r4SD-R0n4udizHY"];
      // foreach (var light in hueBridge.Lights.Values) { Console.WriteLine(light.ID + ": " + light.Name); }
      // var FirstFloorGroup = hueBridge.Groups.First(i => i.Value.Name == "Første etasje").Value;
      // var WakeUpGroup = hueBridge.Groups.First(i => i.Value.Name == "Alle lys (som styres felles)").Value;
      // MyScenes.UpdateSceneSet(MyScenes.WakeUpScenes, WakeUpGroup);

      // string csv = MyScenes.EveningScenes.SerializeCsv();
      // var sceneList = HardcodedSceneDefinitionList.DeserializeCsv(csv);

      // var scheduleTimerTimeValues = String.Join(Environment.NewLine, hueBridge.Timers.Select(i => i.Value.strTime));
      // var scheduleAlarmTimeValues = String.Join(Environment.NewLine, hueBridge.Alarms.Select(i => i.Value.strTime));
      // var weekValues = String.Join(Environment.NewLine, hueBridge.Schedules.Where(i => i.Value.strTime.StartsWith('W')).Select(i => new WeekdayBitmask(i.Value)).Select(i => i.strDecimal + "\t" + i.strBinary + "\t" + i.SelectedDays.ToString()));

      // var tmpDays = new WeekdayBitmask(new Alarm());
      // tmpDays.SelectedDays = WeekDays.Monday | WeekDays.Tuesday | WeekDays.Sunday;

      // var tmpSingleAlarm = new Alarm();
      // var tmpRecurringAlarm = new Alarm();
      // tmpRecurringAlarm.strTime = "W124/T17:30:00";
      // tmpSingleAlarm.strTime = "2020-05-08T02:45:00";
      // tmpSingleAlarm.SingleAlarmTime = new DateTime(2020, 05, 17, 16, 30, 00);
      // Console.WriteLine(tmpSingleAlarm.strTime);
      // tmpRecurringAlarm.RecurringAlarmTime = new TimeSpan(17, 15, 00);
      // Console.WriteLine(tmpRecurringAlarm.strTime);
      // tmpRecurringAlarm.WeekDayBitmask.SelectedDays = WeekDays.Friday | WeekDays.Monday;
      // Console.WriteLine(tmpRecurringAlarm.strTime);

      // tmpSingleAlarm.RecurringAlarmTime = new TimeSpan(17, 15, 00);
      // Console.WriteLine(tmpSingleAlarm.strTime);
      // tmpSingleAlarm.WeekDayBitmask.SelectedDays = WeekDays.Friday | WeekDays.Monday;
      // Console.WriteLine(tmpSingleAlarm.strTime);
      // tmpRecurringAlarm.SingleAlarmTime = new DateTime(2020, 05, 17, 16, 30, 00);
      // Console.WriteLine(tmpRecurringAlarm.strTime);

      // var mySceneToRecall = GroupRecallSceneData.GetRecallSceneData(testScene, kontorGroup);



      // var textSettings = new TextEncoderSettings();
      // textSettings.AllowRanges(UnicodeRanges.All);

      // var obj = JsonEncodedText.Encode(myAction.ActionJson, JavaScriptEncoder.Create(textSettings));
      // var myRule = new Rule();
      // myRule.Actions.Add(myAction);
      // myRule.Actions.Add(myAction2);
      // myRule.Actions.Add(myAction);

      // var tmp = JsonSerializer.Serialize(hueBridge.Groups.First());

      // var json = JsonSerializer.Serialize(myRule, new JsonSerializerOptions() { WriteIndented = true });
      // var obj = JsonSerializer.Deserialize<Rule>(json, null);

      // myAction.Trigger(true, true);
      // mySceneToRecall.Recall(true, true);

      // var myAction = new LightState() { IsOn = true };
      // Console.WriteLine(JsonSerializer.Serialize<LightState>(myAction, new JsonSerializerOptions() { WriteIndented = true }));


      // var tmp = Scenes["7BeDynpBAuK420n"];
      // string SceneID = "fcPo2vMOyBelLX6";
    }
    #endregion
    #region TestCodeFromWeek1
    private static void DoStuff1() {
      var Kveldslys = hueBridge.Scenes.Values.Where(i => i.Name.Contains("Kveldslys")).OrderBy(i => i.Name);
      var MorgenLys = new List<Scene>();
      MorgenLys.Add(hueBridge.Scenes.Values.First(i => i.Name == "0445 Natt 0445"));
      MorgenLys.Add(hueBridge.Scenes.Values.First(i => i.Name == "0445 Nesten natt 0500"));
      MorgenLys.Add(hueBridge.Scenes.Values.First(i => i.Name == "0500 God morgen 0515"));
      MorgenLys.Add(hueBridge.Scenes.Values.First(i => i.Name == "0515 Tid for å våkne 0545"));
      MorgenLys.Add(hueBridge.Scenes.Values.First(i => i.Name == "0545 Rolig morra 0600"));
      MorgenLys.Add(hueBridge.Scenes.Values.First(i => i.Name == "0620 Konsentrer deg 0650"));
      foreach (var sceneFromList in MorgenLys) {
        string CreateScenesCode = GetSceneDefintionCode(sceneFromList);
        // if (sceneFromList.SceneType == "GroupScene") continue;

        // string id, string name, int hueColor, int saturation, int colorTemperatureColor, int colorTemperatureAmbience, int brightnessColor, int brightnessAmbience, int brightnessDimmerOnly, int transitionTime
        /*
          foreach (var light in FirstFloorGroup.Lights) {
            scene.AddLight(light.ID, 100);
          }
          Console.WriteLine("Updating: " + scene.Name);
          Console.WriteLine("Type: " + scene.SceneType + (scene.SceneType == "GroupScene" ? " (" + scene.GroupName + ")" : ""));
          Console.WriteLine("Color values:      " + exampleColor.TransitionTime + " / " + exampleColor.Brightness + " / " + exampleColor.ColorTemperature + " / " + exampleColor.HueColor + " / " + exampleColor.Saturation);
          scene.SetCommonStateColorOnly(true, exampleColor.Brightness, exampleColor.HueColor, exampleColor.Saturation, exampleColor.ColorTemperature, exampleColor.TransitionTime);
          Console.WriteLine("Ambience values:   " + exampleTemperature.TransitionTime + " / " + exampleTemperature.Brightness + " / " + exampleTemperature.ColorTemperature);
          scene.SetCommonStateAmbienceOnly(true, exampleTemperature.Brightness, exampleTemperature.ColorTemperature, exampleTemperature.TransitionTime);
          Console.WriteLine("Dimmer values:     " + exampleDimOnly.TransitionTime + " / " + + exampleDimOnly.Brightness);
          scene.SetCommonStatePureDimmersOnly(true, exampleDimOnly.Brightness, exampleDimOnly.TransitionTime);
          Console.WriteLine("Lights in scene: " + String.Join(',', scene.LightIDs));
          Console.ReadLine();
          // scene.Update(false);
          // scene.Update(false); // Sometimes the first update does not work (I think that is when adding lights...)
          */
      }
      // tmp.AddLight("16", 100);
      // tmp.Lights.Add(hueBridge.Lights["13"].state);
      // tmp.SetCommonState(175, 40000, 255, 300, 100);
      // tmp.Update();
    }
    #endregion
    #endregion
  }
}