using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConsoleTestApp.JsonConverters {
  class DateTimeWithNoneJsonConverter : JsonConverter<DateTime?> {
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
      var v = reader.GetString();
      if (v == "none") return null;
      else return JsonSerializer.Deserialize<DateTime?>(ref reader, options);
    }
    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options) {
      if (value == null) writer.WriteStringValue("none");
      else JsonSerializer.Serialize(writer, value, options);
    }
  }
}
