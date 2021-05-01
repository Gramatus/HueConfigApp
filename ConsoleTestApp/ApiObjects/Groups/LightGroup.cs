using ConsoleTestApp.ApiObjects.Lights;
using ConsoleTestApp.ApiObjects.Scenes;
using ConsoleTestApp.JsonConverters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;

namespace ConsoleTestApp.ApiObjects.Groups {
  public class LightGroup {
    [JsonIgnore]
    public string ID { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonConverter(typeof(LightGroupListJsonConverter))]
    [JsonPropertyName("lights")]
    public List<Light> Lights { get; set; }
    [JsonPropertyName("sensors")]
    public string[] Sensors { get; set; }
    [JsonPropertyName("type")]
    public string GroupType { get; set; }
    [JsonPropertyName("state")]
    public Dictionary<string, bool> state { get; set; }
    [JsonPropertyName("recycle")]
    public bool CanBeAutoRecycled { get; set; }
    /// <summary>The light state of one of the lamps in the group.</summary>
    // [JsonPropertyName("action")]
    // public Dictionary<string, string> StateOfExampleLamp { get; set; }
    public void RecallScene(Scene sceneToRecall, bool printInfo, bool pauseBeforeUpdating) {
      GroupRecallSceneData.Recall(sceneToRecall, this, printInfo, pauseBeforeUpdating);
    }
  }
}
/*
	"14": {
		"name": "Alle ambience",
		"lights": [
			"8",
			"10",
			"6",
			"5"
		],
		"sensors": [],
		"type": "LightGroup",
		"state": {
			"all_on": true,
			"any_on": true
		},
		"recycle": false,
		"action": {
			"on": true,
			"bri": 254,
			"ct": 331,
			"alert": "select",
			"colormode": "ct"
		}
	}, 
*/
