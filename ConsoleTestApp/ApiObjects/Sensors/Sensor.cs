using ConsoleTestApp.ApiObjects.Sensors.State;
using ConsoleTestApp.Helpers;
using ConsoleTestApp.JsonConverters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConsoleTestApp.ApiObjects.Sensors {
  #region Sensor<T>
  public class Sensor<T> : Sensor where T : SensorState, new() {
    [JsonPropertyName("state")]
    public T State { get; set; }
    #region ### Constructor
    public Sensor() {
      this.State = new T();
      if (this.State is BoolSensorState) {
        this.ModelID = "Gramatus Flag";
        this.Type = "CLIPGenericFlag";
      }
      else if (this.State is IntSensorState) {
        this.ModelID = "Gramatus Status";
        this.Type = "CLIPGenericStatus";
      }
      else {
        // We only create the two types above, so this is not relevant
      }
    }
    /// <summary></summary>
    /// <param name="myID">Used to set a "UniqueID" for the sensor. Required, but not important (and I don't think it needs to be unique...)</param>
    public Sensor(string myID) : this() {
      this.AddGenericData(myID);
    }
    #endregion
  }
  #endregion
  #region ConvertedSensor
  /// <summary>Used when serializing/deserializing, to avoid converter loops and stack overflow
  ///
  /// </summary>
  public class ConvertedSensor : Sensor {
    [JsonPropertyName("state")]
    [JsonConverter(typeof(SensorStateJsonConverter))]
    public object ObjState { get; set; }
  }
  #endregion
  #region Sensor
  [JsonConverter(typeof(SensorJsonConverter))]
  public class Sensor :IApiObject{
    #region ### Instance properties
    [JsonIgnore]
    public string ID { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Type { get; set; }
    [JsonPropertyName("modelid")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string ModelID { get; set; }
    [JsonPropertyName("swversion")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string SW_version { get; set; }
    [JsonPropertyName("uniqueid")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string UniqueID { get; set; }
    [JsonPropertyName("manufacturername")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string ManufacturerName { get; set; }
    [JsonPropertyName("config")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SensorConfig Config { get; set; }
    [JsonExtensionData]
    public Dictionary<string, object> ExtensionData { get; set; }
    [JsonIgnore]
    public bool IsCreatedFromCode { get; set; }
    #endregion
    #region ### Instance methods
    #region CopyFromOtherSensor
    public void CopyFromOtherSensor(Sensor source) {
      this.Config = source.Config;
      this.ExtensionData = source.ExtensionData;
      this.ID = source.ID;
      this.ManufacturerName = source.ManufacturerName;
      this.ModelID = source.ModelID;
      this.Name = source.Name;
      this.SW_version = source.SW_version;
      this.Type = source.Type;
      this.UniqueID = source.UniqueID;
    }
    #endregion
    #region CreateIfMissingHijackIfExisting
    public void CreateIfMissingHijackIfExisting(bool printInfo, bool pauseBeforeUpdating, bool leaveExisting = false) {
      var oldSensor = Program.hueBridge.Sensors.Values.FirstOrDefault(i => i.Name == this.Name && !i.IsCreatedFromCode);
      // If there already exists a sensor in the bridge with this name, we "hijack" it and send an update from our new sensor
      if (oldSensor != null) {
        this.ID = oldSensor.ID;
        if (leaveExisting) return;
        this.Update(printInfo, pauseBeforeUpdating);
        Program.hueBridge.Sensors[this.ID] = this;
      }
      else {
        this.Create(printInfo, pauseBeforeUpdating);
        this.IsCreatedFromCode = false;
      }
    }
    #endregion
    #region Update
    public void Update(bool printInfo, bool pauseBeforeUpdating) {
      var type = this.Type;
      var modelid = this.ModelID;
      var manufact = this.ManufacturerName;
      var swver = this.SW_version;
      var uniqueid = this.UniqueID;
      this.Type = null;
      this.ModelID = null;
      this.ManufacturerName = null;
      this.SW_version = null;
      this.UniqueID = null;
      Program.hueBridge.UpdateBridge("/sensors/" + ID + "/", this, printInfo, printInfo, pauseBeforeUpdating);
      this.Type = type;
      this.ModelID = modelid;
      this.ManufacturerName = manufact;
      this.SW_version = swver;
      this.UniqueID = uniqueid;
    }
    #endregion
    #region Create
    public void Create(bool printInfo, bool pauseBeforeUpdating) {
      if (this.ID != null) throw new Exception("Sensor already created!");
      string id = Program.hueBridge.AddToBridge("/sensors/", this, printInfo, printInfo, pauseBeforeUpdating);
      // var responseObject = JsonSerializer.Deserialize<List<BridgeResponse>>(response);
      this.ID = id; // responseObject[0].success["id"];
      if (Program.hueBridge.Sensors.ContainsKey(this.ID)) Program.hueBridge.Sensors[this.ID] = this;
      else Program.hueBridge.Sensors.Add(this.ID, this);
    }
    #endregion
    #region AddGenericData
    /// <summary></summary>
    /// <param name="myID">Used to set a "UniqueID" for the sensor. Required, but not important (and I don't think it needs to be unique...)</param>
    public void AddGenericData(string myID) {
      this.ManufacturerName = "GramatusWeb";
      this.UniqueID = "GramatusSensor_" + myID;
      this.SW_version = "0.1";
    }
    #endregion
    #region CreateBoolSensor
    /// <summary></summary>
    /// <param name="Name"></param>
    /// <param name="myID">Used to set a "UniqueID" for the sensor. Required, but not important (and I don't think it needs to be unique...)</param>
    /// <param name="state"></param>
    /// <param name="printInfo"></param>
    /// <param name="pauseBeforeUpdating"></param>
    /// <returns></returns>
    public static Sensor<BoolSensorState> CreateBoolSensor(string Name, string myID, bool state, bool printInfo, bool pauseBeforeUpdating) {
      var sensor = new Sensor<BoolSensorState>(myID);
      sensor.Name = Name;
      sensor.State = new BoolSensorState() { State = state };
      sensor.Create(printInfo, pauseBeforeUpdating);
      // Program.hueBridge.Sensors.Add(sensor.ID, sensor);
      return sensor;
    }
    #endregion
    #region CreateIntSensor
    /// <summary></summary>
    /// <param name="Name"></param>
    /// <param name="myID">Used to set a "UniqueID" for the sensor. Required, but not important (and I don't think it needs to be unique...)</param>
    /// <param name="state"></param>
    /// <param name="printInfo"></param>
    /// <param name="pauseBeforeUpdating"></param>
    /// <returns></returns>
    public static Sensor<IntSensorState> CreateIntSensor(string Name, string myID, int state, bool printInfo, bool pauseBeforeUpdating) {
      var sensor = new Sensor<IntSensorState>(myID);
      sensor.Name = Name;
      sensor.State = new IntSensorState() { State = state };
      sensor.Create(printInfo, pauseBeforeUpdating);
      // Program.hueBridge.Sensors.Add(sensor.ID, sensor);
      return sensor;
    }
    #endregion
    #endregion
  }
  #endregion
}
/*
	"2": {
		"state": {
			"buttonevent": 4002,
			"lastupdated": "2020-05-08T03:35:30"
		},
		"swupdate": { },
		"config": { },
		"name": "BryterSoverom",
		"type": "ZLLSwitch",
		"modelid": "RWL021",
		"manufacturername": "Signify Netherlands B.V.",
		"productname": "Hue dimmer switch",
		"diversityid": "73bbabea-3420-499a-9856-46bf437e119b",
		"swversion": "6.1.1.28573",
		"uniqueid": "00:17:88:01:08:73:d9:cb-02-fc00",
		"capabilities": {
 */
