using ConsoleTestApp.ApiObjects.Config;
using ConsoleTestApp.ApiObjects.Groups;
using ConsoleTestApp.ApiObjects.Lights;
using ConsoleTestApp.ApiObjects.Lights.State;
using ConsoleTestApp.ApiObjects.Rules;
using ConsoleTestApp.ApiObjects.Rules.Actions;
using ConsoleTestApp.ApiObjects.Scenes;
using ConsoleTestApp.ApiObjects.Schedules;
using ConsoleTestApp.ApiObjects.Sensors;
using ConsoleTestApp.Helpers;
using ConsoleTestApp.Helpers.Csv;
using Microsoft.VisualBasic.CompilerServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ConsoleTestApp.AppModel {
  public class Bridge {
    #region ### Instance Fields
    private readonly string RESTfulRoot;
    private Config config;
    public readonly string[] PrettyPrintIntProps = new string[] { "HueColor", "Saturation", "ColorTemperatureColor", "ColorTemperatureAmbience", "BrightnessColor", "BrightnessAmbience", "BrightnessDimmerOnly", "TransitionTime" };
    private readonly string[] _dictProperties = new string[] { "lightstates", "whitelist" };
    private readonly string[] _complexJsonProps = new string[] { "lights", "xy", "capabilities|inputs" };
    private readonly string _colNameID = "ID";
    private readonly System.IO.DirectoryInfo _logDir;
    private bool isInitialized = false;
    #endregion
    #region ### Instance Properties
    public Dictionary<string, Light> Lights { get; private set; }
    public SceneList Scenes { get; private set; }
    public Dictionary<string, LightGroup> Groups { get; private set; }
    public Dictionary<string, Rule> Rules { get; private set; }
    public Dictionary<string, Sensor> Sensors { get; private set; }
    public Dictionary<string, Timer> Timers { get; private set; }
    public Dictionary<string, Alarm> Alarms { get; private set; }
    public readonly HttpClient client = new HttpClient();
    public Dictionary<string, WhitelistEntry> ConnectedApps { get { return config.ConnectedApps; } }
    #endregion
    #region ### Constructor
    public Bridge(string RESTfulRoot, string logDir) {
      this.RESTfulRoot = RESTfulRoot;
      var logDirBase = new System.IO.DirectoryInfo(logDir);
      this._logDir = logDirBase.CreateSubdirectory(DateTime.Now.ToString("yyMMdd")).CreateSubdirectory(DateTime.Now.ToString("HHmm"));
    }
    #endregion
    #region ### Functionality
    #region Prettify
    public string Prettify(string json) {
      var document = JsonDocument.Parse(json);
      var stream = new MemoryStream();
      var writer = new Utf8JsonWriter(stream, new JsonWriterOptions() { Indented = true });
      document.WriteTo(writer);
      writer.Flush();
      return Encoding.UTF8.GetString(stream.ToArray());
    }
    #endregion
    #region CleanUpResourceGroup
    public void CleanUpResourceGroup(string nameStartsWith, bool pauseBeforeDeleting = true) {
      /*var rulesToClean = new Dictionary<string, Rule>();
      foreach (var rule in this.Rules) { if (rule.Value.Name.StartsWith(nameStartsWith)) rulesToClean.Add(rule.Key, rule.Value); }
      var timersToClean = new Dictionary<string, Timer>();
      foreach (var timer in this.Timers) { if (timer.Value.Name.StartsWith(nameStartsWith)) timersToClean.Add(timer.Key, timer.Value); }
      var alarmsToClean = new Dictionary<string, Alarm>();
      foreach (var alarm in this.Alarms) { if (alarm.Value.Name.StartsWith(nameStartsWith)) alarmsToClean.Add(alarm.Key, alarm.Value); }
      var sensorsToClean = new Dictionary<string, Sensor>();
      foreach (var sensor in this.Sensors) { if (sensor.Value.Name.StartsWith(nameStartsWith)) sensorsToClean.Add(sensor.Key, sensor.Value); }
      var scenesToClean = new Dictionary<string, Scene>();
      foreach (var scene in this.Scenes) { if (scene.Value.Name.StartsWith(nameStartsWith)) scenesToClean.Add(scene.Key, scene.Value); }*/

      var itemsToClean = new Dictionary<hueApi, Dictionary<string, string>>();
      itemsToClean.Add(hueApi.rules, this.Rules.Where(i => i.Value.Name.StartsWith(nameStartsWith)).ToDictionary(i => i.Key, i => i.Value.Name));
      var timers = this.Timers.Where(i => i.Value.Name.StartsWith(nameStartsWith)).ToDictionary(i => i.Key, i => i.Value.Name);
      itemsToClean.Add(hueApi.schedules, timers);
      var alarms = this.Alarms.Where(i => i.Value.Name.StartsWith(nameStartsWith)).ToDictionary(i => i.Key, i => i.Value.Name);
      itemsToClean[hueApi.schedules].Concat(alarms);
      itemsToClean.Add(hueApi.sensors, this.Sensors.Where(i => i.Value.Name.StartsWith(nameStartsWith)).ToDictionary(i => i.Key, i => i.Value.Name));
      itemsToClean.Add(hueApi.scenes, this.Scenes.Where(i => i.Value.Name.StartsWith(nameStartsWith)).ToDictionary(i => i.Key, i => i.Value.Name));
      foreach (var api in itemsToClean) {
        Console.WriteLine("### This will be deleted from /" + api.Key.ToString() + "/:");
        foreach (var itemName in api.Value.Values) Console.WriteLine(itemName);
      }
      if (pauseBeforeAllDeletes) {
        Console.WriteLine("--- Press enter to delete ---");
        Console.ReadLine();
      }
      foreach (var api in itemsToClean) {
        this.DeleteFromBridge(api.Key, api.Value.Keys.ToArray());
        if (api.Key == hueApi.schedules) {
          foreach (var item in alarms) this.Alarms.Remove(item.Key);
          foreach (var item in timers) this.Alarms.Remove(item.Key);
        }
        else {
          foreach (var item in api.Value) {
            if (api.Key == hueApi.rules) this.Rules.Remove(item.Key);
            else if (api.Key == hueApi.sensors) this.Sensors.Remove(item.Key);
            else if (api.Key == hueApi.scenes) this.Scenes.Remove(item.Key);
          }
        }
      }
      if (nameStartsWith.StartsWith("Bryter")) {
        CleanUpResourceGroup(nameStartsWith.Replace("Bryter", "Br"), pauseBeforeEachDelete, pauseBeforeAllDeletes);
      }
    }
    #endregion
    #region Backups
    #region BackupSubApi
    public void BackupSubApiAsJosn(string SaveToFolder, string apiAdress) {
      string filename = apiAdress.Replace("/", "") + ".json";
      Directory.CreateDirectory(SaveToFolder);
      string json = this.GetJsonFromBridge(apiAdress);
      json = this.Prettify(json);
      File.WriteAllText(SaveToFolder + filename, json);
      Console.WriteLine("Saved " + apiAdress + " to " + filename);
    }
    public enum hueApi {
      lights,
      groups,
      schedules,
      sensors,
      rules,
      config,
      scenes
    }
    public void BackupSubApiAsCsv(string SaveToFolder, hueApi api, string fileName = null, params string[] sceneNames) {
      Console.WriteLine("Saving current config in api /" + api.ToString() + "/ to CSV-files(s) in " + SaveToFolder);
      if (api == hueApi.scenes) this.BackupScenesToCsv(SaveToFolder, fileName, sceneNames);
      else FileData.CreateCsvFiles(this.GetJsonFromBridge("/" + api.ToString() + "/"), fileName ?? api.ToString(), SaveToFolder, this._colNameID, this._dictProperties, this._complexJsonProps);
    }
    #endregion
    #region BackupSceneDefinitions
    public void BackupSceneDefinitions(string saveToFolder, string fileName = null, params string[] sceneNames) {
      var list = SceneDefinitionList.CreateFromSceneList(sceneNames == null ? this.Scenes.Values : this.Scenes.Values.Where(i => sceneNames.Contains(i.Name)));
      Console.WriteLine("### Calculating scenedefinitions!");
      var json = JsonSerializer.Serialize(list, new JsonSerializerOptions() { WriteIndented = true });
      Console.WriteLine("### Saving as CSV");
      FileData.CreateCsvFiles(json, fileName ?? "Scenedefs", saveToFolder, this._colNameID, this._dictProperties, this._complexJsonProps);
    }
    #endregion
    #region BackupScenesAsJson
    public void BackupScenesAsJson(string SaveToFolder, bool AsPureJson) {
      int i = 0;
      foreach (var sceneFromList in Scenes.Values) {
        string SceneFileName;
        if (sceneFromList.SceneType == "GroupScene") SceneFileName = "GroupScene - " + sceneFromList.GroupName + " - " + sceneFromList.Name + ".txt";
        else SceneFileName = "LightScene - " + sceneFromList.Name + ".txt";
        SceneFileName = SceneFileName.Replace('/', '_');
        Scene scene;
        string SceneJson;
        if (AsPureJson) {
          scene = sceneFromList;
          SceneJson = this.Prettify(scene.GetDetailsJson());
        }
        else {
          scene = sceneFromList.GetDetails();
          SceneJson = JsonSerializer.Serialize(scene, typeof(Scene), new JsonSerializerOptions() { WriteIndented = true });
        }
        Directory.CreateDirectory(SaveToFolder + scene.OwnerFriendlyAppName);
        File.WriteAllText(SaveToFolder + scene.OwnerFriendlyAppName + "\\" + SceneFileName, SceneJson);
        Console.WriteLine(i++ + ": Saved " + SceneFileName);
      }
    }
    #endregion
    #region BackupScenesToCsv
    private void BackupScenesToCsv(string saveToFolder, string fileName = null, params string[] sceneNames) {
      // FileData.CreateCsvFiles(hueBridge.GetJsonFromBridge("/scenes/"), "scenes", @"C:\Temp\HueBackups\");
      string sceneJson = "{" + Environment.NewLine;
      int i = 0;
      foreach (var scene in this.Scenes.Values) {
        if (!(sceneNames?.Contains(scene.Name) ?? true)) continue;
        sceneJson += "\"" + scene.ID + "\": ";
        sceneJson += scene.GetDetailsJson().Trim();
        sceneJson += ",";
        Console.WriteLine("Done getting json for: #" + i + " " + scene.Name);
        i++;
      }
      sceneJson = sceneJson.TrimEnd(',') + Environment.NewLine + "}";
      FileData.CreateCsvFiles(sceneJson, fileName ?? "scenes", saveToFolder, this._colNameID, this._dictProperties, this._complexJsonProps);
    }
    #endregion
    #endregion
    #region ReadFromCsv
    #region GetScenesFromCsv
    public Dictionary<string, Scene> GetScenesFromCsv(string dataFolder) {
      Console.WriteLine("Reading scenes from " + dataFolder + "Scenes.csv (+ScenesLightstates.csv)");
      string sceneJson = CsvHelper.GetJsonFromCsvFile(dataFolder + "Scenes.csv", this._colNameID, this._complexJsonProps, this.PrettyPrintIntProps, false);
      var Scenes = JsonSerializer.Deserialize<Dictionary<string, Scene>>(sceneJson);
      var lightstateJson = CsvHelper.GetJsonFromCsvFile(dataFolder + "Scene_Lightstates.csv", this._colNameID, this._complexJsonProps, this.PrettyPrintIntProps, false);
      var lightStates = JsonSerializer.Deserialize<Dictionary<string, LightState>>(lightstateJson);
      // var ruleActionJson = CsvHelper.GetJsonFromCsv(dataFolder + "RuleActions.csv");
      // var RuleActions = JsonSerializer.Deserialize<Dictionary<string, RuleActionBase>>(ruleActionJson);
      foreach (var state in lightStates) {
        string sceneID = state.Key.Split('_')[0];
        if (Scenes[sceneID].Lights == null) Scenes[sceneID].Lights = new List<LightState>();
        Scenes[sceneID].Lights.Add(state.Value);
      }
      return Scenes;
    }
    #endregion
    #region GetRulesFromCsv
    public Dictionary<string, Rule> GetRulesFromCsv(string dataFolder) {
      Console.WriteLine("Reading rules from " + dataFolder + "Rules.csv (+RuleConditions.csv and RuleActions.csv)");
      var Rules = JsonSerializer.Deserialize<Dictionary<string, Rule>>(CsvHelper.GetJsonFromCsvFile(dataFolder + "Rules.csv", this._colNameID, this._complexJsonProps, this.PrettyPrintIntProps, false));
      var RuleConditions = JsonSerializer.Deserialize<Dictionary<string, RuleCondition>>(CsvHelper.GetJsonFromCsvFile(dataFolder + "Rule_Conditions.csv", this._colNameID, this._complexJsonProps, this.PrettyPrintIntProps, false));
      var ruleActionJson = CsvHelper.GetJsonFromCsvFile(dataFolder + "Rule_Actions.csv", this._colNameID, this._complexJsonProps, this.PrettyPrintIntProps, false);
      var RuleActions = JsonSerializer.Deserialize<Dictionary<string, RuleActionBase>>(ruleActionJson);
      foreach (var cond in RuleConditions) {
        string ruleID = cond.Key.Split('_')[0];
        if (Rules[ruleID].Conditions == null) Rules[ruleID].Conditions = new List<RuleCondition>();
        Rules[ruleID].Conditions.Add(cond.Value);
      }
      foreach (var action in RuleActions) {
        string ruleID = action.Key.Split('_')[0];
        if (Rules[ruleID].Actions == null) Rules[ruleID].Actions = new List<RuleActionBase>();
        Rules[ruleID].Actions.Add(action.Value);
      }
      return Rules;
    }
    #endregion
    #region GetSensorsFromCsv
    public Dictionary<string, Sensor> GetSensorsFromCsv(string dataFolder) {
      Console.WriteLine("Reading sensors from " + dataFolder + "Sensors.csv");
      string json = CsvHelper.GetJsonFromCsvFile(dataFolder + "Sensors.csv", this._colNameID, this._complexJsonProps, this.PrettyPrintIntProps, false);
      var Sensors = JsonSerializer.Deserialize<Dictionary<string, Sensor>>(json);
      return Sensors;
    }
    #endregion
    #region GetSchedulesFromCsv
    public Dictionary<string, Schedule> GetSchedulesFromCsv(string dataFolder) {
      Console.WriteLine("Reading schedules from " + dataFolder + "Schedules.csv");
      var schedules = JsonSerializer.Deserialize<Dictionary<string, Schedule>>(CsvHelper.GetJsonFromCsvFile(dataFolder + "Schedules.csv", this._colNameID, this._complexJsonProps, this.PrettyPrintIntProps, false));
      return schedules;
    }
    #endregion
    #endregion
    #endregion
    #region ### Initialization tasks
    #region Initialize
    public void Initialize() {
      Console.WriteLine("# Getting initial data from bridge...");
      Console.Write("Getting...");
      Console.Write("config...");
      this.config = this.GetConfig();
      Console.Write("lights...");
      this.Lights = this.GetLightList();
      Console.Write("groups...");
      this.Groups = this.GetGroupList();
      this.Groups.Add("0", new LightGroup() { ID = "0", Lights = this.Lights.Values.ToList(), Name = "Alle lys (\"hemmelig\" systemgruppe)" });
      Console.Write("scenes...");
      this.Scenes = this.GetSceneList();
      Console.Write("sensors...");
      this.Sensors = this.GetSensorList();
      Console.Write("schedules...");
      this.GetSchedules();
      Console.Write("rules...");
      this.Rules = this.GetRuleList();
      this.isInitialized = true;
      Console.WriteLine(Environment.NewLine + "# Initialization done...");
      Console.WriteLine();
    }
    #endregion
    #region GetConfig
    private Config GetConfig() {
      return GetFromBridge<Config>("/config/");
    }
    #endregion
    #region GetLightList
    private Dictionary<string, Light> GetLightList() {
      var list = GetFromBridge<Dictionary<string, Light>>("/lights/");
      foreach (var item in list) {
        item.Value.ID = item.Key;
        item.Value.state.ConnectToLight(item.Value);
      }
      return list;
    }
    #endregion
    #region GetSceneList
    private SceneList GetSceneList() {
      var list = GetFromBridge<Dictionary<string, Scene>>("/scenes/");
      foreach (var item in list.Values) {
        if (item.SceneType == "GroupScene") {
          if (Groups.ContainsKey(item.GroupID)) item.GroupName = Groups[item.GroupID].Name;
          else throw new KeyNotFoundException("Scenen er knyttet til gruppen med ID " + item.GroupID + ", men denne finnes ikke i listen over grupper i Hue Bridge!");
        }
      }
      return new SceneList(list);
    }
    #endregion
    #region GetGroupList
    private Dictionary<string, LightGroup> GetGroupList() {
      var list = GetFromBridge<Dictionary<string, LightGroup>>("/groups/");
      foreach (var group in list) {
        group.Value.ID = group.Key;
      }
      return list;
    }
    #endregion
    #region GetRuleList
    private Dictionary<string, Rule> GetRuleList() {
      var list = GetFromBridge<Dictionary<string, Rule>>("/rules/");
      foreach (var rule in list) {
        rule.Value.ID = rule.Key;
      }
      return list;
    }
    #endregion
    #region GetSensorList
    private Dictionary<string, Sensor> GetSensorList() {
      var list = GetFromBridge<Dictionary<string, Sensor>>("/sensors/");
      foreach (var sensor in list) {
        sensor.Value.ID = sensor.Key;
      }
      return list;
    }
    #endregion
    #region GetSchedules
    private void GetSchedules() {
      this.Timers = new Dictionary<string, Timer>();
      this.Alarms = new Dictionary<string, Alarm>();
      var list = GetFromBridge<Dictionary<string, ConvertedSchedule>>("/schedules/");
      foreach (var schedule in list) {
        schedule.Value.ID = schedule.Key;
        if (schedule.Value is Timer) this.Timers.Add(schedule.Key, (Timer)schedule.Value);
        if (schedule.Value is Alarm) this.Alarms.Add(schedule.Key, (Alarm)schedule.Value);
      }
    }
    #endregion
    #endregion
    #region ### Basic methods for communicating with bridge ###
    #region AddToBridge
    /// <summary>Does a post to the bridge</summary>
    /// <param name="ResourceAdress">Adress of resource, without the RESTful root</param>
    /// <param name="Json">The JSON to post</param>
    /// <returns>The response from the bridge</returns>
    public string AddToBridge<T>(string resourceAddress, T data, bool printJson, bool printResponse, bool pauseAfterPrintingJson) {
      PrintJson(data, ActionMethod.POST, resourceAddress, pauseAfterPrintingJson, printJson);
      var Json = JsonContent.Create(data);
      var response = AddToBridgeAsync(resourceAddress, Json);
      return HandleResponse(response.Result, ActionMethod.POST, resourceAddress, printResponse, true);
      // var responseMessage = response.Result.Content.ReadAsStringAsync().Result;
      // return responseMessage;
    }
    private async Task<HttpResponseMessage> AddToBridgeAsync(string ResourceAdress, JsonContent Json) {
      string uriToWrite = RESTfulRoot + ResourceAdress;
      return await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, uriToWrite) { Content = Json });
    }
    #endregion
    #region UpdateBridge
    /// <summary>Does a PUT to the bridge</summary>
    /// <param name="resourceAdress">Adress of resource, without the RESTful root</param>
    /// <param name="Json">The JSON to PUT</param>
    /// <returns>The response from the bridge</returns>
    public string UpdateBridge<T>(string resourceAdress, T data, bool printJson, bool printResponse, bool pauseAfterPrintingJson) {
      this.PrintJson(data, ActionMethod.PUT, resourceAdress, pauseAfterPrintingJson, printJson);
      var Json = JsonContent.Create(data);
      var response = this.UpdateBridgeAsync(resourceAdress, Json);
      return this.HandleResponse(response.Result, ActionMethod.PUT, resourceAdress, printResponse, false);
    }
    private async Task<HttpResponseMessage> UpdateBridgeAsync(string ResourceAdress, JsonContent Json) {
      string uriToWrite = RESTfulRoot + ResourceAdress;
      return await client.SendAsync(new HttpRequestMessage(HttpMethod.Put, uriToWrite) { Content = Json });
    }
    #endregion
    #region GetFromBridge
    /// <summary>Does a GET from the bridge</summary>
    /// <typeparam name="T">The type you expect to get back</typeparam>
    /// <param name="ResourceAdress">Adress of resource, without the RESTful root</param>
    /// <returns>The response in the form of the expected object</returns>
    public T GetFromBridge<T>(string ResourceAdress) {
      // var result = GetFromBridgeAsync<T>(ResourceAdress);
      var json = GetJsonFromBridge(ResourceAdress);
      System.IO.File.AppendAllText(this._logDir.FullName + "\\GET" + ResourceAdress.Replace('/', '_').TrimEnd('_') + ".log", this.Prettify(json));
      var result = JsonSerializer.Deserialize<T>(json);
      return result;
    }
    private async Task<T> GetFromBridgeAsync<T>(string ResourceAdress) {
      string uriToRead = RESTfulRoot + ResourceAdress;
      var result = await client.GetFromJsonAsync<T>(uriToRead);
      return result;
    }
    public string GetJsonFromBridge(string ResourceAdress) {
      var result = this.GetJsonFromBridgeInternal(ResourceAdress);
      return result.Result;
    }
    private async Task<string> GetJsonFromBridgeInternal(string ResourceAdress) {
      string uriToRead = RESTfulRoot + ResourceAdress;
      var result = await client.GetStringAsync(uriToRead);
      return result;
    }
    #endregion
    #region DeleteFromBridgeAsync
    private void DeleteFromBridge(hueApi api, params int[] IDs) {
      this.DeleteFromBridge(api, IDs.Select(i => i.ToString()).ToArray());
    }
    private void DeleteFromBridge(hueApi api, params string[] IDs) {
      if (this.isInitialized) throw new Exception("Bridge is already initialized! Resource deletion must happen BEFORE initialization, or we will keep references in the local object that will give us all kinds of trouble!");
      string resourceAddress = "/" + api.ToString() + "/";
      foreach (var id in IDs) {
        var response = this.DeleteFromBridgeAsync(resourceAddress, id);
        this.HandleResponse(response.Result, ActionMethod.DELETE, resourceAddress, false, false, logAndReturn: true);
      }
    }
    private async Task<HttpResponseMessage> DeleteFromBridgeAsync(string resourceAddress, string resourceID) {
      string uriToWrite = RESTfulRoot + resourceAddress + resourceID;
      return await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, uriToWrite));
    }
    #endregion
    #region PrintJson
    private void PrintJson<T>(T data, ActionMethod method, string resouceAddress, bool pauseAfterPrintingJson, bool PrintToScreen) {
      var JsonThatWillBeSent = JsonSerializer.Serialize(data, new JsonSerializerOptions() { WriteIndented = true });
      JsonThatWillBeSent = "### Sending (" + method.ToString() + ") the following Json (to " + resouceAddress + "): ###" + Environment.NewLine + JsonThatWillBeSent;
      System.IO.File.AppendAllText(this._logDir.FullName + "\\" + method.ToString() + resouceAddress.Replace('/', '_').TrimEnd('_') + ".log", JsonThatWillBeSent);
      if (PrintToScreen) Console.WriteLine(JsonThatWillBeSent);
      if (pauseAfterPrintingJson) {
        Console.Write("Press any key to contiune...");
        Console.ReadLine();
        Console.Write(" continuing");
        Console.WriteLine();
      }
    }
    #endregion
    #region HandleResponse
    private string HandleResponse(HttpResponseMessage response, ActionMethod method, string resourceAddress, bool printResponse, bool returnID = false, bool logAndReturn = false) {
      var responseJson = response.Content.ReadAsStringAsync().Result;
      string loggedResponse = "### Reply from hueBridge: ###" + Environment.NewLine + this.Prettify(responseJson);
      System.IO.File.AppendAllText(this._logDir.FullName + "\\" + method.ToString() + resourceAddress.Replace('/', '_').TrimEnd('_') + ".log", loggedResponse);
      if (logAndReturn) return "";
      string Response = "";
      string ID = "";
      var responseObject = JsonSerializer.Deserialize<List<BridgeResponse>>(responseJson);
      if (responseObject.Any(i => i.error != null)) {
        Response = "### ERROR ###" + Environment.NewLine;
        foreach (var error in responseObject.Where(i => i.error != null).Select(i => i.error)) {
          Response += "Type " + error.type + ": " + error.description + " (at " + error.address + ")" + Environment.NewLine;
        }
        if (responseObject.Any(i => i.error == null)) {
          Response = "### SUCCESS ###" + Environment.NewLine;
          Response += string.Join(null, responseObject.Where(i => i.error == null).Select(i => i.success.Keys.First() + ": " + i.success.Values.First() + Environment.NewLine));
        }
      }
      else {
        ID = responseObject.First().success.Values.First();
        Response = "### SUCCESS ###" + Environment.NewLine;
        Response += string.Join(null, responseObject.Select(i => i.success.Keys.First() + ": " + i.success.Values.First() + Environment.NewLine));
      }
      // Always pause at errors!
      if (Response.Contains("### ERROR ###")) {
        Console.WriteLine(Response);
        Console.ReadLine();
      }
      else if (printResponse) Console.WriteLine(Response);
      return returnID ? ID : Response;
    }
    #endregion
    #endregion
  }
}
