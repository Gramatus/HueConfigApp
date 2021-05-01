using ConsoleTestApp.ApiObjects.Schedules;
using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleTestApp.AppModel {
  static class MyTransitionRules {
    public static List<TransitionStateList> GetTransitionRules(string dataFolder, string sceneListFilename, string specialLightStatesFileName) {
      var sceneDefs = SceneDefinitionList.GetFromDataFiles(dataFolder, "MyScenes.csv", "MySpecialLights.csv", "ID", Program.hueBridge.PrettyPrintIntProps, null);
      var transition1 = new TransitionStateList("Kveldslys", new TimeSpan(16, 0, 0), WeekdayBitmask.workingDays, "Første etasje");
      transition1.Add(sceneDefs.GetByName("1600 Kveldslys 1700"));
      transition1.Add(sceneDefs.GetByName("1700 Kveldslys 1715"));
      transition1.Add(sceneDefs.GetByName("1715 Kveldslys 1730"));
      transition1.Add(sceneDefs.GetByName("1730 Kveldslys 1745"));
      // transition1.AddOrUpdateToBridgeWithAlarms();
    }
    public static void AddOrUpdateToBridgeWithAlarms(this List<TransitionStateList> list) {
      foreach(var rule in list) {
        rule.AddOrUpdateToBridgeWithAlarms();
      }
    }
    public static void AddOrUpdateToBridgeWithTimers(this List<TransitionStateList> list) {
      foreach (var rule in list) {
        rule.AddOrUpdateToBridgeWithTimers();
      }
    }
  }
}
