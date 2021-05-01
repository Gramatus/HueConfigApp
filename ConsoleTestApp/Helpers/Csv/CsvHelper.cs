using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace ConsoleTestApp.Helpers.Csv {

  #region FileData
  #endregion
  #region CsvHelper
  public static class CsvHelper {
    #region GetJsonFromCsvFile
    public static string GetJsonFromCsvFile(string fileName, string colNameID, string[] complexJsonProps, string[] prettyPrintIntProps, bool addEmptyProperties) {
      int WIN_1252_CP = 28591; // Windows ANSI codepage 1252 (when running c#, it seems I have to use 28591 to get CP1252...)
      var data = CsvDataWithRowIDs.ReadFromCsv(System.IO.File.ReadAllText(fileName, Encoding.GetEncoding(WIN_1252_CP)), colNameID);
      var Rows = new Dictionary<string, Dictionary<string, string>>();
      #region Get an ID for all rows
      foreach (var rowID in data.RowIDs) {
        // If we are reading a CSV with no ID supplied, use a GUID. Note that this ID will not be used when creating objects anyways.
        Rows.Add(rowID, new Dictionary<string, string>()); // add a new object with this ID
        // Rows.Add(row.Value, new Dictionary<string, string>()); 
      }
      #endregion
      #region Restructure the data from a list of rows with only data to a list (dictionary) of rows where each column in the row also knows its header
      // iterate over each column
      for (int colNum = 0; colNum < data.Columns.Count; colNum++) {
        // iterate over each row (all columns should have the same number of rows in a csv-file)
        for (int rowNum = 0; rowNum < data.Columns[0].Count; rowNum++) {
          Rows[data.RowIDs[rowNum]].Add(data.Headers[colNum], data.Columns[colNum].Rows[rowNum]);
        }
      }
      #endregion
      string json = "{" + Environment.NewLine;
      #region Iterate over all rows in the CSV-file (in the restructured form)
      // The Rows object has one entry for each row that has it's ID as the key (based on an assumption that each row has an ID-column).
      // Then each row contains a dictionary where the corresponding column header is the key and the column value is the value
      foreach (var row in Rows) {
        // string parentKey = row.Key;
        Dictionary<string, string> columns = row.Value;
        #region All kinds of special cases that needs special handling
        // Note that "parentKey" is the parent of whatever level of recursion we are at, while the current scope is the top level. Thus we need to pass it in as a parameter to "getReturnTypeFunc".
        jsonValueType getReturnTypeFunc(string propKey, string propValue, string parentKey) {
          jsonValueType returnType = jsonValueType.tryParse;
          // for just this one file, also numbers and booleans are stored as string... (!)
          if (fileName.ToLower().Contains("rule") && fileName.ToLower().Contains("conditions") && propKey == "value") returnType = jsonValueType.alwaysString;
          // "body"-objects must be strings to work (at least in some cases)
          else if (parentKey == "body") returnType = jsonValueType.alwaysString;
          else if (propKey == "group") returnType = jsonValueType.alwaysString;
          else if (prettyPrintIntProps.Contains(propKey)) returnType = jsonValueType.alwaysString;
          else if (propKey == "xy") returnType = jsonValueType.neverString;
          else if (propKey == "lights" || propKey == "GroupList") returnType = jsonValueType.neverString;
          // These we don't care about getting back (unless they have already been handled by a rule above)
          else if (complexJsonProps != null && complexJsonProps.Contains(propKey)) returnType = jsonValueType.alwaysNothing;
          if (string.IsNullOrEmpty(propValue) && !addEmptyProperties) returnType = jsonValueType.alwaysNothing;
          return returnType;
        }
        #endregion
        json += CsvHelper.GetJsonForDataRow(
          row.Value,
          row.Key,
          getReturnTypeFunc,
          // The lights-property needs special handling (but in all instances? or should this be more specific?)
          (propKey, propValue) => (propKey == "lights") ? propValue.TrimEnd(',').Replace("[", "[\"").Replace(",", "\",\"").Replace("]", "\"]") : propValue
                );
      }
      #endregion
      json = json.TrimEnd(Environment.NewLine.ToCharArray().Concat(new char[] { ',' }).ToArray()) + Environment.NewLine + "}";
      return json;
    }
    #endregion
    #region GetJsonForDataRow
    /// <summary>Used to create a json property that contains an object based on a dictonary of the keys and values that are part of that object.</summary>
    /// <param name="props">
    /// The "props"-parameter is a dictionary of properties where the key is the property name and the value is the property value
    /// If we are calling this based on a CSV-file, this would equal a dictionary for all columns in a single row, where the corresponding column header is the key and the column value is the value
    /// If we are calling it for a subproperty-type-object, it would still be the same concept, but the keys would be the column header when the corresponding parent columns that belong together has been taken out to a separate (virtual) file
    /// Thus, if we have the following CSV:
    /// Name;ID;SomeValue;Body|OtherValue;Body|SubObj|a;Body|SubObj|b
    /// Then the first pass would have each value within the semicolon as the key in props
    /// The second pass would have these keys:
    /// OtherValue;SubObj|a;SubObj|b
    /// The third pass would have these keys:
    /// a;b
    /// </param>
    /// <param name="parentKey">We assume that each row we are handling is ID'ed by some key</param>
    /// <param name="getValueType">It is the callers responsibility to decide what kind of value to return, using this callback function</param>
    /// <param name="formatValue">This parameter can be null. If it is not null, the parameter key and value will be passed to this function.
    /// The expectation is that the value is returned, possibly reformatted (e.g. if there is a given key or a given value or some other logic implmeneted outside this function.</param>
    /// <returns>A string on the format "parentKey": { a json-object derived from the passed props }</returns>
    public static string GetJsonForDataRow(Dictionary<string, string> props, string parentKey, Func<string, string, string, jsonValueType> getValueType, Func<string, string, string> formatValue) {
      #region containedObjects
      // containedObjects handles any *columns* that is used for child objects. These columns will be on the form body|flag, and body|value
      // These will then be stored in a dictonary of dictionaries where the key is the parent object (e.g. "body"),
      // and the value is a dictionary where the key is the sub-property name (e.g. "flag")
      // and the value is the value (unless it is nested deeper, then we will do the same thing for another level)
      // See also function parameter description for "props"
      var containedObjects = new Dictionary<string, Dictionary<string, string>>();
      #endregion
      var sortedProps = props.OrderBy(i => i.Key); // To make sure we don't miss objects that fit together (not sure if it is needed), anyways it shouldn't matter that the returned JSON is sorted
      string json = "\"" + parentKey + "\": {" + Environment.NewLine;
      #region Iterate over all properties ("columns" for this "row")
      foreach (var prop in sortedProps) {
        string propValue = prop.Value;
        if (formatValue != null) propValue = formatValue(prop.Key, propValue);
        var returnType = getValueType(prop.Key, propValue, parentKey);
        #region Handle any "sub objects" embedded in a column, and store these columns for handling in a new pass (recursive)
        // If the key of the subprop is on the form body|flag (which in the serialization meant that we split a subobject into multiple columns in the same file)
        if (prop.Key.Contains('|')) {
          var parentPropName = prop.Key.Split('|', 2)[0];
          var childPropName = prop.Key.Split('|', 2)[1];
          if (!containedObjects.ContainsKey(parentPropName)) containedObjects.Add(parentPropName, new Dictionary<string, string>());
          containedObjects[parentPropName].Add(childPropName, propValue);
        }
        #endregion
        #region Handle "normal" columns (not sub-objects)
        else {
          if (returnType == jsonValueType.alwaysNothing) json += "";
          else if (returnType == jsonValueType.alwaysString) json += "\"" + prop.Key + "\": \"" + propValue + "\"," + Environment.NewLine;
          else if (returnType == jsonValueType.neverString) json += "\"" + prop.Key + "\": " + propValue + "," + Environment.NewLine;
          // The last three lines handles the TryParse-scenario
          else if (int.TryParse(propValue, out int dummyInt)) json += "\"" + prop.Key + "\": " + propValue + "," + Environment.NewLine;
          else if (bool.TryParse(propValue, out bool dummyBool)) json += "\"" + prop.Key + "\": " + propValue + "," + Environment.NewLine;
          else json += "\"" + prop.Key + "\": \"" + propValue + "\"," + Environment.NewLine;
        }
        #endregion
      }
      #endregion
      #region Iterate over all discovered "sub-objects"
      foreach (var obj in containedObjects) {
        json += GetJsonForDataRow(obj.Value, obj.Key, getValueType, formatValue);
      }
      #endregion
      json = json.TrimEnd(Environment.NewLine.ToCharArray().Concat(new char[] { ',' }).ToArray()) + "}," + Environment.NewLine;
      return json;
    }

    #endregion
    #region valueType enum
    /// <summary>Defines different approaches to return JSON data.</summary>
    public enum jsonValueType {
      /// <summary>Always return as a string, even if it can be pasred as something else</summary>
      alwaysString,
      /// <summary>Never return as a string (e.g. never add "" around the value, even if it cannot be parsed as something else)</summary>
      neverString,
      /// <summary>Try to parse it as an Int or a Bool, return string if that doesn't work</summary>
      tryParse,
      /// <summary>Don't return this property no matter what</summary>
      alwaysNothing
    }
    #endregion
    #region GetMultiLineValue
    /// <summary>The main CSV-functions in this application splits json into lines and then handles them by iterating over the lines in a for-loop.
    /// But sometimes we want to send a multiline-object to another function as one entity, typically for properties that are themselves arrays or objects.
    /// What this function does is find the end of the array/object (taking into account nested arrays/objects) and returning the whole array/object as one string.
    /// At the same time, the counter used by the outer function is incremented to tell where we currently are.</summary>
    /// <param name="jsonLines">The complete json we are working on in the form of an array split by lines.</param>
    /// <param name="pos">The position in jsonLines that the caller was currently at when calling this function, will be updated to the correct end position because it is a REF parameter.</param>
    /// <param name="startChar">The start character (normally either { for objects, or [ for arrays, but it could be anything that fits a start/stop pattern.</param>
    /// <param name="endChar">The end character (normally either } for objects, or ] for arrays, but it could be anything that fits a start/stop pattern.</param>
    /// <returns>A string containing all the lines that fits the object, with one exception: the first line is swapped with a single startChar (i.e. "propname": { is returned only as a single { og [ character).</returns>
    public static string GetMultiLineValue(this string[] jsonLines, ref int pos, char startChar, char endChar) {
      bool endFound = false;
      int nestCounter = 0;
      string subJson = startChar + Environment.NewLine;
      pos++;
      while (!endFound) {
        if (jsonLines[pos].EndsWith("{}") || jsonLines[pos].EndsWith("{},")) { // we have an empty object nested within this object
                                                                               // do nothing here
        }
        else if (jsonLines[pos].EndsWith(startChar)) { // we have a nested object
          nestCounter++;
        }
        else if (jsonLines[pos].TrimEnd(',').EndsWith(endChar)) { // End of an object
          if (nestCounter >= 1) nestCounter--;
          else endFound = true;
        }
        else { // normal key/value
               // do nothing here
        }
        subJson += jsonLines[pos] + Environment.NewLine;
        pos++;
      }
      return subJson;
    }
    #endregion
    #region DEPRECATED
    #region JsonToCsv (old method)
    public static string JsonToCsv_deprecated(string json) {
      // var json = this.SerializeJson();
      json = json.Replace("[" + Environment.NewLine, "");
      json = json.Replace(Environment.NewLine + "]", "");
      json = json.Replace("  " + Program.PadString("null"), "\" \"");
      var lines = new List<Dictionary<string, string>>();
      foreach (var line in json.Split(Environment.NewLine)) {
        var dict = new Dictionary<string, string>();
        foreach (var prop in line.Replace("{\"", "").Replace("\"},", "").Replace("\"}", "").Split("\",\"")) {
          var item = prop.Split("\":\"");
          dict.Add(item[0], item[1]);
        }
        lines.Add(dict);
      }
      string csv = "";
      foreach (var key in lines.First().Keys) { csv += key + ";"; }
      csv = csv.TrimEnd(';') + Environment.NewLine;
      foreach (var line in lines) {
        foreach (var val in line.Values) { csv += val + ";"; }
        csv = csv.TrimEnd(';') + Environment.NewLine;
      }
      return csv;
    }
    #endregion
    #region DeserializeCsv (old method)
    public static string CsvToJson_deprecated(string csv) {
      var lines = new List<Dictionary<string, string>>();
      var keys = new List<string>();
      string[] arrLines = csv.Split(Environment.NewLine);
      foreach (var key in arrLines[0].Split(';')) keys.Add(key);
      for (int i = 0; i < arrLines.Length; i++) {
        if (i == 0) continue;
        if (string.IsNullOrEmpty(arrLines[i])) continue;
        var dict = new Dictionary<string, string>();
        string[] arrProps = arrLines[i].Split(';');
        for (int ii = 0; ii < arrProps.Length; ii++) {
          string prop = arrProps[ii];
          if (string.IsNullOrWhiteSpace(prop)) prop = "  " + Program.PadString("null");
          else prop = "\"" + Program.PadString(prop, true) + "\"";
          dict.Add(keys[ii], prop);
        }
        lines.Add(dict);
      }
      string Json = "";
      foreach (var line in lines) {
        string lineJson = "";
        foreach (var prop in line) {
          lineJson += "\"" + prop.Key + "\":" + prop.Value + ",";
        }
        Json += "{" + lineJson.TrimEnd(',') + "}," + Environment.NewLine;
      }
      Json = "[" + Environment.NewLine + Json.TrimEnd(("," + Environment.NewLine).ToCharArray()) + Environment.NewLine + "]";
      return Json;
    }
    #endregion
    #endregion
  }
  #endregion
}
