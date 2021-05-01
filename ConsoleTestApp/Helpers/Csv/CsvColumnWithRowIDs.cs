using System.Collections.Generic;
using System.Linq;

namespace ConsoleTestApp.Helpers.Csv {
  /// <summary>Describes a column in a CSV-file. It is in the form of a dictionary, where each entry has an ID that identifies the row as the key and the data as the value.</summary>
  public class CsvColumnWithRowIDs : Dictionary<string, string> {
    /// <summary>Quick access to just the data as a list</summary>
    public List<string> Rows { get { return Values.ToList(); } }
  }
}
