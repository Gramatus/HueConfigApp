using ConsoleTestApp.ApiObjects.Schedules;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConsoleTestApp.JsonConverters {
  class ScheduleJsonConverter : JsonConverter<ConvertedSchedule> {
    public override ConvertedSchedule Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
      var objData = JsonSerializer.Deserialize<Schedule>(ref reader, options);
      if (objData.IsTimer) {
        var timer = new Timer();
        timer.CopyFromOtherSchedule(objData);
        return timer;
      }
      else {
        var alarm = new Alarm();
        alarm.CopyFromOtherSchedule(objData);
        return alarm;
      }
    }
    public override void Write(Utf8JsonWriter writer, [DisallowNull] ConvertedSchedule value, JsonSerializerOptions options) {
      JsonSerializer.Serialize(writer, value, options);
    }
  }
}
