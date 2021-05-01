using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleTestApp.Helpers {
  public static class StringExtensions {
    public static string FixNorwegianChars(this string value) {
      return value?.Replace('Æ', 'E').Replace('Ø', 'O').Replace('Å', 'A').Replace('æ', 'e').Replace('ø', 'o').Replace('å', 'a');
    }
    public static string Truncate(this string value, int maxLength) {
      if (value == null) return null;
      if (maxLength < 1) return "";
      return value.Substring(0, Math.Min(value.Length, maxLength));
    }
    public static string TruncateStart(this string value, int maxLength) {
      if (value == null) return null;
      if (maxLength < 1) return "";
      int actualMaxLength = Math.Min(value.Length, maxLength);
      return value.Substring(value.Length - actualMaxLength, actualMaxLength);
    }
  }
}
