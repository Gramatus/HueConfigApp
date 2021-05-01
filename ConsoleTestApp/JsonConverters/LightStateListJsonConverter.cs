using ConsoleTestApp.ApiObjects.Lights.State;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConsoleTestApp.JsonConverters {
  public class LightStateListJsonConverter : JsonConverter<List<LightState>> {
    public override List<LightState> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
      if (reader.TokenType != JsonTokenType.StartObject) {
        throw new JsonException();
      }
      var lightStateList = new List<LightState>();
      while (reader.Read()) {
        if (reader.TokenType == JsonTokenType.EndObject) {
          break;
        }
        if (reader.TokenType != JsonTokenType.PropertyName) {
          throw new JsonException();
        }
        string propertyName = reader.GetString();
        var lightState = JsonSerializer.Deserialize<LightState>(ref reader, options);
        if (Program.hueBridge.Lights.ContainsKey(propertyName)) lightState.ConnectToLight(Program.hueBridge.Lights[propertyName]);
        else throw new KeyNotFoundException("Scene contains a light that is not in the bridge!");
        lightStateList.Add(lightState);
      }
      return lightStateList;
    }

    public override void Write(Utf8JsonWriter writer, List<LightState> value, JsonSerializerOptions options) {
      var lightStateDictionary = new Dictionary<string, LightState>();
      foreach (var lightState in value) {
        string serializedState = "";
        if (lightState.colorModeCalculated == ColorMode.hs) serializedState = JsonSerializer.Serialize(new LightStateHueColor(lightState));
        else if (lightState.colorModeCalculated == ColorMode.ct) serializedState = JsonSerializer.Serialize(new LightStateTemp(lightState));
        else if (lightState.colorModeCalculated == ColorMode.xy) serializedState = JsonSerializer.Serialize(new LightStateXy(lightState));
        else serializedState = JsonSerializer.Serialize<LightStateBase>(lightState);
        var newStateObject = JsonSerializer.Deserialize<LightState>(serializedState);
        // var tmp = JsonSerializer.Serialize<LightState>(newStateObject, new JsonSerializerOptions() { IgnoreNullValues = true });
        newStateObject.ColorMode = null;
        lightStateDictionary.Add(lightState.LightID, newStateObject);
      }
      JsonSerializer.Serialize(writer, lightStateDictionary, options);
      #region Example of writing an object (something within {}) manually (content is not written manually)
      /*
      writer.WriteStartObject();
      foreach (var kvp in lightStateDictionary) {
        writer.WritePropertyName(kvp.Key);
        JsonSerializer.Serialize(writer, kvp.Value, options);
      }
      writer.WriteEndObject();
      */
      #endregion
      // string serializedValue = JsonSerializer.Serialize<Dictionary<string, LightState>>(lightStateDictionary, options);
      // writer.WriteStringValue(serializedValue);
      // throw new NotImplementedException();
    }
  }
}