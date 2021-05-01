using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConsoleTestApp.JsonConverters {
  // DEPRECATED!
  /*class JsonJsonConverter : JsonConverter<string> {
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
      throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, [DisallowNull] string value, JsonSerializerOptions options) {
      var tmp = new JsonEncodedText();
      value.Replace("\"", "'quote'");
      // writer.WriteString(new JsonEncodedText() {   });
      throw new NotImplementedException();
    }
  }*/
}
