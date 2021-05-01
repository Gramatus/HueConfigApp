using ConsoleTestApp.ApiObjects.Rules;
using ConsoleTestApp.ApiObjects.Scenes;
using ConsoleTestApp.ApiObjects.Sensors.State;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace ConsoleTestApp.AppModel.SwitchConfiguration {

  struct sceneButton {
    public string[] scenes { get; private set; }
    public dButton button { get; private set; }
    public bState? state { get; private set; }
    public Rule[] rules { get; set; }
    public string ruleID2 { get; private set; }
    public static sceneButton[] getOffSetup(string groupName, string multiID, SwitchConfig dimmer, string onName = null, string onLightList = null, string dimUpName = null, string dimUpLightList = null, string dimDownName = null, string dimDownLightList = null, string offName = null, string offLightList = null) {
      var buttonList = new List<sceneButton>();
      if (onName != null) {
        Scene.CreatOnOrOffSceneIfMissing(onName, onLightList.Split(','), " av", false, false, false);
        buttonList.Add(new sceneButton { scenes = new string[] { onName + " av" }, button = dButton.On, ruleID2 = onName });
      }
      if (dimUpName != null) {
        Scene.CreatOnOrOffSceneIfMissing(dimUpName, dimUpLightList.Split(','), " av", false, false, false);
        buttonList.Add(new sceneButton { scenes = new string[] { dimUpName + " av" }, button = dButton.DimUp, ruleID2 = dimUpName });
      }
      if (dimDownName != null) {
        Scene.CreatOnOrOffSceneIfMissing(dimDownName, dimDownLightList.Split(','), " av", false, false, false);
        buttonList.Add(new sceneButton { scenes = new string[] { dimDownName + " av" }, button = dButton.DimDown, ruleID2 = dimDownName });
      }
      if (offName != null) {
        Scene.CreatOnOrOffSceneIfMissing(offName, offLightList.Split(','), " av", false, false, false);
        buttonList.Add(new sceneButton { scenes = new string[] { offName + " av" }, button = dButton.Off, ruleID2 = offName });
      }
      return buttonList.ToArray();
    }
    /// <summary>The "OnOffSetup" is a setup that triggers different groups on/off for different buttons. If no "offName" is provided, the off-button will trigger the whole group on/off.</summary>
    /// <param name="groupName"></param>
    /// <param name="multiID"></param>
    /// <param name="dimmer"></param>
    /// <param name="onName"></param>
    /// <param name="onLightList"></param>
    /// <param name="dimUpName"></param>
    /// <param name="dimUpLightList"></param>
    /// <param name="dimDownName"></param>
    /// <param name="dimDownLightList"></param>
    /// <param name="offName"></param>
    /// <param name="offLightList"></param>
    /// <returns></returns>
    public static sceneButton[] getOnOffSetup(string groupName, string multiID, SwitchConfig dimmer, string onName = null, string onLightList = null, string dimUpName = null, string dimUpLightList = null, string dimDownName = null, string dimDownLightList = null, string offName = null, string offLightList = null) {
      var buttonList = new List<sceneButton>();
      if (onName != null) {
        Scene.CreateOnOffScenesIfMissing(onName, onLightList.Split(','), " på", " av", false, false);
        buttonList.Add(new sceneButton { scenes = new string[] { onName + " av", onName + " på" }, button = dButton.On, ruleID2 = onName });
      }
      if (dimUpName != null) {
        Scene.CreateOnOffScenesIfMissing(dimUpName, dimUpLightList.Split(','), " på", " av", false, false);
        buttonList.Add(new sceneButton { scenes = new string[] { dimUpName + " av", dimUpName + " på" }, button = dButton.DimUp, ruleID2 = dimUpName });
      }
      if (dimDownName != null) {
        Scene.CreateOnOffScenesIfMissing(dimDownName, dimDownLightList.Split(','), " på", " av", false, false);
        buttonList.Add(new sceneButton { scenes = new string[] { dimDownName + " av", dimDownName + " på" }, button = dButton.DimDown, ruleID2 = dimDownName });
      }
      if (offName != null) {
        Scene.CreateOnOffScenesIfMissing(offName, offLightList.Split(','), " på", " av", false, false);
        buttonList.Add(new sceneButton { scenes = new string[] { offName + " av", offName + " på" }, button = dButton.Off, ruleID2 = offName });
      }
      else {
        var offRule = SwitchConfigHelpers.GetBasicTurnOffRule(
          dimmer._switch,
          dButton.Off,
          bState.short_release,
          ruleID1: "Multi" + multiID + "_",
          ruleID2: "Alle#av",
          dimmer.GetTransitionGroup(groupName),
          turnOffLights: true
        );
        var onRule = SwitchConfigHelpers.GetBasicTurnOnRule(
          dimmer._switch,
          dButton.Off,
          bState.short_release,
          ruleID1: "Multi" + multiID + "_",
          ruleID2: "Alle#paa",
          dimmer.GetTransitionGroup(groupName),
          turnOnLights: true
        );
        buttonList.Add(new sceneButton { scenes = new string[] { }, button = dButton.Off, rules = new Rule[] { offRule, onRule } });
      }
      return buttonList.ToArray();
    }
    public static sceneButton[] getFourButtonArray(string[] onScenes, string[] dimUpScenes, string[] dimDownScenes, string[] offScenes, Rule[] offRules = null) {
      return new sceneButton[] {
        new sceneButton{ scenes = onScenes, button = dButton.On },
        new sceneButton{ scenes = dimUpScenes, button = dButton.DimUp },
        new sceneButton{ scenes = dimDownScenes, button = dButton.DimDown },
        new sceneButton{ scenes = offScenes, button = dButton.Off, rules = offRules }
      };
    }
    public static sceneButton[] getFourButtonArray(string onScene, string dimUpScene, string dimDownScene, string offScene) {
      return new sceneButton[] {
        new sceneButton{ scenes = new string[]{ onScene }, button = dButton.On },
        new sceneButton{ scenes = new string[]{ dimUpScene }, button = dButton.DimUp },
        new sceneButton { scenes = new string[]{ dimDownScene }, button = dButton.DimDown },
        new sceneButton{ scenes = new string[]{ offScene }, button = dButton.Off }
      };
    }
    public static sceneButton Create(string scene, dButton button) {
      return new sceneButton { scenes = new string[] { scene }, button = button };
    }
  }
  static class sceneButtonExstensions {
    public static sceneButton[] AddButton(this sceneButton[] list, string scene, dButton button) {
      return list.Append(sceneButton.Create(scene, button)).ToArray();
    }
  }
}
