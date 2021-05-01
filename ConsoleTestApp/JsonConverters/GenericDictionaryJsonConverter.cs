using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConsoleTestApp.JsonConverters {
  class GenericDictionaryJsonConverter : JsonConverter<Dictionary<string, string>> {
    public override Dictionary<string, string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
      if (reader.TokenType != JsonTokenType.StartObject) {
        throw new JsonException();
      }
      var dataList = new Dictionary<string, string>();
      while (reader.Read()) {
        if (reader.TokenType == JsonTokenType.EndObject) {
          break;
        }
        if (reader.TokenType != JsonTokenType.PropertyName) {
          throw new JsonException();
        }
        string propertyName = reader.GetString();
        reader.Read();
        string entry = "";
        if (reader.TokenType == JsonTokenType.Number) entry = reader.GetDouble().ToString();
        else if (reader.TokenType == JsonTokenType.True) entry = reader.GetBoolean().ToString();
        else if (reader.TokenType == JsonTokenType.False) entry = reader.GetBoolean().ToString();
        else if (reader.TokenType == JsonTokenType.Null) entry = null;
        else if (reader.TokenType == JsonTokenType.None) entry = null;
        else if (reader.TokenType == JsonTokenType.String) entry = reader.GetString();
        else if (reader.TokenType == JsonTokenType.StartArray) {
          while (reader.Read()) {
            if (reader.TokenType == JsonTokenType.EndArray) {
              break;
            }
            if (reader.TokenType != JsonTokenType.String) {
              // The values in the array are *not* strings...
              var obj = JsonSerializer.Deserialize<object>(ref reader, options);
              entry += obj.ToString() + Environment.NewLine;
            }
            else {
              entry += reader.GetString() + ", ";
            }
          }
          entry = entry.TrimEnd(',', ' ');
        }
        // We have an objecct within the object
        else if (reader.TokenType == JsonTokenType.StartObject) {
          // Skip for now (but break so that we are aware of what is happening)
          Program.hueBridge.Log("SystemErrors", "Deserializing a dictionary, current property is: \"" + propertyName + "\" and contains an object. This is NOT handled in the generic dictionary converter!");
          var obj = JsonSerializer.Deserialize<object>(ref reader, options);
          entry += obj.ToString() + Environment.NewLine;
        }
        else throw new Exception("Unhandled tokentype!");
        dataList.Add(propertyName, entry);
      }
      return dataList;
    }

    public override void Write(Utf8JsonWriter writer, [DisallowNull] Dictionary<string, string> value, JsonSerializerOptions options) {
      throw new NotImplementedException();
    }
  }
}
