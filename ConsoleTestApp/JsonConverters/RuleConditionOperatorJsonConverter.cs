using ConsoleTestApp.ApiObjects.Lights.State;
using ConsoleTestApp.ApiObjects.Rules;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConsoleTestApp.JsonConverters {
  public class RuleConditionOperatorJsonConverter : JsonConverter<RuleConditionOperator> {
    public override RuleConditionOperator Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
      string StringValue = reader.GetString();
      RuleConditionOperator value;
      if (StringValue == "not stable") StringValue = "not_stable";
      if (StringValue == "in") StringValue = "time_in";
      if (StringValue == "not in") StringValue = "time_not_in";
      if (Enum.TryParse(StringValue, out value)) return value;
      else return RuleConditionOperator.eq;
    }

    public override void Write(Utf8JsonWriter writer, [DisallowNull] RuleConditionOperator value, JsonSerializerOptions options) {
      string StringValue = value.ToString();
      if (StringValue == "not_stable") StringValue = "not stable";
      if (StringValue == "time_in") StringValue = "in";
      if (StringValue == "time_not_in") StringValue = "not in";
      writer.WriteStringValue(StringValue);
    }
  }
}