using System.Text.Json.Serialization;

namespace ConsoleTestApp.Helpers {
  public class ErrorEntry {
    [JsonPropertyName("type")]
    public int type { get; set; }
    [JsonPropertyName("address")]
    public string address { get; set; }
    [JsonPropertyName("description")]
    public string description { get; set; }

  }
}