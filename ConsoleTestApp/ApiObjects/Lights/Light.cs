using ConsoleTestApp.ApiObjects.Lights.State;
using System;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace ConsoleTestApp.ApiObjects.Lights {
  public class Light {
    #region Instance properties
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("state")]
    public LightState state { get; set; }
    [JsonIgnore]
    public string ID { get; set; }
    [JsonPropertyName("capabilities")]
    public LightCapabilities Capabilities { get; set; }
    #region More properties
    /*
				"swupdate": {
					"state": "noupdates",
					"lastinstall": "2020-04-26T04:53:41"
				},
				"type": "Extended color light",
				"modelid": "LCG002",
				"manufacturername": "Signify Netherlands B.V.",
				"productname": "Hue color spot",
				"config": {
					"archetype": "spotbulb",
					"function": "mixed",
					"direction": "omnidirectional",
					"startup": {
						"mode": "powerfail",
						"configured": true
					}
				},
				"uniqueid": "00:17:88:01:08:50:a1:34-0b",
				"swversion": "1.65.9_hB3217DF",
				"swconfigid": "90C759F1",
				"productid": "Philips-LCG002-1-GU10ECLv2"
		 */
    #endregion
    #endregion
    #region Instance methods
    #region SetState
    public void SetState(bool printInfo, bool pauseBeforeUpdating) {
      // var response = SetLightState();
      // var responseMessage = response.Result.Content.ReadAsStringAsync().Result;
      if (state.colorModeCalculated == ColorMode.hs) Program.hueBridge.UpdateBridge("/lights/" + this.ID + "/state/", this.GetHueColorState(), printInfo, printInfo, pauseBeforeUpdating);
      else if (state.colorModeCalculated == ColorMode.ct) Program.hueBridge.UpdateBridge("/lights/" + this.ID + "/state/", this.GetTempState(), printInfo, printInfo, pauseBeforeUpdating);
      else if (state.colorModeCalculated == ColorMode.xy) throw new NotSupportedException();
      else Program.hueBridge.UpdateBridge("/lights/" + this.ID + "/state/", this.GetDimmerState(), printInfo, printInfo, pauseBeforeUpdating);
      // var responseMessage = Program.hueBridge.UpdateBridge("/lights/" + ID + "/state/", GetStateJson());
    }
    #endregion
    #region GetStateJson (Deprecated)
    /*public JsonContent GetStateJson() {
      JsonContent content;
      if (state.colorModeCalculated == ColorMode.hs) content = JsonContent.Create(GetHueColorState());
      else if (state.colorModeCalculated == ColorMode.ct) content = JsonContent.Create(GetTempState());
      else if (state.colorModeCalculated == ColorMode.xy) throw new NotSupportedException();
      else content = JsonContent.Create(GetDimmerState());
      return content;
    }*/
    #endregion
    #endregion
    #region Private helper methods
    private LightStateHueColor GetHueColorState() {
      return new LightStateHueColor(state);
    }
    private LightStateTemp GetTempState() {
      return new LightStateTemp(state);
    }
    private LightStateBase GetDimmerState() {
      return state;
    }
    #endregion
  }
}