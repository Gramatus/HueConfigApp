using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ConsoleTestApp.ApiObjects.Sensors {
  public class SensorConfig {
    [JsonPropertyName("on")]
    public bool IsOn { get; set; }
    [JsonPropertyName("reachable")]
    public bool IsReachable { get; set; }
    [JsonExtensionData]
    public Dictionary<string, object> ExtensionData { get; set; }
    /*
				"config": {
					"on": true,
					"battery": 100,
					"reachable": true,
					"pending": []
				},
		"config": {
			"on": true,
			"battery": 100,
			"reachable": true,
			"alert": "none",
			"sensitivity": 2,
			"sensitivitymax": 2,
			"ledindication": false,
			"usertest": false,
			"pending": []
		},
		*/
  }
}
/*
	"2": {
		"state": {
			"buttonevent": 4002,
			"lastupdated": "2020-05-08T03:35:30"
		},
		"swupdate": { },
		"config": { },
		"name": "BryterSoverom",
		"type": "ZLLSwitch",
		"modelid": "RWL021",
		"manufacturername": "Signify Netherlands B.V.",
		"productname": "Hue dimmer switch",
		"diversityid": "73bbabea-3420-499a-9856-46bf437e119b",
		"swversion": "6.1.1.28573",
		"uniqueid": "00:17:88:01:08:73:d9:cb-02-fc00",
		"capabilities": {
 */
