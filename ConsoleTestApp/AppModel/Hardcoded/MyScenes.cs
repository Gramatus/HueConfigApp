using ConsoleTestApp.ApiObjects.Groups;
using System;
using System.Collections.Generic;

namespace ConsoleTestApp.AppModel.Hardcoded {
  class MyScenes {
    #region ### SceneSets
    public static SceneDefinitionList EveningScenes { get { return GetEveningScenes(); } }
    public static SceneDefinitionList MorningScenes { get { return GetMorningScenes(); } }
    public static SceneDefinitionList WakeUpScenes { get { return GetWakeUpScenes(); } }
    #endregion
    #region ### Definitions
    #region GetEveningScenes
    private static SceneDefinitionList GetEveningScenes() {
      var list = new SceneDefinitionList();
      /*
      list.Add(new SceneDefinition("MstxNEg3-y10eVr", "1600 Kveldslys 1700", null, null, 0284, 284, 189, 189, 189, 36000));
      list.Add(new SceneDefinition("vQ4RYZOHyzy1NNy", "1700 Kveldslys 1715", null, null, 0307, 307, 187, 187, 187, 9000));
      list.Add(new SceneDefinition("87cV8vIm7rNgYdz", "1715 Kveldslys 1730", null, null, 0333, 333, 191, 191, 191, 9000));
      list.Add(new SceneDefinition("X3DqEQXqdEiiCZp", "1730 Kveldslys 1745", null, null, 0333, 333, 177, 177, 177, 9000));
      list.Add(new SceneDefinition("X-MV7lOQ8vvf8x9", "1745 Kveldslys 1800", null, null, 0334, 334, 166, 166, 166, 9000));
      list.Add(new SceneDefinition("vGwcdg3yTuli77c", "1800 Kveldslys 1815", null, null, 0353, 353, 165, 165, 165, 9000));
      list.Add(new SceneDefinition("OFIAUtyxN4oLKTf", "1815 Kveldslys 1830", null, null, 0377, 377, 185, 185, 185, 9000));
      list.Add(new SceneDefinition("O8O78ooUOdhXwQA", "1830 Kveldslys 1845", null, null, 0404, 404, 162, 162, 162, 9000));
      list.Add(new SceneDefinition("x23IvEak6Fy-F-l", "1845 Kveldslys 1900", null, null, 0434, 434, 164, 164, 164, 9000));
      list.Add(new SceneDefinition("kT4SzmeE-E4v3fB", "1900 Kveldslys 1915", null, null, 0449, 449, 149, 149, 149, 9000));
      list.Add(new SceneDefinition("9r-MA08LU8DTRbQ", "1915 Kveldslys 1930", null, null, 0465, 454, 137, 137, 137, 9000));
      list.Add(new SceneDefinition("Vi-ii3t5Cd1qD5T", "1930 Kveldslys 1945", null, null, 0481, 454, 111, 111, 111, 9000));
      list.Add(new SceneDefinition("fcPo2vMOyBelLX6", "1945 Kveldslys 2000", 4041, 0255, null, 454, 101, 068, 101, 9000));
      list.Add(new SceneDefinition("phEynbBb1fKpiPG", "2000 Kveldslys 2015", 3241, 0255, null, 454, 077, 073, 077, 9000));
      list.Add(new SceneDefinition("Wsnn0BlKUptZbHS", "2015 Kveldslys 2030", 2442, 0255, null, 454, 000, 053, 000, 9000));
      */
      return list;
    }
    #endregion
    #region GetMorningScenes
    private static SceneDefinitionList GetMorningScenes() {
      var list = new SceneDefinitionList();
      // list.Add(new SceneDefinition("sPW5aB9pRdBQ-RT", "0545 Rolig morra 0600", null, null, 380, 380, 255, 255, 255, 9000));
      // list.Add(new SceneDefinition("7BeDynpBAuK420n", "0620 Konsentrer deg 0650", null, null, 233, 233, 254, 254, 254, 18000));
      return list;
    }
    #endregion
    #region GetWakeUpScenes
    private static SceneDefinitionList GetWakeUpScenes() {
      var list = new SceneDefinitionList();
      // list.Add(new HardcodedSceneDefinition("iDM3ZkLOphHoGHA", "0445 Natt 0445", 2442, 255, null, 454, 1, 1, 1, 100));
      // list.Add(new HardcodedSceneDefinition("an-vu6wipvOszk4", "0445 Nesten natt 0500", 4041, 255, null, 454, 64, 69, 64, 8900));
      // list.Add(new HardcodedSceneDefinition("WxfIYtJuoYmqF5C", "0500 God morgen 0515", null, null, 434, 434, 255, 255, 255, 9000));
      // list.Add(new SceneDefinition("AeIH8MVCR7l5gC1", "0515 Tid for aa vaakne 0545", null, null, 333, 333, 255, 255, 255, 18000));
      return list;
    }
    #endregion
    #endregion
  }
}
