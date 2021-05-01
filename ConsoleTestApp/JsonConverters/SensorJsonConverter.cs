using ConsoleTestApp.ApiObjects.Sensors;
using ConsoleTestApp.ApiObjects.Sensors.State;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConsoleTestApp.JsonConverters {
  class SensorJsonConverter : JsonConverter<Sensor> {
    public override Sensor Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
      var objData = JsonSerializer.Deserialize<ConvertedSensor>(ref reader, options);
      Sensor sensor;
      if (objData.ObjState is BoolSensorState) sensor = new Sensor<BoolSensorState>() { State = (BoolSensorState)objData.ObjState };
      else if (objData.ObjState is IntSensorState) sensor = new Sensor<IntSensorState>() { State = (IntSensorState)objData.ObjState };
      else if (objData.ObjState is DimmerButtonSensorState) sensor = new Sensor<DimmerButtonSensorState>() { State = (DimmerButtonSensorState)objData.ObjState };
      else if (objData.ObjState is TemperatureSensorState) sensor = new Sensor<TemperatureSensorState>() { State = (TemperatureSensorState)objData.ObjState };
      else throw new NotImplementedException();
      sensor.CopyFromOtherSensor(objData);
      return sensor;
    }

    public override void Write(Utf8JsonWriter writer, [DisallowNull] Sensor value, JsonSerializerOptions options) {
      if (value is Sensor<BoolSensorState>) JsonSerializer.Serialize<Sensor<BoolSensorState>>(writer, (Sensor<BoolSensorState>)value, options);
      else if (value is Sensor<IntSensorState>) JsonSerializer.Serialize<Sensor<IntSensorState>>(writer, (Sensor<IntSensorState>)value, options);
      else if (value is Sensor<DimmerButtonSensorState>) JsonSerializer.Serialize<Sensor<DimmerButtonSensorState>>(writer, (Sensor<DimmerButtonSensorState>)value, options);
      else if (value is Sensor<TemperatureSensorState>) JsonSerializer.Serialize<Sensor<TemperatureSensorState>>(writer, (Sensor<TemperatureSensorState>)value, options);
      else throw new NotImplementedException();
      // JsonSerializer.Serialize<ConvertedSensor>(writer, value, options);
    }
  }
}
