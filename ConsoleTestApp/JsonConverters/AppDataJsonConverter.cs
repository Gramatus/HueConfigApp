using ConsoleTestApp.ApiObjects.Scenes;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConsoleTestApp.JsonConverters {
  class AppDataJsonConverter : JsonConverter<SceneAppData> {
    public override SceneAppData Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
      return JsonSerializer.Deserialize<SceneAppData>(ref reader, options);
      /*if (reader.TokenType != JsonTokenType.StartObject) {
        throw new JsonException();
      }
      while (reader.Read()) {
        if (reader.TokenType == JsonTokenType.EndObject) {
          break;
        }
        if (reader.TokenType != JsonTokenType.PropertyName) {
          throw new JsonException();
        }
        string propertyName = reader.GetString();
        var tmp = JsonSerializer.Deserialize<SceneAppData>(ref reader, options);
      }
      return lightStateList;*/
    }

    public override void Write(Utf8JsonWriter writer, [DisallowNull] SceneAppData value, JsonSerializerOptions options) {
      JsonSerializer.Serialize(writer, value, options);
    }
  }
}
