using ConsoleTestApp.ApiObjects;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using ConsoleTestApp.ApiObjects.Lights.State;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace ConsoleTestApp.JsonConverters {
  class EnabledStateJsonConverter :JsonConverter<EnabledState?>{
    public override EnabledState? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
      string StringValue = reader.GetString();
      if (Enum.TryParse(StringValue, out EnabledState value)) return value;
      else return null;
    }

    public override void Write(Utf8JsonWriter writer, [DisallowNull] EnabledState? value, JsonSerializerOptions options) {
      if (value != null) {
        writer.WriteStringValue(value.ToString());
      }
    }
  }
}
