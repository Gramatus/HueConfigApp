using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace ConsoleTestApp.Helpers.Csv {
  /// <summary>This class contains CSV-data. The headers are the key, and the data for each column are the values.</summary>
  public class CsvDataWithRowIDs : Dictionary<string, CsvColumnWithRowIDs> {
    #region ### Instance fields
    private string _colNameID;
    #endregion
    #region ### Instance properties
    public List<CsvColumnWithRowIDs> Columns { get { return this.Values.ToList(); } }
    /// <summary>Key and value should be the same in this, but check that later</summary>
    public List<string> RowIDs { get => this[this._colNameID].Rows; }
    public List<string> Headers { get { return this.Keys.ToList(); } }
    #endregion
    #region ### Constructor
    public CsvDataWithRowIDs(string colNameID) {
      this._colNameID = colNameID;
    }
    #endregion
    #region ### Instance methods
    #region SafeAddJsonKeyValueStringToDataStore
    public void SafeAddJsonKeyValueStringToDataStore(string rowID, string keyvaluestring, string parentKey) {
      var content = keyvaluestring.Split(':', 2);
      var key = content[0].Trim(' ', '"');
      if (!string.IsNullOrEmpty(parentKey)) key = parentKey + "|" + key;
      var value = content[1].Replace("\",", "").TrimEnd(',').Trim(' ', '"');
      SafeAddToDataStore(rowID, key, value);
    }
    #endregion
    #region SafeAddIDColumnToDataStore
    public void SafeAddIDColumnToDataStore(string rowID, string value) {
      this.SafeAddToDataStore(rowID, this._colNameID, value);
    }
    #endregion
    #region SafeAddToDataStore
    public void SafeAddToDataStore(string rowID, string key, string value) {
      if (!this.ContainsKey(key)) this.Add(key, new CsvColumnWithRowIDs());
      if (!this[key].ContainsKey(rowID)) this[key].Add(rowID, value);
      else throw new DuplicateNameException("Already added a property " + key + " to row with ID " + rowID);
    }
    #endregion
    #region GetHeaders
    /// <summary>Get the headers for a CSV-file based on the contents of this object</summary>
    /// <returns>A one-line string that is the CSV header row</returns>
    private string GetHeaders() {
      string headers = "";
      foreach (var header in Headers) {
        headers += header + ";";
      }
      return headers.TrimEnd(';');
    }
    #endregion
    #region GetData
    /// <summary>Gets the data part of the CSV-file that is described by this object</summary>
    /// <returns>The text that equals the content of this CSV-file</returns>
    private string GetData() {
      string data = "";
      // Each column has the rowID as its keys. Some columns might not have data for all rows, but the ID-column should.
      var rowIDs = this.RowIDs;
      #region Iterate over all rows
      foreach (var row in rowIDs) {
        #region Iterate over all columns (e.g all headers)
        for (int col = 0; col < this.Headers.Count; col++) {
          if (this.Columns[col].ContainsKey(row)) data += "\"" + this.Columns[col][row] + "\";";
          else data += ";";
        }
        #endregion
        data = data.TrimEnd(';') + Environment.NewLine;
      }
      #endregion
      return data;
    }
    #endregion
    #region GetCsv
    /// <summary>Get the CSV-representation of the data in this object</summary>
    /// <returns>A string that can be saved as a CSV-file</returns>
    public string GetCsv() {
      return GetHeaders() + Environment.NewLine + GetData();
    }
    #endregion
    #endregion
    #region ### Static methods
    #region ReadCsv
    public static CsvDataWithRowIDs ReadFromCsv(string csv, string colNameID) {
      var data = new CsvDataWithRowIDs(colNameID);
      var lines = csv.Split(Environment.NewLine);
      var headers = lines[0].Split(';');
      int colNumID = headers.ToList().IndexOf(colNameID);
      for (int row = 1; row < lines.Length; row++) {
        if (string.IsNullOrEmpty(lines[row])) continue;
        var columns = lines[row].Split(';');
        var rowID = string.IsNullOrEmpty(columns[colNumID]) ? Guid.NewGuid().ToString() : columns[colNumID];
        for (int col = 0; col < headers.Length; col++) {
          if (col == colNumID) data.SafeAddToDataStore(rowID, headers[col], rowID); // We already stored this columns value in rowID, replaced with a GUID if empty
          else if (columns.Length <= col) data.SafeAddToDataStore(rowID, headers[col], ""); // some rows end in blank, and then the split function will not add the last column to the array
          else data.SafeAddToDataStore(rowID, headers[col], columns[col]);
        }
      }
      return data;
    }
    #endregion
    #endregion
  }
}
