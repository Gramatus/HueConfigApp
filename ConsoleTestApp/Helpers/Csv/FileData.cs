using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ConsoleTestApp.Helpers.Csv {
  /// <summary>This class contains csv-data to be stored in different files. The filenames are the key, and the data are the value.</summary>
  public class FileData : Dictionary<string, CsvDataWithRowIDs> {
    private string[] _dictProperties;
    private string[] _complexJsonProps;
    private TextInfo _caseHelper;
    private string _colNameID;
    private FileData(string colNameID, string[] dictProperties, string[] complexJsonProps) {
      this._colNameID = colNameID;
      this._dictProperties = dictProperties;
      this._complexJsonProps = complexJsonProps;
      this._caseHelper = new CultureInfo("nb-NO").TextInfo;
    }
    #region AddObject
    /// <summary>Add data from a JSON-object to the CSV, optionally creating separate files for arrays that are hardcoded to do that.</summary>
    /// <param name="json">A json string that is an array, and that should be added to this FileData-instance according to given rules.</param>
    /// <param name="fileName">If we are working on an object in the "Rules" json (i.e. a rule), then fileName will be "Rules" or "Groups", and key will be blank (the filename is also the key).</param>
    /// <param name="parentKey">However, if we are working on a subobject that should *not* have its own file, then parentKey will have a value. Then headers will be prefixed with the parentKey.</param>
    /// <param name="rowID">An ID value to identify the row we are currently reading data for</param>
    private void AddObject(string json, string fileName, string parentKey, string rowID) {
      fileName = this._caseHelper.ToTitleCase(fileName);
      if (!string.IsNullOrEmpty(fileName) && !this.ContainsKey(fileName)) this.Add(fileName, new CsvDataWithRowIDs(this._colNameID));
      var csvData = this[fileName];
      if (parentKey == null) csvData.SafeAddIDColumnToDataStore(rowID, rowID);

      var lines = json.Split(Environment.NewLine);
      int i = 1; // Skip first line, which is the object initializer {
      while (i < lines.Length) {
        // End of an object
        if (lines[i].EndsWith("},") || lines[i].EndsWith('}')) {
          i++;
        }
        // Start of an array
        else if (lines[i].EndsWith("[")) {
          string keyName = lines[i].Split(':', 2)[0].Trim(' ', '"');
          if (!string.IsNullOrEmpty(parentKey)) keyName = parentKey + "|" + keyName;
          string subArrayJson = lines.GetMultiLineValue(ref i, '[', ']');
          // If we for instance are handling the "Conditions" property of the "Rules" file, this would submit the parameters "Rules" and "conditions"
          this.HandleArray(subArrayJson, fileName, keyName, rowID);
        }
        // Value is an object
        else if (lines[i].EndsWith("{")) {
          string keyName = lines[i].Split(':', 2)[0].Trim(' ', '"');
          if (!string.IsNullOrEmpty(parentKey)) keyName = parentKey + "|" + keyName;
          string subObjectJson = lines.GetMultiLineValue(ref i, '{', '}');
          if (this._dictProperties.Contains(keyName)) {
            // This is a "dictionary" type structure, and in my code that is handled as an array, even if in JSON it is an object
            this.HandleArray(subObjectJson, fileName, keyName, rowID);
          }
          else {
            // Thus, all the objects in this subobject will have columns prefixed with the name of the parent object
            // E.g., if this is the "state" property of a light, we will get two columns in the csv, name state|all_on and state|any_on
            this.AddObject(subObjectJson, fileName, keyName, rowID);
          }
        }
        // Other lines ending in a comma is (hopefully) a simple name and value pair
        // If the next line is the object finalizer, this is (hopefully) a pure value without the comma
        else if (lines[i].EndsWith(",") || lines.Length > i + 1 && (lines[i + 1].EndsWith('}') || lines[i + 1].EndsWith("},"))) {
          csvData.SafeAddJsonKeyValueStringToDataStore(rowID, lines[i], parentKey);
          i++;
        }
        // Skip any line that is pure whitespace
        else if (string.IsNullOrEmpty(lines[i])) {
          i++;
        }
        else {
          string currentLine = lines[i];
          bool nextLineExists = lines.Length >= i;
          string Nextline;
          if (nextLineExists) Nextline = lines[i + 1];
          bool MyTest = lines[i].EndsWith(",") || lines.Length >= i && (lines[i + 1].EndsWith('}') || lines[i + 1].EndsWith("},"));
          throw new NotImplementedException();
        }
      }
    }
    #endregion
    #region HandleArray
    /// <summary>Handles arrays according to hardcoded logic:
    /// - should be saved as separate files, and of these:
    ///   - some are dictionaries and should be handled as such
    ///   - others are pure arrays and should be handles as such
    /// - others should simply add the raw json to the cell
    /// </summary>
    /// <param name="json">A json string that is an array, and that should be added to this FileData-instance according to given rules.</param>
    /// <param name="fileName">If we for instance are handling the "Conditions" property of the "Rules" file, the fileName would be "Rules"</param>
    /// <param name="key">If we for instance are handling the "Conditions" property of the "Rules" file, the key would be "conditions"</param>
    /// <param name="parentRowID">If we for instance are handling the "Conditions" property of the "Rules" file, this would be "Rules"</param>
    private void HandleArray(string json, string fileName, string key, string parentRowID) {
      string parentKey = this._caseHelper.ToTitleCase(fileName.TrimEnd('s'));
      #region Create parent key/value (for properties that should have a separate file)
      // Create a "key" and a "value" that should be added to the child file that contains data to identify the parent.
      // Could be multiple values, then they will simply have a semicolon between the columns. When saved as a CSV, this will be handles as multiple columns.
      // If "parentInfo" is null after this block, this should NOT be saved as a separate file
      KeyValuePair<string, string>? parentInfo = null;
      // These two properties are *arrays*, but we still want them in separate files. Another piece of pure logic hardcoded in this method.
      string[] separateFileArrayProperties = new string[] { "conditions", "actions" };
      #region if this is a know "dictionary" that we want saved to a separate file
      if (this._dictProperties.Concat(separateFileArrayProperties).Contains(key)) {
        if (fileName == "Config") {
          parentInfo = new KeyValuePair<string, string>("Config", "Everything");
        }
        else {
          string parentElementName = this[fileName]["name"][parentRowID];
          parentInfo = new KeyValuePair<string, string>(parentKey + "#;" + parentKey + "Name", parentRowID + ";" + parentElementName);
        }
      }
      #endregion
      #region if a pure array, but we still want it in a separate file (not used yet)
      else if (key == "xy") { // Fictional example really, but added to not miss the logic
        // At least something like this...
        // string parentElementName = this[parentKey]["Light#;LightName"][parentRowID];
        // parentInfo = new KeyValuePair<string, string>(parentKeySingular + "#;" + parentKeySingular + "Name", parentRowID + ";" + parentElementName);
      }
      #endregion
      #endregion
      #region Call functions to handle this array as agreed
      #region If we should create a separate file
      if (parentInfo != null) {
        if (this._dictProperties.Contains(key)) {
          this.AddDictonary(json, fileName + "_" + this._caseHelper.ToTitleCase(key), parentInfo);
        }
        else {
          // If we for instance are handling the "Conditions" property of the "Rules" file, this method would have received the parameters "Rules" and "conditions", and thus:
          // - Send the key "Rule_Conditions" as the new filename (key)
          // Also, the logic just above would make a key-/value-pair that can be used in the CSV to add two columns that is "Rule#" and "RuleName", with the correct values
          this.AddArray(json, this._caseHelper.ToTitleCase(parentKey) + "_" + this._caseHelper.ToTitleCase(key), parentInfo.Value);
        }
      }
      #endregion
      #region If we should save the pure JSON in the cell
      else {
        if (this._complexJsonProps.Contains(key)) {
          json = json.Replace(Environment.NewLine, "").Replace(" ", "").Replace("\"", "").TrimEnd(',').Trim(' ', '"');
        }
        else {
          json = "\"" + json.Replace("\"", "\"\"") + "\"";
        }
        // If we for instance are handling the "Conditions" property of the "Rules" file, this would get the parameters "Rules" and "conditions", and thus:
        // - Add data to the "Rules" file
        // - Add data for the current row we are working on (as given by parentRowID), in the column "conditions"
        // - Add the pure json stored in this property (rather useless, but good for making sure all data is saved)
        this[fileName].SafeAddToDataStore(parentRowID, key, json);
      }
      #endregion
      #endregion
    }
    #endregion
    #region AddArray
    /// <summary>Handle an array and save the values to a new file. This might be an existing file, but the file is for this level of data (e.g. it might be a file for "Rule_Condtions", containing all conditions for all rules).</summary>
    /// <param name="json">A json string that is an array, and that should be added to this FileData-instance.</param>
    /// <param name="fileName">The filename this array should be stored in. If we for instance are handling the "Conditions" property of the "Rules" file, this would submit the key "Rule_Conditions".</param>
    /// <param name="parentInfo">Header and data that should be added to all rows in the file</param>
    private void AddArray(string json, string fileName, KeyValuePair<string, string> parentInfo) {
      fileName = this._caseHelper.ToTitleCase(fileName);
      // We could be handling the "Rule_Conditions"-file many times with differnt parents, thus this "add if key does not exist"
      if (!this.ContainsKey(fileName)) this.Add(fileName, new CsvDataWithRowIDs(this._colNameID));
      var csvData = this[fileName];
      var lines = json.Split(Environment.NewLine);
      int i = 1; // skip first line, which is just the beginning [
      int rowID = 1;
      while (i < lines.Length) {
        #region Handle end of array or pure whitespace
        if (lines[i].EndsWith("],") || lines[i].EndsWith(']') || string.IsNullOrEmpty(lines[i])) {
          i++;
          continue;
        }
        #endregion
        #region Add information about the parent object to this child file
        // Will e.g. add the value "1;Some rule" to the column "Rule#;RuleName" (it is not a problem that this column actually contains two columns and two values, that will fix itself in the CSV)
        // The row will then have an internal identifier of e.g. "1;Some rule_1".
        string strRowID = parentInfo.Value.Replace(";", "_") + "_" + rowID;
        csvData.SafeAddToDataStore(strRowID, parentInfo.Key, parentInfo.Value);
        #endregion
        #region Handle the actual value for the next element
        #region Next element in the array is another array
        if (lines[i].EndsWith("[")) {
          string keyName = lines[i].Split(':', 2)[0].Trim(' ', '"');
          string subArrayJson = lines.GetMultiLineValue(ref i, '[', ']');
          this.HandleArray(subArrayJson, fileName, keyName, strRowID);
        }
        #endregion
        #region Next element in the array is an object
        else if (lines[i].EndsWith("{")) {
          string keyName = lines[i].Split(':', 2)[0].Trim(' ', '"');
          string subObjectJson = lines.GetMultiLineValue(ref i, '{', '}');
          this.AddObject(subObjectJson, fileName, null, strRowID);
        }
        #endregion
        #region Next element in the array is a value
        else {
          // Then add a column "Value" that contains the value
          csvData.SafeAddToDataStore(strRowID, "Value", lines[i].Trim(' ', '"'));
          i++;
        }
        #endregion
        #endregion
        rowID++;
      }
    }
    #endregion
    #region AddDictonary
    /// <summary>Hue uses a "dictonary" format for it's API, i.e. all main api's return an object where each property is really a value in a dictionary. This method handles that.</summary>
    /// <param name="json">The json object that equals this dictionary.</param>
    /// <param name="fileName">The filename to save the data to.</param>
    /// <param name="parentInfo">If this is used to add a child file, this contains a header and values to add to each row to identify the parent object.</param>
    private void AddDictonary(string json, string fileName, KeyValuePair<string, string>? parentInfo) {
      fileName = this._caseHelper.ToTitleCase(fileName);
      if (!this.ContainsKey(fileName)) this.Add(fileName, new CsvDataWithRowIDs(this._colNameID));
      var csvData = this[fileName];
      var lines = json.Split(Environment.NewLine);
      int i = 1; // skip first line, which is just the beginning {
      while (i < lines.Length - 1) { // skip last line, which is just the final } or },
        // End of object/array or pure whitespace
        if (
          lines[i].EndsWith("},") ||
          lines[i].EndsWith('}') ||
          string.IsNullOrEmpty(lines[i])) {
          i++;
          continue;
        }
        if (lines[i].EndsWith("],") || lines[i].EndsWith(']')) {
          Console.WriteLine("Why are we reaching the end of an array inside this function?");
        }
        string keyName = lines[i].Split(':', 2)[0].Trim(' ', '"');
        string strRowID = null;
        if (parentInfo != null) {
          strRowID = parentInfo.Value.Value.Replace(";", "_") + "_" + keyName;
          csvData.SafeAddToDataStore(strRowID, parentInfo.Value.Key, parentInfo.Value.Value);
        }
        string elementJson = lines.GetMultiLineValue(ref i, '{', '}');
        this.AddObject(elementJson, fileName, null, strRowID ?? keyName);
      }
    }
    #endregion
    #region CreateCsvFiles
    /// <summary>Traverse the given json, create files and subfiles (for array values we want in separate files) and save to the given directory.</summary>
    /// <param name="json"></param>
    /// <param name="topLevelFilename">The filename for the top level file, without extension (e.g. "rules")</param>
    /// <param name="folderPath">The path of the folder to save the csv files to. Should include the final \</param>
    /// <param name="dictProperties">An array of property names that we expect to be dictionaries. Dictionaries should be handled as an array, even though they are technically object.</param>
    /// <param name="complexJsonProps">An array of property names that we expect to be complex objects, but which we want to cramp into one cell, with no line breaks and other possible issues for CSV-files. We accept that this might make the data unusable, but this is data that is not important to us.</param>
    public static void CreateCsvFiles(string json, string topLevelFilename, string folderPath, string colNameID, string[] dictProperties, string[] complexJsonProps) {
      var data = new FileData(colNameID, dictProperties, complexJsonProps);
      // This will recursively handle everything in the json and convert it to a set of dictionaries that we can save as files
      string prettyJson = Program.hueBridge.Prettify(json);
      data.AddDictonary(prettyJson, topLevelFilename, null);
      foreach (var file in data) {
        string fileName = file.Key;
        var csvData = file.Value;
        System.IO.File.WriteAllText(folderPath + fileName + ".csv", csvData.GetCsv());
      }
    }
    #endregion
  }
}
