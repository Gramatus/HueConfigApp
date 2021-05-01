using ConsoleTestApp.ApiObjects.Sensors.State;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConsoleTestApp.JsonConverters {
  class SensorStateJsonConverter : JsonConverter<object> {
    public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
      var objState = JsonSerializer.Deserialize<SensorState>(ref reader, options);
      if (objState.ExtensionData.ContainsKey("flag")) { return new BoolSensorState() { LastUpdated = objState.LastUpdated, State = Boolean.Parse(objState.ExtensionData["flag"].ToString()), ExtensionData = objState.ExtensionData }; }
      else if (objState.ExtensionData.ContainsKey("status")) { return new IntSensorState() { LastUpdated = objState.LastUpdated, State = Int32.Parse(objState.ExtensionData["status"].ToString()), ExtensionData = objState.ExtensionData }; }
      else if (objState.ExtensionData.ContainsKey("buttonevent")) {
        // Button has never been pressed...
        if(objState.ExtensionData["buttonevent"] == null) return new DimmerButtonSensorState() { LastUpdated = objState.LastUpdated, ExtensionData = objState.ExtensionData };
        else return new DimmerButtonSensorState() { LastUpdated = objState.LastUpdated, State = Int32.Parse(objState.ExtensionData["buttonevent"].ToString()), ExtensionData = objState.ExtensionData };
      }
      else if (objState.ExtensionData.ContainsKey("temperature")) { return new TemperatureSensorState() { LastUpdated = objState.LastUpdated, State = Int32.Parse(objState.ExtensionData["temperature"].ToString()), ExtensionData = objState.ExtensionData }; }
      else if (objState.ExtensionData.ContainsKey("daylight")) { return new BoolSensorState() { LastUpdated = objState.LastUpdated, State = Boolean.Parse(objState.ExtensionData["daylight"].ToString()), ExtensionData = objState.ExtensionData }; }
      else if (objState.ExtensionData.ContainsKey("presence")) { return new BoolSensorState() { LastUpdated = objState.LastUpdated, State = Boolean.Parse(objState.ExtensionData["presence"].ToString()), ExtensionData = objState.ExtensionData }; }
      else throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, [DisallowNull] object value, JsonSerializerOptions options) {
      throw new NotImplementedException();
    }
  }
}
