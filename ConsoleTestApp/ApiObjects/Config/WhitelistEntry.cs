using System;
using System.Text.Json.Serialization;

namespace ConsoleTestApp.ApiObjects.Config
{
  public class WhitelistEntry
  {
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string ID { get; set; }
    [JsonPropertyName("name")]
    public string name { get; set; }
    [JsonPropertyName("last use date")]
    public DateTime LastUsedDate { get; set; }
    [JsonPropertyName("create date")]
    public DateTime CreatedDate { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string AppName {
      get {
        if (name.Contains('#')) return name.Split('#')[0];
        else return name;
      }
    }
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string DeviceName {
      get {
        if (name.Contains('#')) return name.Split('#')[1];
        else return "";
      }
    }
  }
}
