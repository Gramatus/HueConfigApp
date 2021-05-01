using ConsoleTestApp.ApiObjects.Groups;
using ConsoleTestApp.ApiObjects.Lights.State;
using ConsoleTestApp.ApiObjects.Scenes;
using ConsoleTestApp.JsonConverters;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConsoleTestApp.ApiObjects.Rules.Actions {
  public class RuleActionBase<T> : RuleActionBase {
    // public T ActionData { get => JsonSerializer.Deserialize<T>(this.ActionJson); set => this.ActionJson = JsonSerializer.Serialize(value); }
    [JsonIgnore]
    public T ActionData { get; set; }
    // public T ActionData { get => (T)this.objActionData; set => WriteActionData(value); }
    // public abstract void WriteActionData(T data);
  }
  public class RuleActionData {
    [JsonExtensionData]
    public Dictionary<string, object> ExtensionData { get; set; }
    public RuleActionData() {
      this.ExtensionData = new Dictionary<string, object>();
    }
  }
  public class ApiBody : Dictionary<string, object>, IApiObject {
    [JsonIgnore]
    public string ID { get => "Action body has no ID"; }
    [JsonIgnore]
    public string Name { get => "Performing som action, look at the object in Visual Studio to get details..."; }

  }
  public class RuleActionBase : IApiObject {
    protected ApiBody _dataDict;
    [JsonIgnore]
    public string ID { get => "RuleActionBase has no ID"; }
    [JsonIgnore]
    public string Name { get => "Performing a " + this.Method.ToString() + " to address" + this.AddressOfAction; }
    [JsonPropertyName("address")]
    public virtual string AddressOfAction { get; set; }
    [JsonPropertyName("method")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ActionMethod Method { get; set; }
    [JsonPropertyName("body")]
    public ApiBody objActionData { get => ReadActionData(); set => WriteActionData(value); }
    // public string ActionJson { get=>JsonSerializer.Serialize(this.objActionData); set =>this.objActionData = JsonSerializer.Deserialize<object>(value); }
    [JsonIgnore]
    public string ActionJson { get; set; }
    // [JsonIgnore]
    // public ActionTypes ActionType { get; protected set; }
    [JsonIgnore]
    public bool IsScheduleAction { get; set; }
    public RuleActionBase() {
      this._dataDict = new ApiBody();
    }
    public void Trigger(bool printInfo, bool pauseAfterPrintingJson) {
      if (this.Method == ActionMethod.PUT) Program.hueBridge.UpdateBridge(this.AddressOfAction, this.objActionData, printInfo, printInfo, pauseAfterPrintingJson);
      else if (this.Method == ActionMethod.POST) Program.hueBridge.AddToBridge(this.AddressOfAction, this.objActionData, printInfo, printInfo, pauseAfterPrintingJson);
      else if (this.Method == ActionMethod.DELETE) throw new NotImplementedException();
      else throw new NotImplementedException();
    }
    public virtual void WriteActionData(ApiBody data) {
      this._dataDict = data;
    }
    protected virtual ApiBody ReadActionData() { return this._dataDict; }
    protected void SafeAddToDataDict<T>(string key, T value, bool saveEmptyValues) {
      if (!saveEmptyValues && value == null) return;
      if (!saveEmptyValues && string.IsNullOrEmpty(value.ToString())) return;
      if (this._dataDict.ContainsKey(key)) this._dataDict[key] = value;
      else this._dataDict.Add(key, value);
    }
    protected int? GetIntFromDataDict(string key, Dictionary<string, object> data, bool Nullable) {
      if (data.ContainsKey(key)) {
        int v;
        if (Int32.TryParse(data[key].ToString(), out v)) return v;
        else return Nullable ? null : (int?)0;
      }
      else {
        return Nullable ? null : (int?)0;
      }
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <param name="data"></param>
    /// <param name="DefaultValueIfNotNullable">If the value is nullable, let this argument be null. Else give the suggested default value.</param>
    /// <returns></returns>
    protected bool? GetBoolFromDataDict(string key, Dictionary<string, object> data, bool? DefaultValueIfNotNullable) {
      if (data.ContainsKey(key)) {
        bool v;
        if (Boolean.TryParse(data[key].ToString(), out v)) return v;
        else return (DefaultValueIfNotNullable == null) ? null : DefaultValueIfNotNullable;
      }
      else {
        return (DefaultValueIfNotNullable == null) ? null : DefaultValueIfNotNullable;
      }
    }
    protected string GetStringFromDataDict(string key, Dictionary<string, object> data) {
      return data.ContainsKey(key) ? data[key].ToString() : null;
    }
    protected EnabledState? GetEnabledStateFromDataDict(string key, Dictionary<string, object> data) {
      return data.ContainsKey(key) ? (EnabledState?)Enum.Parse<EnabledState>(data[key].ToString()) : null;
    }
    protected AlertState GetAlertStateFromDataDict(string key, Dictionary<string, object> data, AlertState DefaultState) {
      return data.ContainsKey(key) ? (AlertState)Enum.Parse<AlertState>(data[key].ToString()) : DefaultState;
    }
  }
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
