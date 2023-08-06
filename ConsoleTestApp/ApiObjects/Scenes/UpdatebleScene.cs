using ConsoleTestApp.ApiObjects.Lights.State;
using ConsoleTestApp.JsonConverters;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace ConsoleTestApp.ApiObjects.Scenes {
  /// <summary>A version of the scene class that can be serialized to JSON that the bridge will accepts as input for changing a scene definition</summary>
  public class UpdatebleScene : IApiObject {
    [JsonIgnore]
    public string ID { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; }
    /// <summary>Not really needed, as the same list of lights is also in the light states, but needed in communication with the bridge.</summary>
    [JsonPropertyName("lights")]
    public string[] LightIDs {
      get { return Lights.Select(i => i.LightID).ToArray(); }
      set { }
    }
    [JsonConverter(typeof(LightStateListJsonConverter))]
    [JsonPropertyName("lightstates")]
    public List<LightState> Lights { get; set; }
    [JsonPropertyName("appdata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonConverter(typeof(AppDataJsonConverter))]
    public SceneAppData CustomAppData { get; set; }

  }
}
