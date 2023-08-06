using ConsoleTestApp.ApiObjects.Groups;
using ConsoleTestApp.ApiObjects.Lights.State;
using ConsoleTestApp.Helpers;
using ConsoleTestApp.JsonConverters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConsoleTestApp.ApiObjects.Scenes {
  public class SceneAppData {
    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("data")]
    public string Data { get; set; }
    public SceneAppData() { }
    public SceneAppData(string data) {
      this.Version = 1;
      this.Data = data;
    }
    public static SceneAppData GetForGroup(string groupID) {
      string value = "(ukjent)";
      if (groupID == "2") value = "XdF1q_r02";
      if (groupID == "3") value = "vSNmy_r03";
      if (groupID == "4") value = "BVEdh_r04";
      if (groupID == "11") value = "F4Gvo_r11";
      if (groupID == "9") value = "k5Tn3_r09";
      if (groupID == "16") value = "aaiiZ_r15";
      return new SceneAppData(value);
    }

  }
  public class Scene : IApiObject {
    #region Instance properties
    [JsonIgnore]
    public string ID { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("recycle")]
    public bool CanBeAutoRecycled { get; set; }
    /// <summary>I.e. is referenced by something.</summary>
    [JsonPropertyName("locked")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsLocked { get; set; }
    [JsonPropertyName("type")]
    public string SceneType { get; set; }
    [JsonPropertyName("group")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string GroupID { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string GroupName { get; set; }
    [JsonPropertyName("owner")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Owner { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string OwnerFriendlyAppName {
      get {
        if (Program.hueBridge.ConnectedApps.ContainsKey(Owner)) return Program.hueBridge.ConnectedApps[Owner].AppName;
        else return "Unknown";
      }
    }
    [JsonPropertyName("image")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string ImageGuid { get; set; }
    [JsonPropertyName("lastupdated")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? LastUpdated { get; set; }
    [JsonPropertyName("version")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Version { get; set; }
    /// <summary>Not really needed, as the same list of lights is also in the light states, but needed in communication with the bridge.</summary>
    [JsonPropertyName("lights")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[] LightIDs {
      get { return Lights?.Select(i => i.LightID).ToArray(); }
      set { }
    }
    [JsonConverter(typeof(LightStateListJsonConverter))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("lightstates")]
    public List<LightState> Lights { get; set; }
    [JsonPropertyName("appdata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonConverter(typeof(AppDataJsonConverter))]
    public SceneAppData CustomAppData { get; set; }
    #region Not implemented properties that exists in JSON
    // [JsonPropertyName("picture")]
    // public string CustomPicture { get; set; }
    #endregion
    #endregion
    #region Instance Methods
    #region GetDetails
    /// <summary>When a list of scenes is read, the details are not part of the data returned. This reads the specific scene and gets all data.</summary>
    public Scene GetDetails() {
      var scene = Program.hueBridge.GetFromBridge<Scene>("/scenes/" + this.ID);
      scene.ID = ID;
      if (scene.SceneType == "GroupScene") {
        if (Program.hueBridge.Groups.ContainsKey(scene.GroupID)) scene.GroupName = Program.hueBridge.Groups[scene.GroupID].Name;
        else throw new KeyNotFoundException("Scenen er knyttet til gruppen med ID " + scene.GroupID + ", men denne finnes ikke i listen over grupper i Hue Bridge!");
      }
      Program.hueBridge.Scenes[this.ID] = scene;
      return scene;
    }
    public string GetDetailsJson() {
      return Program.hueBridge.GetJsonFromBridge("/scenes/" + this.ID);
    }
    #endregion
    #region Update
    public void Update(bool printInfo, bool pauseBeforeUpdating) {
      var data = new UpdatebleScene() { Name = Name, Lights = Lights, CustomAppData = CustomAppData?.Data == null ? null : CustomAppData };
      string response = Program.hueBridge.UpdateBridge("/scenes/" + ID + "/", data, printInfo, printInfo, pauseBeforeUpdating);
    }
    #endregion
    #region SetCommonState
    public void SetCommonState(bool? IsOn, int? brightness, int? hueColor, int? saturation, int? colortemperature, int? transitiontime) {
      foreach (var light in Lights) {
        if (IsOn != null) light.IsOn = IsOn;
        if (brightness != null) light.Brightness = brightness.Value;
        if (hueColor != null && light.ColorCapabilities == ColorMode.hs) light.HueColor = hueColor;
        else light.HueColor = null;
        if (saturation != null && light.ColorCapabilities == ColorMode.hs) light.Saturation = saturation;
        else light.Saturation = null;
        if (colortemperature != null && (light.ColorCapabilities == ColorMode.ct || light.ColorCapabilities == ColorMode.hs && hueColor == null)) light.ColorTemperature = colortemperature;
        else light.ColorTemperature = null;
        light.TransitionTime = transitiontime ?? 4;
      }

    }
    #endregion
    #region SetSingleLightState
    public void SetSingleLightState(LightState state) {
      var light = this.Lights.First(i => i.LightID == state.LightID);
      if (state.IsOn != null) light.IsOn = state.IsOn;
      if (state.Brightness != null) light.Brightness = state.Brightness;
      if (state.HueColor != null) light.HueColor = state.HueColor;
      if (state.Saturation != null) light.Saturation = state.Saturation;
      if (state.ColorTemperature != null && state.HueColor == null) light.ColorTemperature = state.ColorTemperature;
      if (state.XY_position != null && state.XY_position.Length == 2) {
        light.XY_position = state.XY_position;
        light.HueColor = null;
        light.Saturation = null;
      }
      if (state.TransitionTime != null) light.TransitionTime = state.TransitionTime;
    }
    #endregion
    #region SetCommonStateColorOnly
    public void SetCommonStateColorOnly(bool? IsOn, int? brightness, int? hueColor, int? saturation, int? colortemperature, int? transitiontime) {
      foreach (var light in Lights.Where(i => i.ColorCapabilities == ColorMode.hs)) {
        if (IsOn != null) light.IsOn = IsOn;
        if (brightness != null) light.Brightness = brightness.Value;
        if (hueColor != null) light.HueColor = hueColor;
        else light.HueColor = null;
        if (saturation != null) light.Saturation = saturation;
        else light.Saturation = null;
        if (colortemperature != null && hueColor == null) light.ColorTemperature = colortemperature;
        else light.ColorTemperature = null;
        light.TransitionTime = transitiontime ?? 4;
      }
    }
    #endregion
    #region SetCommonStateAmbienceOnly
    public void SetCommonStateAmbienceOnly(bool? IsOn, int? brightness, int? colortemperature, int? transitiontime) {
      foreach (var light in Lights.Where(i => i.ColorCapabilities == ColorMode.ct)) {
        if (IsOn != null) light.IsOn = IsOn;
        if (brightness != null) light.Brightness = brightness.Value;
        if (colortemperature != null) light.ColorTemperature = colortemperature;
        else light.ColorTemperature = null;
        light.TransitionTime = transitiontime ?? 4;
      }
    }
    #endregion
    #region SetCommonStatePureDimmersOnly
    public void SetCommonStatePureDimmersOnly(bool? IsOn, int? brightness, int? transitiontime) {
      foreach (var light in Lights.Where(i => i.ColorCapabilities == null)) {
        if (IsOn != null) light.IsOn = IsOn;
        if (brightness != null) light.Brightness = brightness.Value;
        light.TransitionTime = transitiontime ?? 4;
      }
    }
    #endregion
    #region Create
    public void Create(bool printInfo, bool pauseBeforeUpdating) {
      if (this.ID != null) throw new Exception("Scene already created!");
      string id = Program.hueBridge.AddToBridge("/scenes/", this, printInfo, printInfo, pauseBeforeUpdating);
      // var responseObject = JsonSerializer.Deserialize<List<BridgeResponse>>(response);
      this.ID = id; // responseObject[0].success["id"];
    }
    #endregion
    #region AddLight
    public void AddLight(string id, bool isOn) {
      if (this.Lights == null) this.Lights = new List<LightState>();
      if (Lights.Any(i => i.LightID == id)) return; // Don't add a light that is already part of the scene again
      if (Program.hueBridge.Lights.ContainsKey(id)) {
        var light = new LightState();
        light.ConnectToLight(Program.hueBridge.Lights[id]);
        // light.Brightness = Brightness;
        light.IsOn = isOn;
        light.ColorMode = null;
        Lights.Add(light);
      }
      else throw new KeyNotFoundException("Attempting to add light with id " + id + " to scene, but light does not exist in bridge.");

    }
    #endregion
    #region GetDefinitionCsv
    public string GetDefinitionCsvHeaders() {
      return "ID;Name;HueColor;Saturation;ColorTemperatureColor;ColorTemperatureAmbience;BrightnessColor;BrightnessAmbience;BrightnessDimmerOnly;TransitionTime" + Environment.NewLine;
    }
    public string GetDefinitionCsv() {
      var scene = this.GetDetails();
      var exampleColor = scene.Lights.First(i => i.ColorCapabilities == ColorMode.hs);
      var exampleTemperature = scene.Lights.First(i => i.ColorCapabilities == ColorMode.ct);
      var exampleDimOnly = scene.Lights.First(i => i.ColorCapabilities == null);
      return
        scene.ID + ";"
        + scene.Name + ";"
        + (exampleColor.HueColor == null ? "null" : exampleColor.HueColor.ToString()) + ";"
        + (exampleColor.Saturation == null ? "null" : exampleColor.Saturation.ToString()) + ";"
        + (exampleColor.ColorTemperature == null ? "null" : exampleColor.ColorTemperature.ToString()) + ";"
        + (exampleTemperature.ColorTemperature == null ? "null" : exampleTemperature.ColorTemperature.ToString()) + ";"
        + exampleColor.Brightness.ToString() + ";"
        + exampleTemperature.Brightness.ToString() + ";"
        + exampleDimOnly.Brightness.ToString() + ";"
        + (exampleColor.TransitionTime == null ? "null" : exampleColor.TransitionTime.ToString())
        + Environment.NewLine;
    }
    #endregion
    #endregion
    #region Static methods
    #region CreateOnOffScenesIfMissing
    public static void CreateOnOffScenesIfMissing(string name, string[] lights, string onSuffix, string offSuffix, bool printInfo, bool pauseBeforeUpdating) {
      Scene.CreatOnOrOffSceneIfMissing(name, lights, onSuffix, true, printInfo, pauseBeforeUpdating);
      Scene.CreatOnOrOffSceneIfMissing(name, lights, offSuffix, false, printInfo, pauseBeforeUpdating);
    }
    public static void CreatOnOrOffSceneIfMissing(string name, string[] lights, string suffix, bool turnOnLights, bool printInfo, bool pauseBeforeUpdating) {
      var scene = Program.hueBridge.Scenes.Values.FirstOrDefault(i => i.Name == name + suffix);
      if (scene == null) Scene.Create(name + suffix, lights, turnOnLights, printInfo, pauseBeforeUpdating);
      else {
        if (scene.Lights == null) foreach (var light in lights) scene.AddLight(light, turnOnLights);
        else scene.SetCommonState(turnOnLights, null, null, null, null, 0);
        scene.Update(printInfo, pauseBeforeUpdating);
      }
    }
    public static void CreateOnOffScenesIfMissingForGroup(string name, string groupID, bool printInfo, bool pauseBeforeUpdating) {
      if (!Program.hueBridge.Scenes.Values.Any(i => i.Name == name + " på")) {
        var onScene = Scene.Create(name + " på", groupID, printInfo, pauseBeforeUpdating);
        onScene.SetCommonState(true, null, null, null, null, 0);
        onScene.Update(printInfo, pauseBeforeUpdating);
      }
      if (!Program.hueBridge.Scenes.Values.Any(i => i.Name == name + " av")) {
        var offScene = Scene.Create(name + " av", groupID, printInfo, pauseBeforeUpdating);
        offScene.SetCommonState(false, null, null, null, null, 0);
        offScene.Update(printInfo, pauseBeforeUpdating);
      }
    }
    #endregion
    #region Create
    public static Scene Create(string name, string groupID, bool printInfo, bool pauseBeforeUpdating, SceneAppData appData = null) {
      if (!Program.hueBridge.Groups.ContainsKey(groupID)) throw new ArgumentOutOfRangeException("No group with ID " + groupID + " found in bridge!");
      return Scene.Create(name, Program.hueBridge.Groups[groupID], printInfo, pauseBeforeUpdating, appData: appData);
    }
    public static Scene Create(string name, LightGroup group, bool printInfo, bool pauseBeforeUpdating, SceneAppData appData = null) {
      var scene = new Scene();
      scene.Name = name;
      scene.CanBeAutoRecycled = false;
      scene.SceneType = "GroupScene";
      scene.GroupID = group.ID;
      scene.GroupName = group.Name;
      scene.CustomAppData = appData;
      scene.Create(printInfo, pauseBeforeUpdating);
      Program.hueBridge.Scenes.Add(scene.ID, scene);
      foreach (var light in group.Lights) {
        scene.AddLight(light.ID, true);
      }
      return scene;
    }
    public static Scene Create(string name, string[] lights, bool isOn, bool printInfo, bool pauseBeforeUpdating, SceneAppData appData = null) {
      var scene = new Scene();
      scene.Name = name;
      scene.CanBeAutoRecycled = false;
      scene.SceneType = "LightScene";
      scene.Lights = new List<LightState>();
      scene.CustomAppData = appData;
      foreach (string id in lights) {
        scene.AddLight(id, isOn);
      }
      scene.Create(printInfo, pauseBeforeUpdating);
      Program.hueBridge.Scenes.Add(scene.ID, scene);
      return scene;
    }
    #endregion
    #endregion
  }
}
/*
{
"name": "awesomescene",
 "lights": ["1", "2"],
 "lightstates": {
     "1": {
         "on": false,
         "bri": 100,
         "xy": [0.3, 0.2],
     },
     "2": {
         "on": false,
         "bri": 100,
          "xy": [0.3, 0.2],
          "effect": "colorloop",
     }
 }
}
 */
