using ConsoleTestApp.ApiObjects.Lights.State;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConsoleTestApp.JsonConverters {
  public class ColorModeJsonConverter : JsonConverter<ColorMode?> {
    public override ColorMode? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
      string StringValue = reader.GetString();
      ColorMode value;
      if (Enum.TryParse(StringValue, out value)) return value;
      else return null;
    }

    public override void Write(Utf8JsonWriter writer, [DisallowNull] ColorMode? value, JsonSerializerOptions options) {
      if (value != null) {
        writer.WriteStringValue(value.ToString());
      }
    }
  }
}