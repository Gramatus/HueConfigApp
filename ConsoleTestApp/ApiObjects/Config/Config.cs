using ConsoleTestApp.JsonConverters;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace ConsoleTestApp.ApiObjects.Config {
  public class Config
  {
    [JsonPropertyName("whitelist")]
    [JsonConverter(typeof(WhitelistJsonConverter))]
    public Dictionary<string, WhitelistEntry> ConnectedApps { get; set; }
    public string GetUserNames()
    {
      string result = "";
      foreach (var app in ConnectedApps.Values)
      {
        result += app.AppName + "\t\t" + app.DeviceName + Environment.NewLine;
      }
      return result;
    }
  }
}
