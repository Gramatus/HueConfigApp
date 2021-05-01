using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConsoleTestApp.JsonConverters {
  public class PrettyNullabeIntJsonConverter : JsonConverter<int?> {
    public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
      if (reader.TokenType == JsonTokenType.Null) return null;
      else {
        string v = reader.GetString().Trim();
        int value;
        if (Int32.TryParse(v, out value)) return value;
        else return null;
      }
    }

    public override void Write(Utf8JsonWriter writer, [DisallowNull] int? value, JsonSerializerOptions options) {
      string v = value.ToString();
      writer.WriteStringValue(Program.PadString(v));
    }
  }
  public class PrettyIntJsonConverter : JsonConverter<int> {
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
      string v = reader.GetString().Trim();
      int value;
      if (Int32.TryParse(v, out value)) return value;
      else return 0;
    }

    public override void Write(Utf8JsonWriter writer, [DisallowNull] int value, JsonSerializerOptions options) {
      string v = value.ToString();
      writer.WriteStringValue(Program.PadString(v));
    }
  }
}
