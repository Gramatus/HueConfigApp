using System.Text.Json.Serialization;

namespace ConsoleTestApp.Helpers {
  public class ResponseEntry {
    [JsonPropertyName("id")]
    public string id { get; set; }
  }
}