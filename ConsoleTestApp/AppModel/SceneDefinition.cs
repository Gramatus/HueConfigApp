using ConsoleTestApp.ApiObjects.Groups;
using ConsoleTestApp.ApiObjects.Lights;
using ConsoleTestApp.ApiObjects.Lights.State;
using ConsoleTestApp.ApiObjects.Scenes;
using ConsoleTestApp.JsonConverters;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;

namespace ConsoleTestApp.AppModel {
  class SceneDefinition {
    #region ### Instance properties
    [JsonIgnore]
    public string ID { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; }
    public double[]? xy { get; set; }
    [JsonConverter(typeof(PrettyNullabeIntJsonConverter))]
    public int? HueColor { get; set; }
    [JsonConverter(typeof(PrettyNullabeIntJsonConverter))]
    public int? Saturation { get; set; }
    [JsonConverter(typeof(PrettyNullabeIntJsonConverter))]
    public int? ColorTemperatureColor { get; set; }
    [JsonConverter(typeof(PrettyIntJsonConverter))]
    public int ColorTemperatureAmbience { get; set; }
    [JsonConverter(typeof(PrettyIntJsonConverter))]
    public int BrightnessColor { get; set; }
    [JsonConverter(typeof(PrettyIntJsonConverter))]
    public int BrightnessAmbience { get; set; }
    [JsonConverter(typeof(PrettyIntJsonConverter))]
    public int BrightnessDimmerOnly { get; set; }
    [JsonConverter(typeof(PrettyIntJsonConverter))]
    public int? TransitionTime { get; set; }
    [JsonPropertyName("lightstates")]
    [JsonConverter(typeof(LightStateListJsonConverter))]
    public List<LightState> SpecialLightStates { get; set; }
    [JsonPropertyName("WantedGroup")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Group { get; set; }
    public string GroupID { get => this.Group?.ToString(); }
    [JsonPropertyName("lights")]
    public string[] LightIDs {
      get { return _lights.Select(i => i.ID).ToArray(); }
      set {
        this._lights = new List<Light>();
        foreach (string id in value) {
          if (!Program.hueBridge.Lights.ContainsKey(id)) {
            Console.WriteLine("### Warning: Scenedefintion includes light #" + id + ", which is no longer present in the bridge!", Color.Yellow);
            continue;
          }
          this._lights.Add(Program.hueBridge.Lights[id]);
        }
      }
    }
    private List<Light> _lights;
    [JsonPropertyName("Rekkefolge")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Order { get; set; }
    [JsonPropertyName("GroupList")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int[] GroupList { get; set; }
    [JsonPropertyName("Tving")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IgnoreIsOff { get; set; }
    [JsonPropertyName("Fadegruppe")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string FadeGroup { get; set; }
    [JsonPropertyName("StartJson")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? InitialStartTime { get; set; }
    [JsonPropertyName("OldName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string OldSceneName { get; set; }
    [JsonPropertyName("OldID")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string OldSceneID { get; set; }
    #endregion
    #region ### Constructors
    public SceneDefinition() {
      this.SpecialLightStates = new List<LightState>();
    }
    public SceneDefinition(string id, string name, double[]? xy, int? hueColor, int? saturation, int? colorTemperatureColor, int? colorTemperatureAmbience, int? brightnessColor, int? brightnessAmbience, int? brightnessDimmerOnly, int? transitionTime, int fallbackCT, int fallbackBrightness) : this() {
      this.ID = id;
      this.Name = name;
      this.xy = xy;
      this.HueColor = hueColor;
      this.Saturation = saturation;
      this.ColorTemperatureColor = colorTemperatureColor;
      this.ColorTemperatureAmbience = colorTemperatureAmbience ?? fallbackCT;
      this.BrightnessColor = brightnessColor ?? fallbackBrightness;
      this.BrightnessAmbience = brightnessAmbience ?? fallbackBrightness;
      this.BrightnessDimmerOnly = brightnessDimmerOnly ?? fallbackBrightness;
      this.TransitionTime = transitionTime;
    }
    public SceneDefinition(string id, string name, double[]? xy, int? hueColor, int? saturation, int? colorTemperatureColor, int colorTemperatureAmbience, int brightnessColor, int brightnessAmbience, int brightnessDimmerOnly, int transitionTime) : this() {
      this.ID = id;
      this.Name = name;
      this.xy = xy;
      this.HueColor = hueColor;
      this.Saturation = saturation;
      this.ColorTemperatureColor = colorTemperatureColor;
      this.ColorTemperatureAmbience = colorTemperatureAmbience;
      this.BrightnessColor = brightnessColor;
      this.BrightnessAmbience = brightnessAmbience;
      this.BrightnessDimmerOnly = brightnessDimmerOnly;
      this.TransitionTime = transitionTime;
    }
    #endregion
    #region ### Instance methods
    #region GetExisitingScene
    private static Scene GetScene(SceneDefinition def, bool printInfo, bool pauseBeforeUpdating, string altGroupID = null) {
      // This call is for creating a scene for another group than the one the scene definition really belongs to. The scenedefinition has no knowlegde of scene ID, but the name and the group should help us get the right one.
      if (altGroupID != null) {
        var scene = Program.hueBridge.Scenes.Values.FirstOrDefault(i => i.GroupID == altGroupID && i.Name == (def.OldSceneName ?? def.Name)) ?? Program.hueBridge.Scenes.Values.FirstOrDefault(i => i.GroupID == altGroupID && i.Name == def.Name);
        if (scene == null) {
          Console.WriteLine("No scene with name \"" + def.OldSceneName + "\" or \"" + def.Name + "\" found in the bridge for group " + altGroupID + ", creating a new scene.");
          scene = Scene.Create(def.Name, altGroupID, printInfo, pauseBeforeUpdating, SceneAppData.GetForGroup(altGroupID));
          return scene;
        }
        else {
          Console.WriteLine("Scene with name \"" + scene.Name + "\" already exists in the bridge for group " + altGroupID + " (ID: " + scene.ID + "), updating with new values.");
          // scene.CustomAppData = SceneAppData.GetForGroup(altGroupID);
          return scene;
        }
      }
      // If an ID is stored, the scene should exist - return an error if it is not found! (exception: if we have an "old scene ID" - then we are in a transition state and should treat this a  bit differently)
      if (def.ID != null && Program.hueBridge.Scenes.ContainsKey(def.ID)) {
        Console.WriteLine("Scene with ID: " + def.ID + " already exists in the bridge(name: " + Program.hueBridge.Scenes[def.ID].Name + "), updating with new values.");
      }
      else if (def.OldSceneID == null && def.ID != null && !Program.hueBridge.Scenes.ContainsKey(def.ID)) {
        throw new ArgumentException("Scene with ID: " + def.ID + " not found!");
      }
      // If no ID is stored, check if something with the correct name exists and use if it does
      else if (Program.hueBridge.Scenes.Any(i => i.Value.Name == (def.OldSceneName ?? def.Name)) || Program.hueBridge.Scenes.Values.Any(i => i.Name == def.Name)) {
        var scene = Program.hueBridge.Scenes.Values.FirstOrDefault(i => i.Name == def.OldSceneName) ?? Program.hueBridge.Scenes.Values.First(i => i.Name == def.Name);
        // def.ID = Program.hueBridge.Scenes.Values.First(i => i.Name == def.Name).ID;
        def.ID = scene.ID;
        Console.WriteLine("Scene with name \"" + scene.Name + "\" already exists in the bridge (ID: " + def.ID + "), updating with new values.");
      }
      else {
        Console.WriteLine("No scene with name \"" + def.OldSceneName + "\" or \"" + def.Name + "\" found, creating a new scene.");
        Scene newScene;
        if (def.GroupID != null) newScene = Scene.Create(def.Name, def.GroupID, printInfo, pauseBeforeUpdating);
        else newScene = Scene.Create(def.Name, def.LightIDs, true, printInfo, pauseBeforeUpdating);
        def.ID = newScene.ID;
      }
      return Program.hueBridge.Scenes[def.ID];
    }
    #endregion
    #region UpdateSceneToDefinition
    private static void SaveSceneToBridgeFromDefinition(Scene scene, SceneDefinition def, string groupID, bool printInfo, bool printBridgeInfo, bool pauseBeforeUpdating) {
      if (scene.Name != def.Name) {
        Console.WriteLine("Updating a scene with the same ID, but another name. Old name: " + scene.Name + ", new name: " + def.Name);
        Console.WriteLine("Press enter to continue with updating the scene");
        Console.ReadLine();
        scene.Name = def.Name;
      }
      scene.Lights = null; // If the scene already exists in the bridge, the lightlist will contain the lights that was there before. In case those are no longer relevant, we remove them
      if (groupID != null) {
        if (!Program.hueBridge.Groups.ContainsKey(groupID)) throw new ArgumentOutOfRangeException("No group with ID " + groupID + " found in bridge!");
        scene.SceneType = "GroupScene";
        foreach (var light in Program.hueBridge.Groups[groupID].Lights) {
          scene.AddLight(light.ID, true);
        }
      }
      else {
        scene.SceneType = "LightScene";
        foreach (var light in def.LightIDs) {
          scene.AddLight(light, true);
        }
      }

      if (printInfo) Console.WriteLine("Updating: " + scene.Name);
      if (printInfo) Console.WriteLine("Type: " + scene.SceneType + (scene.SceneType == "GroupScene" ? " (" + scene.GroupName + ")" : ""));
      if (printInfo) Console.WriteLine("Color values:      " + def.TransitionTime + " / " + def.BrightnessColor + " / " + (def.ColorTemperatureColor ?? def.ColorTemperatureAmbience) + " / " + def.HueColor + " / " + def.Saturation);
      scene.SetCommonStateColorOnly(true, def.BrightnessColor, def.HueColor, def.Saturation, (def.ColorTemperatureColor ?? def.ColorTemperatureAmbience), def.TransitionTime);
      if (printInfo) Console.WriteLine("Ambience values:   " + def.TransitionTime + " / " + def.BrightnessAmbience + " / " + def.ColorTemperatureAmbience);
      scene.SetCommonStateAmbienceOnly(true, def.BrightnessAmbience, def.ColorTemperatureAmbience, def.TransitionTime);
      if (printInfo) Console.WriteLine("Dimmer values:     " + def.TransitionTime + " / " + +def.BrightnessDimmerOnly);
      scene.SetCommonStatePureDimmersOnly(true, def.BrightnessDimmerOnly, def.TransitionTime);
      if (printInfo) Console.WriteLine("Updating special values for these lights: " + String.Join(',', def.SpecialLightStates.Select(i => i.LightID)));
      foreach (var light in def.SpecialLightStates) {
        if (scene.LightIDs.Contains(light.LightID)) {
          scene.SetSingleLightState(light);
        }
      }
      if (printInfo) Console.WriteLine("Lights in scene: " + string.Join(',', scene.LightIDs));
      if (pauseBeforeUpdating) {
        Console.Write("Press any key to update scene...");
        Console.ReadLine();
        Console.Write(" continuing");
        Console.WriteLine();
      }
      scene.Update(printBridgeInfo, pauseBeforeUpdating);
      scene.Update(false, false); // Sometimes the first update does not work (I think that is when adding lights...)
    }
    #endregion
    #region SaveSceneDefinitionToBridge
    public void SaveSceneDefinitionToBridge(bool printInfo, bool printBridgeInfo, bool pauseBeforeUpdating) {
      var scene = GetScene(this, printBridgeInfo, pauseBeforeUpdating);
      SaveSceneToBridgeFromDefinition(scene, this, this.GroupID, printInfo, printBridgeInfo, pauseBeforeUpdating);
      if (this.Order == null && this.GroupList?.Length > 0) {
        foreach (var group in this.GroupList) {
          var groupScene = GetScene(this, printBridgeInfo, pauseBeforeUpdating, altGroupID: group.ToString());
          SaveSceneToBridgeFromDefinition(groupScene, this, group.ToString(), printInfo, printBridgeInfo, pauseBeforeUpdating);
        }
      }
    }
    #endregion
    #endregion
    #region ### Static methods
    #region GetSceneDefinitionFromScene
    public static SceneDefinition GetSceneDefinitionFromScene(Scene sceneWithoutDetails) {
      var scene = sceneWithoutDetails.GetDetails();
      #region Create groups of lights based on their settings
      var groupedLights = new Dictionary<string, List<LightState>>();
      groupedLights.Add("colorHSlights", new List<LightState>());
      groupedLights.Add("colorXYlights", new List<LightState>());
      groupedLights.Add("tempLights", new List<LightState>());
      groupedLights.Add("dimLights", new List<LightState>());
      foreach (var light in scene.Lights) {
        if (light.HueColor != null) groupedLights["colorHSlights"].Add(light);
        else if (light.XY_position != null) groupedLights["colorXYlights"].Add(light);
        else if (light.ColorTemperature != null) groupedLights["tempLights"].Add(light);
        else groupedLights["dimLights"].Add(light);
      }
      #endregion
      #region Get "examples" of each kind, based on the settings that most lights have (if any group has more than others)
      var exampleLights = new Dictionary<string, LightState>();

      exampleLights.Add("colorHSlights", (groupedLights["colorHSlights"].Count > 0) ? groupedLights["colorHSlights"].GroupBy(p => p.HueColor + "_" + p.Saturation + "_" + p.Brightness + "_" + p.TransitionTime).OrderBy(g => g.Count()).First().First() : null);
      exampleLights.Add("colorXYlights", (groupedLights["colorXYlights"].Count > 0) ? groupedLights["colorXYlights"].GroupBy(p => p.XY_position[0] + "_" + p.XY_position[1] + "_" + p.Brightness + "_" + p.TransitionTime).OrderBy(g => g.Count()).First().First() : null);
      exampleLights.Add("tempLights", (groupedLights["tempLights"].Count > 0) ? groupedLights["tempLights"].GroupBy(p => p.ColorTemperature + "_" + p.Brightness + "_" + p.TransitionTime).OrderBy(g => g.Count()).First().First() : null);
      exampleLights.Add("dimLights", (groupedLights["dimLights"].Count > 0) ? groupedLights["dimLights"].GroupBy(p => p.Brightness + "_" + p.TransitionTime).OrderBy(g => g.Count()).First().First() : null);

      var colorExample = exampleLights["colorHSlights"];
      var xyExample = exampleLights["colorXYlights"];
      var tempExample = exampleLights["tempLights"];
      var dimExample = exampleLights["dimLights"];
      #endregion
      #region Get example transitiontime already here
      int? TransitionTimeDim = dimExample?.TransitionTime;
      int? TransitionTimeTemp = tempExample?.TransitionTime;
      int? TransitionTimeColor = colorExample?.TransitionTime;
      int? TransitionTimeXy = xyExample?.TransitionTime;
      int? TransitionTime = TransitionTimeColor ?? TransitionTimeTemp ?? TransitionTimeDim ?? TransitionTimeXy;
      #endregion
      #region Find all lights that are outside the scene definition
      var diffLights = new Dictionary<string, LightState>();
      foreach (var example in exampleLights) {
        if (example.Value == null) continue;
        foreach (var light in scene.Lights) {
          if (!groupedLights[example.Key].Select(i => i.LightID).Contains(light.LightID)) continue;
          #region Check Huecolors first
          if (example.Key == "colorHSlights") {
            if (
                 (light.HueColor != example.Value.HueColor && !diffLights.ContainsKey(light.LightID))
              || (light.Saturation != example.Value.Saturation && !diffLights.ContainsKey(light.LightID))
              || (light.ColorTemperature != example.Value.ColorTemperature && !diffLights.ContainsKey(light.LightID))
              || (light.Brightness != example.Value.Brightness && !diffLights.ContainsKey(light.LightID))
              || (light.TransitionTime != TransitionTime && !diffLights.ContainsKey(light.LightID))
            ) {
              light.ColorMode = ColorMode.hs;
              diffLights.Add(light.LightID, light);
            }
          }
          #endregion
          #region XY second
          else if (example.Key == "colorXYlights") {
            if (
                 (light.HueColor != example.Value.HueColor && !diffLights.ContainsKey(light.LightID))
              || (light.Saturation != example.Value.Saturation && !diffLights.ContainsKey(light.LightID))
              || (light.ColorTemperature != example.Value.ColorTemperature && !diffLights.ContainsKey(light.LightID))
              || (light.Brightness != example.Value.Brightness && !diffLights.ContainsKey(light.LightID))
              || (light.TransitionTime != TransitionTime && !diffLights.ContainsKey(light.LightID))
            ) {
              light.ColorMode = ColorMode.xy;
              diffLights.Add(light.LightID, light);
            }
          }
          #endregion
          #region Ambience third
          else if (example.Key == "tempLights") {
            if (
                 (light.ColorTemperature != example.Value.ColorTemperature && !diffLights.ContainsKey(light.LightID))
              || (light.Brightness != example.Value.Brightness && !diffLights.ContainsKey(light.LightID))
              || (light.TransitionTime != TransitionTime && !diffLights.ContainsKey(light.LightID))
            ) {
              light.ColorMode = ColorMode.ct;
              diffLights.Add(light.LightID, light);
            }

          }
          #endregion
          #region And finally pure dimmers
          else {
            if (
                 (light.Brightness != example.Value.Brightness && !diffLights.ContainsKey(light.LightID))
              || (light.TransitionTime != TransitionTime && !diffLights.ContainsKey(light.LightID))
            ) {
              diffLights.Add(light.LightID, light);
            }
          }
          #endregion
        }
      }
      #endregion
      #region Get the values to make out the definition, handling nulls
      int? BrightnessDim = dimExample?.Brightness;
      int? BrightnessTemp = tempExample?.Brightness;
      int? BrightnessColor = colorExample?.Brightness;
      int? BrightnessXy = xyExample?.Brightness;
      int BrightnessFallback = BrightnessTemp ?? BrightnessDim ?? BrightnessColor ?? BrightnessXy ?? 0;

      int? ColorTemperatureTemp = tempExample?.ColorTemperature;
      int? ColorTemperatureColor = colorExample?.ColorTemperature;
      int? ColorTemperatureXy = xyExample?.ColorTemperature;
      int ColorTemperatureFallback = ColorTemperatureTemp ?? ColorTemperatureColor ?? ColorTemperatureXy ?? 0;

      double[]? xy = xyExample?.XY_position;
      int? HueColor = colorExample?.HueColor;
      int? Saturation = colorExample?.Saturation;
      #endregion
      #region Create scene definition
      var sceneDef = new SceneDefinition(scene.ID, scene.Name, xy, HueColor, Saturation, ColorTemperatureColor, ColorTemperatureTemp, BrightnessColor, BrightnessTemp, BrightnessDim, TransitionTime, ColorTemperatureFallback, BrightnessFallback);
      sceneDef.SpecialLightStates = diffLights.Values.ToList();
      sceneDef.LightIDs = scene.LightIDs;
      #endregion
      return sceneDef;
    }
    #endregion
    #endregion
  }
}
