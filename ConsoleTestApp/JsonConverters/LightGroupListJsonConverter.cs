using ConsoleTestApp.ApiObjects.Lights;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConsoleTestApp.JsonConverters {
  public class LightGroupListJsonConverter : JsonConverter<List<Light>> {
    public override List<Light> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
      if (reader.TokenType != JsonTokenType.StartArray) {
        throw new JsonException();
      }
      var lightList = new List<Light>();
      while (reader.Read()) {
        if (reader.TokenType == JsonTokenType.EndArray) {
          break;
        }
        if (reader.TokenType != JsonTokenType.String) {
          throw new JsonException();
        }
        string lightID = reader.GetString();
        if (Program.hueBridge.Lights.ContainsKey(lightID)) lightList.Add(Program.hueBridge.Lights[lightID]);
        else throw new KeyNotFoundException("Gruppen inneholder et lys med ID " + lightID + ", men dette finnes ikke i listen over lys i Hue Bridge!");
      }
      return lightList;
    }

    public override void Write(Utf8JsonWriter writer, [DisallowNull] List<Light> value, JsonSerializerOptions options) {
      string serializedValue = JsonSerializer.Serialize(value.Select(i => i.ID).ToArray());
      writer.WriteStringValue(serializedValue);
    }
  }
}
