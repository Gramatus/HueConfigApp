using ConsoleTestApp.ApiObjects.Groups;
using ConsoleTestApp.ApiObjects.Lights.State;
using ConsoleTestApp.ApiObjects.Rules;
using ConsoleTestApp.ApiObjects.Rules.Actions;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConsoleTestApp.JsonConverters {
  #region NEW implementation
  class RuleActionListJsonConverter : JsonConverter<List<RuleActionBase>> {
    public override List<RuleActionBase> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
      if (reader.TokenType != JsonTokenType.StartArray) {
        throw new JsonException();
      }
      var ruleActionList = new List<RuleActionBase>();
      while (reader.Read()) {
        if (reader.TokenType == JsonTokenType.EndArray) {
          break;
        }
        if (reader.TokenType != JsonTokenType.StartObject) {
          throw new JsonException();
        }
        reader.Read(); // Get past the StartObject token
        string propertyName1 = reader.GetString(); // address
        reader.Read();
        string address = reader.GetString();
        reader.Read();
        string propertyName2 = reader.GetString(); // method
        reader.Read();
        string strMethod = reader.GetString();
        reader.Read();
        string propertyName3 = reader.GetString(); // body
        reader.Read();
        if (reader.TokenType != JsonTokenType.StartObject) {
          throw new NotImplementedException();
        }
        var dataDict = JsonSerializer.Deserialize<ApiBody>(ref reader);
        RuleActionBase action;
        if (dataDict.ContainsKey("scene")) action = new TriggerSceneAction();
        else if (dataDict.ContainsKey("ct") || dataDict.ContainsKey("bri") || dataDict.ContainsKey("on") || dataDict.Keys.Any(i => i.Contains("_inc"))) action = new TriggerStateAction();
        // I don't think there are any actions for Alarms, or at least that has not been considered yet
        else if (address.Contains("/schedules/")) action = new TimerAction();
        // Change state type action (enable or disable a timer)
        else if (dataDict.ContainsKey("status") && (dataDict["status"].ToString() == "enabled" || dataDict["status"].ToString() == "disabled")) action = new TimerAction();
        else if (dataDict.ContainsKey("status")) action = new IntSensorAction();
        else if (dataDict.ContainsKey("flag")) action = new BoolSensorAction();
        else if (dataDict.ContainsKey("state")) action = new TriggerAlertAction();
        else if (dataDict.ContainsKey("alert")) action = new TriggerAlertAction();
        else if (dataDict.ContainsKey("storelightstate")) action = new RuleActionBase(); // This is not used by me, but by some entertainment setups. Just bypass by creating a RuleActionBase object.
        else if (dataDict.Count == 0) action = new RuleActionBase(); // This probably means something is "wrong" in the bridge, it might be caused e.g. by a group being deleted that was part of thise rule. No biggie, but it should be fixed at some time.
        else throw new NotImplementedException();
        action.AddressOfAction = address;
        action.Method = Enum.Parse<ActionMethod>(strMethod);
        action.WriteActionData(dataDict); //  = new GroupRecallSceneData() { Scene = dataDict["scene"] };
        ruleActionList.Add(action);
        if (reader.TokenType != JsonTokenType.EndObject) { throw new JsonException(); }
        else { reader.Read(); }
      }
      return ruleActionList;
    }

    public override void Write(Utf8JsonWriter writer, [DisallowNull] List<RuleActionBase> value, JsonSerializerOptions options) {
      JsonSerializer.Serialize(writer, value);
    }
  }
  #endregion
  #region OLD implementation
  /*
  class RuleActionListJsonConverterOLD : JsonConverter<List<RuleActionBase>> {
    public override List<RuleActionBase> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
      if (reader.TokenType != JsonTokenType.StartArray) {
        throw new JsonException();
      }
      var ruleActionList = new List<RuleActionBase>();
      while (reader.Read()) {
        if (reader.TokenType == JsonTokenType.EndArray) {
          break;
        }
        if (reader.TokenType != JsonTokenType.StartObject) {
          throw new JsonException();
        }
        reader.Read(); // Get past the StartObject token
        string propertyName1 = reader.GetString(); // address
        reader.Read();
        string address = reader.GetString();
        reader.Read();
        string propertyName2 = reader.GetString(); // method
        reader.Read();
        string strMethod = reader.GetString();
        reader.Read();
        string propertyName3 = reader.GetString(); // body
        reader.Read();
        if (reader.TokenType != JsonTokenType.StartObject) {
          throw new NotImplementedException();
        }
        // the data may be many types of objects, we have to try deserializing all types until we find the right kind...
        object data = null;
        var dataTriggerSceneAction = JsonSerializer.Deserialize<GroupRecallSceneData>(ref reader);
        var dataTriggerStateAction = JsonSerializer.Deserialize<LightState>(ref reader);
        if (dataTriggerSceneAction.Scene != null) {
          var action = new TriggerSceneAction();
          action.AddressOfAction = address;
          action.Method = Enum.Parse<ActionMethod>(strMethod);
          action.ActionData = (GroupRecallSceneData)data;
        }
        else if (dataTriggerStateAction.IsOn != null) {
          var action = new TriggerStateAction();
          action.AddressOfAction = address;
          action.Method = Enum.Parse<ActionMethod>(strMethod);
          action.ActionData = (LightState)data;
        }
        else {
          throw new NotImplementedException();
        }
        if (address.Contains("/groups/")) {
        }
        else if (address.Contains("/something/")) {
          throw new NotImplementedException();
        }
        else {
          throw new NotImplementedException();
        }
        if (reader.TokenType != JsonTokenType.EndObject) { throw new JsonException(); }
        else { reader.Read(); }
      }
      return ruleActionList;
    }

    public override void Write(Utf8JsonWriter writer, [DisallowNull] List<RuleActionBase> value, JsonSerializerOptions options) {
      writer.WriteStartArray();
      foreach (var action in value) {
        if (action.ActionType == ActionTypes.TriggerSceneAction) JsonSerializer.Serialize(writer, (TriggerSceneAction)action, options);
        else if (action.ActionType == ActionTypes.TriggerStateAction) JsonSerializer.Serialize(writer, (TriggerStateAction)action, options);
      }
      writer.WriteEndArray();
      // string serializedValue = JsonSerializer.Serialize(value.ToArray());
      // writer.WriteStringValue(serializedValue);
    }
  }
  */
  #endregion
}
/*
{
				"address": "/groups/16/action",
				"method": "PUT",
				"body": {
					"on": false
				}
			}
*/
