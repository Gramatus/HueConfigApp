using ConsoleTestApp.ApiObjects.Config;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConsoleTestApp.JsonConverters {
  public class WhitelistJsonConverter : JsonConverter<Dictionary<string, WhitelistEntry>> {
    public override Dictionary<string, WhitelistEntry> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
      if (reader.TokenType != JsonTokenType.StartObject) {
        throw new JsonException();
      }
      var whitelist = new Dictionary<string, WhitelistEntry>();
      while (reader.Read()) {
        if (reader.TokenType == JsonTokenType.EndObject) {
          break;
        }
        if (reader.TokenType != JsonTokenType.PropertyName) {
          throw new JsonException();
        }
        string propertyName = reader.GetString();

        var entry = JsonSerializer.Deserialize<WhitelistEntry>(ref reader, options);
        entry.ID = propertyName;
        whitelist.Add(propertyName, entry);
      }
      return whitelist;
    }

    public override void Write(Utf8JsonWriter writer, [DisallowNull] Dictionary<string, WhitelistEntry> value, JsonSerializerOptions options) {
      throw new NotImplementedException();
    }
  }
}
