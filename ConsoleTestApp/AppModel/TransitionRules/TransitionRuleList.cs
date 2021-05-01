using ConsoleTestApp.ApiObjects.Schedules;
using ConsoleTestApp.ApiObjects.Sensors;
using ConsoleTestApp.ApiObjects.Sensors.State;
using ConsoleTestApp.AppModel.Hardcoded;
using ConsoleTestApp.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ConsoleTestApp.AppModel.TransitionRules {
  #region TransitionRuleList
  class TransitionRuleList : List<TransitionRule> {
    #region ### Instance methods
    #region AddRule
    public TransitionRule AddRule(string commonDescription, TimeSpan startTime, WeekDays weekdays, List<SceneDefinition> sceneDefs, Sensor<IntSensorState> currentTransRuleTracker, Sensor<BoolSensorState> houseState, int transRuleID) {
      var transition = new TransitionRule(commonDescription, startTime, weekdays, sceneDefs, currentTransRuleTracker, houseState, transRuleID);
      Add(transition);
      return transition;
    }
    #endregion
    #region SaveToBridge
    public void SaveToBridge(bool printInfo, bool pauseBeforeUpdating, bool deleteFirst) {
      foreach (var rule in this) {
        // TODO: Calculate if the rule should stay at it's final state (e.g. get that from config...)
        rule.SaveToBridge(stayAtFinalState: true, printInfo, pauseBeforeUpdating, deleteFirst);
      }
    }
    #endregion
    #endregion
    #region ### Static methods
    #region GetCurrentTransRuleTracker
    public static Sensor<IntSensorState> GetCurrentTransRuleTracker(string groupName) {
      string trackSensorName = ("FadegroupTracker_" + groupName).FixNorwegianChars().Truncate(32);
      var trackSensor = Program.hueBridge.Sensors.Values.FirstOrDefault(i => i.Name == trackSensorName);
      if (trackSensor != null && trackSensor is Sensor<IntSensorState>) {
        return (Sensor<IntSensorState>)trackSensor;
      }
      else {
        return Sensor.CreateIntSensor(trackSensorName, "", 0, true, true);
      }
    }
    #endregion
    #region GetTransitionRules
    public static TransitionRuleList GetTransitionRules(string dataFolder, string sceneListFilename, string specialLightStatesFileName, WeekDays standardWeekDays) {
      var houseState = MySwitchConfigs.GetHouseStatusSensor();
      // var transTracker = GetCurrentTransRuleTracker();

      var list = new TransitionRuleList();
      var sceneDefs = SceneDefinitionList.GetFromDataFiles(dataFolder, sceneListFilename, specialLightStatesFileName, "ID", Program.hueBridge.PrettyPrintIntProps, null);
      var trackerGroups = sceneDefs.GroupBy(i => i.Value.FadeGroup).Select(i => i.Key);
      var trackers = new Dictionary<string, Sensor<IntSensorState>>();
      foreach (var tracker in trackerGroups) {
        trackers.Add(tracker, GetCurrentTransRuleTracker(tracker));
      }
      var rulegroups = sceneDefs.GroupBy(i => i.Value.Name).ToList();
      // int groupNumber = 1;
      for (int i = 0; i < rulegroups.Count; i++) {
        var rule = rulegroups[i];
        if (rule.First().Value.Order == null) continue; // Not a definition meant for transition groups!
        var firstTransition = rule.Select(i => i.Value).FirstOrDefault(i => i.Order == 1);
        var startTime = firstTransition?.InitialStartTime?.TimeOfDay ?? new TimeSpan(15, 0, 0);
        // bool IsIndependent = firstTransition?.IsIndependent ?? false;
        list.AddRule(rule.Key, startTime, standardWeekDays, rule.Select(i => i.Value).ToList(), trackers[rule.First().Value.FadeGroup], houseState, i);
        // groupNumber++;
      }
      return list;
    }
    #endregion
    #endregion
  }
  #endregion
}
