using ConsoleTestApp.JsonConverters;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ConsoleTestApp.Helpers {
  public class BridgeResponse {
    [JsonPropertyName("success")]
    [JsonConverter(typeof(GenericDictionaryJsonConverter))]
    public Dictionary<string, string> success { get; set; }
    [JsonPropertyName("error")]
    public ErrorEntry error { get; set; }
  }
}