using ConsoleTestApp.ApiObjects.Scenes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace ConsoleTestApp.ApiObjects.Groups {
  public class GroupRecallSceneData : IApiObject {
    [JsonIgnore]
    public string ID { get => this.SceneToRecall.ID; }
    [JsonIgnore]
    public string Name { get => "Performing a scene recall of scene " + this.SceneToRecall.Name + " for group " + this.GroupToAffect.Name; }
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public Scene SceneToRecall { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public LightGroup GroupToAffect { get; set; }
    [JsonIgnore]
    public string RecallSceneResourceAddress { get => "/groups/" + this.GroupToAffect.ID + "/action/"; }
    [JsonPropertyName("scene")]
    public string Scene { get => SceneToRecall.ID; set => SceneToRecall = Program.hueBridge.Scenes[value]; }
    public static GroupRecallSceneData GetRecallSceneData(Scene scene, LightGroup group) {
      return new GroupRecallSceneData() { SceneToRecall = scene, GroupToAffect = group };
    }
    public static void Recall(Scene scene, LightGroup group, bool printInfo, bool pauseBeforeUpdating) {
      GetRecallSceneData(scene, group).Recall(printInfo, pauseBeforeUpdating);
    }
    public void Recall(bool printInfo, bool pauseBeforeUpdating) {
      if (printInfo) Console.WriteLine("Setting group " + this.GroupToAffect.Name + " to scene " + this.SceneToRecall.Name);
      if (printInfo) Console.WriteLine("Transition time (of first light in scene): " + this.SceneToRecall.Lights.First().TransitionTime);
      Program.hueBridge.UpdateBridge(this.RecallSceneResourceAddress, GroupRecallSceneData.GetRecallSceneData(this.SceneToRecall, this.GroupToAffect), printInfo, printInfo, pauseBeforeUpdating);
    }
  }
}
