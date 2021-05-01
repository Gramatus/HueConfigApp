using ConsoleTestApp.ApiObjects.Groups;
using ConsoleTestApp.ApiObjects.Lights.State;
using ConsoleTestApp.ApiObjects.Scenes;
using ConsoleTestApp.Helpers.Csv;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace ConsoleTestApp.AppModel {
  class SceneDefinitionList : Dictionary<string, SceneDefinition> {
    #region Instance methods
    #region Add
    public void Add(SceneDefinition def) {
      if (def.ID == null) throw new ArgumentException("Add can only be called with a single parameter if the scenedefinition stores the ID!");
      this.Add(def.ID, def);
    }
    #endregion
    #region UpdateSceneSet
    public void UpdateSceneSet(LightGroup group, bool printInfo, bool printBridgeInfo, bool pauseAfterPrintingJson) {
      foreach (var sceneDefinition in this.Values) {
        sceneDefinition.SaveSceneDefinitionToBridge(printInfo, printBridgeInfo, pauseAfterPrintingJson);
      }
    }
    /// <summary>Only use this if all scenes in the list are groupscenes! (OR NOT???)</summary>
    /// <param name="printInfo"></param>
    /// <param name="pauseAfterPrintingJson"></param>
    public void UpdateSceneSet(bool printInfo, bool printBridgeInfo, bool pauseAfterPrintingJson) {
      foreach (var sceneDefinition in this.Values) {
        if (sceneDefinition.Order != null) continue; // Scenedefinition is related to a transition rule and should only be added when working with transitionrules!
        // var group = Program.hueBridge.Groups.First(i => i.Value.ID == sceneDefinition.GroupID);
        // Not sure about the logic in the first step, but it shouldn't stop anything (at least yet)
        // if (sceneDefinition == null) throw new ArgumentOutOfRangeException("This method can only be used if all scenes in the list are group scenes!");
        // else if (group.Value == null) throw new ArgumentOutOfRangeException("No group with ID " + sceneDefinition.GroupID + " found in bridge!");
        sceneDefinition.SaveSceneDefinitionToBridge(printInfo, printBridgeInfo, pauseAfterPrintingJson);
      }
    }
    #endregion
    #region GetByName
    public SceneDefinition GetByName(string name) {
      return this.First(i => i.Value.Name == name).Value;
    }
    #endregion
    #endregion
    #region Static methods
    #region CreateFromSceneList
    public static SceneDefinitionList CreateFromSceneList(IEnumerable<Scene> scenes) {
      var defList = new SceneDefinitionList();
      var sceneList = scenes.ToList();
      for (int i = 0; i < sceneList.Count; i++) {
        Console.WriteLine("Computing scenedefinition #" + (i + 1) + ": " + sceneList[i].Name + (sceneList[i].GroupName == null ? "" : " (" + sceneList[i].GroupName + ")"));
        defList.Add(SceneDefinition.GetSceneDefinitionFromScene(sceneList[i]));
      }
      return defList;
    }
    #endregion
    #region GetFromDataFiles
    public static SceneDefinitionList GetFromDataFiles(string dataDirectory, string sceneFile, string lightFile, string colNameID, string[] prettyPrintIntProps, string[] complexJsonProps) {
      Console.WriteLine("Reading scenedefinitons from " + dataDirectory + " (files: " + sceneFile + " and " + lightFile + ")");
      var list = new SceneDefinitionList();
      string json = CsvHelper.GetJsonFromCsvFile(dataDirectory + sceneFile, colNameID, complexJsonProps, prettyPrintIntProps, false);
      var sceneDefs = JsonSerializer.Deserialize<Dictionary<string, SceneDefinition>>(CsvHelper.GetJsonFromCsvFile(dataDirectory + sceneFile, colNameID, complexJsonProps, prettyPrintIntProps, false));
      Dictionary<string, LightState> lightDefs = null;
      if (lightFile != null) {
        var lightDefsJson = CsvHelper.GetJsonFromCsvFile(dataDirectory + lightFile, colNameID, complexJsonProps, prettyPrintIntProps, false);
        lightDefs = JsonSerializer.Deserialize<Dictionary<string, LightState>>(lightDefsJson);
      }
      foreach (var def in sceneDefs) {
        // We are temporarily using Guids for IDs if none was supplied in the CSV, but that should not be stored from here on
        if (Guid.TryParse(def.Key, out Guid guid)) def.Value.ID = null;
        else def.Value.ID = def.Key;
        list.Add(def.Key, def.Value);
      }
      if (lightDefs != null) {
        foreach (var state in lightDefs) {
          string sceneID = state.Key.Split('_')[0];
          if (list.ContainsKey(sceneID)) {
            // Check for the actual light
            try {
              // Third element of e.g. "QfLmA2GQPvgMQYS_FadeKveldslys 3t15min_15"
              string lightID = state.Key.Split('_')[2];
              state.Value.ConnectToLight(Program.hueBridge.Lights[lightID]);
              list[sceneID].SpecialLightStates.Add(state.Value);
            }
            catch (Exception ex) {
              Console.WriteLine("Scene with ID " + sceneID + " should have a special lightstate that could not be connected to a light, the exception was: " + ex.Message);
              // Skip this lightstate
            }
          }
        }
      }
      return list;
    }
    #endregion
    #endregion
    #region DEPRECATED
    #region SerializeJson (old method)
    public string SerializeJson_deprecated() {
      // string tmp1 = JsonSerializer.Serialize(this);
      string Json = "[" + Environment.NewLine;
      foreach (var sceneDefinition in this) {
        Json += JsonSerializer.Serialize(sceneDefinition) + "," + Environment.NewLine;
      }
      Json = Json.TrimEnd(("," + Environment.NewLine).ToCharArray()) + Environment.NewLine + "]";
      Json = Json.Replace("null", "  " + Program.PadString("null"));
      // var tmp3 = JsonSerializer.Deserialize<HardcodedSceneDefinitionList>(tmp2);
      return Json;
    }
    #endregion
    #region DeserializeJson (old method)
    public static SceneDefinitionList DeserializeJson_deprecated(string Json) {
      return JsonSerializer.Deserialize<SceneDefinitionList>(Json);
    }
    #endregion
    #region SerializeCsv (old method)
    public string SerializeCsv_deprecated() {
      // var json = this.SerializeJson();
      return null; // CsvHelper.JsonToCsv(this.SerializeJson());
    }
    #endregion
    #region DeserializeCsv (old method)
    public static SceneDefinitionList DeserializeCsv_deprecated(string csv) {
      return null; //DeserializeJson(CsvHelper.CsvToJson(csv));
    }
    #endregion
    #endregion
  }
}