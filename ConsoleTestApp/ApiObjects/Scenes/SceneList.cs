using System.Collections.Generic;

namespace ConsoleTestApp.ApiObjects.Scenes {
  public class SceneList : Dictionary<string, Scene> {
    public new Scene this[string Key] {
      get {
        if (!base.ContainsKey(Key)) return null;
        GetSceneDetailsIfNeeded(Key);
        return base[Key];
      }
      set { base[Key] = value; }
    }
    public SceneList(Dictionary<string, Scene> innerList) {
      foreach (var entry in innerList) {
        entry.Value.ID = entry.Key;
        Add(entry.Key, entry.Value);
      }
    }

    private void GetSceneDetailsIfNeeded(string Key) {
      if (base[Key].Lights == null) {
        base[Key] = base[Key].GetDetails(); // Get lightstates for scene
      }
    }
  }
}
