using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace ConsoleTestApp.ApiObjects {
  public interface IApiObject {
    [JsonIgnore]
    public string ID { get; }
    [JsonPropertyName("name")]
    public string Name { get; }
  }
}
