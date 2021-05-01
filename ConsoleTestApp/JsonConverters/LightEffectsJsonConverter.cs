using ConsoleTestApp.ApiObjects.Lights.State;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConsoleTestApp.JsonConverters {
  public class LightEffectsJsonConverter : JsonConverter<LightEffects?> {
    public override LightEffects? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
      string v = reader.GetString();
      LightEffects value;
      if (Enum.TryParse(v, out value)) return value;
      else return null;
    }

    public override void Write(Utf8JsonWriter writer, [DisallowNull] LightEffects? value, JsonSerializerOptions options) {
      if (value != null) {
        string v = value.ToString();
        writer.WriteStringValue(v);
      }
    }
  }
}
