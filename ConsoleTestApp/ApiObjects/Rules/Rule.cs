using ConsoleTestApp.ApiObjects.Groups;
using ConsoleTestApp.ApiObjects.Lights.State;
using ConsoleTestApp.ApiObjects.Rules.Actions;
using ConsoleTestApp.ApiObjects.Scenes;
using ConsoleTestApp.ApiObjects.Schedules;
using ConsoleTestApp.ApiObjects.Sensors;
using ConsoleTestApp.ApiObjects.Sensors.State;
using ConsoleTestApp.Helpers;
using ConsoleTestApp.JsonConverters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace ConsoleTestApp.ApiObjects.Rules {
  public class Rule : IApiObject {
    #region ### Instance properties
    [JsonIgnore]
    public string ID { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("owner")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenNull)]
    public string Owner { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string OwnerFriendlyAppName {
      get {
        if (Program.hueBridge.ConnectedApps.ContainsKey(Owner)) return Program.hueBridge.ConnectedApps[Owner].AppName;
        else return "Unknown";
      }
    }
    [JsonPropertyName("created")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenNull)]
    public DateTime? Created { get; set; }
    [JsonPropertyName("lasttriggered")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenNull)]
    [JsonConverter(typeof(DateTimeWithNoneJsonConverter))]
    public DateTime? LastTriggered { get; set; }
    [JsonPropertyName("timestriggered")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenNull)]
    public int? TimesTriggered { get; set; }
    [JsonPropertyName("status")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenNull)]
    public string Status { get; set; }
    [JsonPropertyName("recycle")]
    public bool CanBeAutoRecycled { get; set; }
    [JsonPropertyName("conditions")]
    public List<RuleCondition> Conditions { get; set; }
    [JsonPropertyName("actions")]
    [JsonConverter(typeof(RuleActionListJsonConverter))]
    public List<RuleActionBase> Actions { get; set; }
    [JsonIgnore]
    public bool IsCreatedFromCode { get; set; }
    #endregion
    #region ### Constructor
    public Rule() {
      this.Conditions = new List<RuleCondition>();
      this.Actions = new List<RuleActionBase>();
    }
    public Rule(string name) : this() {
      string ruleName = name.FixNorwegianChars();
      int encodeExtraChars = System.Web.HttpUtility.JavaScriptStringEncode(ruleName, false).Length - ruleName.Length;
      this.Name = ruleName.Truncate(32 - encodeExtraChars);
    }
    #endregion
    #region ### Instance methods
    #region AddCondition
    public void AddConditionTrigger<T>(Sensor sensor, T value) {
      var cond = new RuleCondition(sensor);
      cond.Operator = RuleConditionOperator.dx;
      this.Conditions.Add(cond);
      this.AddConditionValueEquals(sensor, value);
    }
    public void AddConditionDelayedTrigger<T>(Sensor sensor, T value, TimeSpan delay) {
      var cond = new RuleCondition(sensor) {
        Operator = RuleConditionOperator.ddx,
        Value = delay.ToString(@"\P\Thh\:mm\:ss")
      };
      this.Conditions.Add(cond);
      this.AddConditionValueEquals(sensor, value);
    }
    public void AddConditionButtonTrigger(Sensor sensor, dButton button, bState state) {
      this.AddConditionTrigger(sensor, ((int)button * 1000) + (int)state);
    }
    public void AddConditionButtonValue(Sensor sensor, dButton button, bState state) {
      this.AddConditionValueEquals(sensor, ((int)button * 1000) + (int)state);
    }
    public void AddConditionValueEquals<T>(Sensor sensor, T value) {
      var cond = new RuleCondition(sensor);
      cond.Operator = RuleConditionOperator.eq;
      cond.Value = value.ToString().ToLower();
      this.Conditions.Add(cond);
    }
    public void AddConditionTimeSlot(bool inside, TimeSpan startTime, TimeSpan endTime) {
      var cond = new RuleCondition();
      cond.Operator = inside ? RuleConditionOperator.time_in : RuleConditionOperator.time_not_in;
      // "value": "T05:00:00/T22:00:00"
      cond.Value = startTime.ToString(@"\Thh\:mm\:ss") + "/" + endTime.ToString(@"\Thh\:mm\:ss");
      this.Conditions.Add(cond);
    }
    #endregion
    #region AddAction
    public void AddActionSceneRecall(LightGroup group, Scene scene) {
      this.Actions.Add(new TriggerSceneAction(group, scene));
    }
    public void AddActionSceneRecall(string groupName, string sceneName) {
      this.Actions.Add(new TriggerSceneAction(groupName, sceneName));
    }
    public void AddActionSceneRecall(string groupName, Scene scene) {
      this.Actions.Add(new TriggerSceneAction(groupName, scene));
    }
    public void AddActionSetGroupState(string groupName, LightStateChanger state) {
      this.Actions.Add(new TriggerStateAction(groupName, state));
    }
    public void AddActionSetGroupAlertState(string groupName, AlertState state) {
      this.Actions.Add(new TriggerAlertAction(groupName, state));
    }
    public void AddActionStartTimer(Timer timer) {
      this.Actions.Add(new TimerAction(timer, EnabledState.enabled));
    }
    public void AddActionStopTimer(Timer timer) {
      this.Actions.Add(new TimerAction(timer, EnabledState.disabled));
    }
    public void AddActionSetBoolSensorValue(Sensor<BoolSensorState> sensor, bool value) {
      this.Actions.Add(new BoolSensorAction(sensor, value, false));
    }
    public void AddActionSetIntSensorValue(Sensor<IntSensorState> sensor, int value) {
      this.Actions.Add(new IntSensorAction(sensor, value, false));
    }
    #endregion
    #region Create
    public void Create(bool printInfo, bool pauseBeforeUpdating) {
      var owner = this.Owner;
      var created = this.Created;
      var timetriggered = this.TimesTriggered;
      this.Owner = null;
      this.Created = null;
      this.TimesTriggered = null;
      if (this.ID != null) throw new Exception("Rule already created!");
      string id = Program.hueBridge.AddToBridge("/rules/", this, printInfo, printInfo, pauseBeforeUpdating);
      this.ID = id;
      Program.hueBridge.Rules.Add(this.ID, this);
      this.Owner = owner;
      this.Created = created;
      this.TimesTriggered = timetriggered;
    }
    #endregion
    #region CreateIfMissingHijackIfExisting
    public void CreateIfMissingHijackIfExisting(bool printInfo, bool pauseBeforeUpdating) {
      var oldRule = Program.hueBridge.Rules.Values.FirstOrDefault(i => i.Name == this.Name && !i.IsCreatedFromCode);
      // If there already exists a rule in the bridge with this name, we "hijack" it and send an update from our new rule
      if (oldRule != null) {
        this.ID = oldRule.ID;
        this.Update(printInfo, pauseBeforeUpdating);
        Program.hueBridge.Rules[this.ID] = this;
      }
      else {
        this.Create(printInfo, pauseBeforeUpdating);
        this.IsCreatedFromCode = false;
      }
    }
    #endregion
    #region Update
    public void Update(bool printInfo, bool pauseBeforeUpdating) {
      var owner = this.Owner;
      var created = this.Created;
      var timetriggered = this.TimesTriggered;
      this.Owner = null;
      this.Created = null;
      this.TimesTriggered = null;
      Program.hueBridge.UpdateBridge("/rules/" + ID + "/", this, printInfo, printInfo, pauseBeforeUpdating);
      this.Owner = owner;
      this.Created = created;
      this.TimesTriggered = timetriggered;
    }
    #endregion
    #endregion
    #region ### Static methods
    // Could for example be named:  Kveldslys +1t15min Kontor
    // Or:                          FadeTransTest !start >trigger
    // Or:                          FadeKveldslys 1t15min Kj\u00F8kken
    public static Rule GetTransitionRule(string transLongName, string groupName) {
      transLongName = transLongName.FixNorwegianChars();
      groupName = groupName.Replace(" (\"hemmelig\" systemgruppe)", "").FixNorwegianChars();
      var transID = transLongName.Substring(transLongName.LastIndexOf(' ') + 1);
      var transShortName = transLongName.Substring(0, transLongName.LastIndexOf(' '));
      int encodeExtraChars = System.Web.HttpUtility.JavaScriptStringEncode(groupName, false).Length - groupName.Length;
      int usedLength = transShortName.Length + transID.Length + 2 + encodeExtraChars;
      groupName = groupName.Truncate(32 - usedLength);
      string ruleName = transShortName + " " + transID + " " + groupName;
      return new Rule(ruleName);
    }
    #region GetButtonRule
    [JsonIgnore]
    public string switchName { get; set; }
    [JsonIgnore]
    public string buttonName { get; private set; }
    [JsonIgnore]
    public string stateName { get; private set; }
    // First ID-part in the rule name, comes between switchname and buttonname (rule name could be "BryterRoom ID1_Button State (ID2)")
    [JsonIgnore]
    public string ruleID1 { get; set; }
    // Second ID-part in the rule name, comes last in the name with a paranthesis (rule name could be "BryterRoom ID1_Button State (ID2)")
    [JsonIgnore]
    public string ruleID2 { get; set; }
    [JsonIgnore]
    public bool IsButtonRule { get; private set; }
    // Could for example be named: BryterKontor On Short (Scene#1)
    public static Rule GetButtonRule(Sensor _switch, dButton button, bState state, string ruleID1, string ruleID2 = null, string altBtnName = null) {
      return new Rule("") {
        IsButtonRule = true,
        switchName = _switch.Name.FixNorwegianChars(),
        ruleID1 = ruleID1.FixNorwegianChars(),
        buttonName = altBtnName ?? button.GetDescription(),
        stateName = state.GetDescription(),
        ruleID2 = ruleID2.FixNorwegianChars()
      };
    }
    public static void SetButtonRuleNames(IEnumerable<Rule> buttonRules) {
      int maxSwitchNameLength = buttonRules.Max(i => System.Web.HttpUtility.JavaScriptStringEncode(i.switchName, false).Length);
      int maxRuleID1NameLength = buttonRules.Max(i => i.ruleID1 == null ? 0 : System.Web.HttpUtility.JavaScriptStringEncode(i.ruleID1, false).Length) + 1; // Add space
      int maxButtonNameLength = buttonRules.Max(i => i.buttonName.Length);
      int maxStateNameLength = buttonRules.Max(i => i.stateName.Length);
      int maxRuleID2NameLength = buttonRules.Max(i => i.ruleID2?.Replace("Scene", "Sc").Length ?? 0) + 3;// Add space and parenthesis...

      // NOTE: SOME COMMENTS ARE OLD!!!
      // We must keep the Rule name below 32 characters
      // - State is max 6 chars
      // - We have two or three spaces and two paranthesis
      // - Button is 7 at max
      // int usedSpace = maxButtonNameLength + maxStateNameLength + 2 + maxRuleID1NameLength + maxRuleID2NameLength; // Can be 18 at absolute max!
      // int usedSpaceWithRuleID = usedSpace + maxRuleID1NameLength + maxRuleID2NameLength;
      // int maxLengthSwitchName = 32 - usedSpaceWithRuleID;
      int maxLengthSwitchName = 32 - (maxRuleID1NameLength + maxButtonNameLength + maxStateNameLength + maxRuleID2NameLength + 2);
      bool truncateSwitchName = false;
      if (maxLengthSwitchName < 10) {
        maxLengthSwitchName = 10;
        truncateSwitchName = true;
      }
      int maxLengthRuleID1Name = 32 - (maxLengthSwitchName + maxButtonNameLength + maxStateNameLength + maxRuleID2NameLength + 2);
      bool truncateRuleID1Name = false;
      if (maxLengthRuleID1Name < maxRuleID1NameLength) {
        truncateRuleID1Name = true;
      }

      foreach (var rule in buttonRules) {
        // Switch name is not allowed to be more than 10 characters total (if we need to save space).
        // If above, we first change "Bryter" to "Br", and then truncate. Thus we can have up to 4 characters in ruleID if everything else is maxed out.
        int switchNameEncodedLength = System.Web.HttpUtility.JavaScriptStringEncode(rule.switchName, false).Length;
        if (truncateSwitchName) rule.switchName = rule.switchName.Replace("Bryter", "Br").Truncate(maxLengthSwitchName - (switchNameEncodedLength - rule.switchName.Length));
        // int ruleID1hNameEncodedLength = System.Web.HttpUtility.JavaScriptStringEncode(rule.ruleID1, false).Length;
        if (truncateRuleID1Name) rule.ruleID1 = rule.ruleID1.Replace("Multi", "M").Replace("Cycle", "C"); // .Truncate(maxLengthRuleID1Name - (ruleID1hNameEncodedLength - rule.ruleID1.Length));

        int usedSpace = rule.switchName.Length + ((rule.ruleID1?.Length + 1) ?? 0) + rule.buttonName.Length + rule.stateName.Length + 2;
        int maxRuleID2length = 32 - (usedSpace + 3); // Space and parenthesis = 3

        // Note: if the ruleID is "Scene#x", then the X is crucial! Therefore, we change it to "Sc#x"
        // int maxRuleIDlength = 32 - (usedSpace + rule.switchName.Length);
        // if (ruleID?.Contains("Scene#") && ruleID?.Length > maxRuleIDlength) ruleID = ruleID.Replace("Scene", "Sc").TruncateStart(maxRuleIDlength);
        if (rule.ruleID2?.Length > maxRuleID2length) {
          rule.ruleID2 = rule.ruleID2.Replace("Scene", "Sc");
          if (rule.ruleID2.Contains("#")) rule.ruleID2 = rule.ruleID2.TruncateStart(maxRuleID2length);
          else rule.ruleID2 = rule.ruleID2.Truncate(maxRuleID2length);
        }
        // string ruleName = switchName + " " + (altBtnName ?? button.GetDescription()) + " " + state.GetDescription() + (string.IsNullOrEmpty(ruleID) ? "" : " (" + ruleID + ")");
        rule.Name = rule.switchName + " " + (string.IsNullOrEmpty(rule.ruleID1) ? "" : rule.ruleID1 + ((rule.ruleID1.EndsWith('_') || rule.ruleID1.EndsWith('!')) ? "" : " ")) + rule.buttonName + "-" + rule.stateName + (string.IsNullOrEmpty(rule.ruleID2) ? "" : " (" + rule.ruleID2 + ")");
        if (rule.Name.Length > 32) throw new ArithmeticException("Logic is not yet correct for rulename computation...");
      }
    }
    #endregion
    #region GetByName
    public static Rule GetByName(string name) {
      return Program.hueBridge.Rules.First(i => i.Value.Name == name).Value;
    }
    #endregion
    #endregion
  }
}
/*
	"27": {
		"name": "Bønneplass av",
		"owner": "70dc206e-3b1e-4d2c-a1f9-3aa47fce2150",
		"created": "2020-05-05T03:45:18",
		"lasttriggered": "none",
		"timestriggered": 0,
		"status": "enabled",
		"recycle": false,
		"conditions": [
			{
				"address": "/sensors/43/state/lastupdated",
				"operator": "dx"
			},
			{
				"address": "/sensors/43/state/buttonevent",
				"operator": "eq",
				"value": "3001"
			}
		],
		"actions": [
			{
				"address": "/groups/16/action",
				"method": "PUT",
				"body": {
					"on": false
				}
			}
		]
	}
*/
