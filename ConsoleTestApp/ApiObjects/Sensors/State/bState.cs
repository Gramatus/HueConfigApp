using System;
using System.Linq;
using System.Reflection;
using System.ComponentModel;

namespace ConsoleTestApp.ApiObjects.Sensors.State {
  public static class EnumExtensionMethodsAndGenerics {
    //Hint: Change the method signature and input paramter to use the type parameter T
    public static string GetDescription<T>(this T GenericEnum) where T : Enum {
      Type genericEnumType = GenericEnum.GetType();
      var memberInfo = genericEnumType.GetMember(GenericEnum.ToString());
      if ((memberInfo != null && memberInfo.Length > 0)) {
        var _Attribs = memberInfo[0].GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false);
        if ((_Attribs != null && _Attribs.Count() > 0)) {
          return ((System.ComponentModel.DescriptionAttribute)_Attribs.ElementAt(0)).Description;
        }
      }
      // Fallback if no description was found
      return GenericEnum.ToString();
    }
    // {
  }
  public enum bState {
    /// <summary>DON'T use this normally. It will trigger on ALL presses, whether short or long. Use short_release instead.</summary>
    [Description("init")]
    initial_press = 0,
    /// <summary>Triggers after the button has been held for about a second. Then (I believe) it will re-trigger (every second or half-second or something?), thus it can be used e.g. for dimming up while holding.</summary>
    [Description("Hold")]
    repeat = 1,
    /// <summary>Triggers only if the button is released quickly</summary>
    [Description("Trykk")]
    short_release = 2,
    /// <summary>Triggers after a hold is done. Probably most useful for "finishing" tasks or something.</summary>
    [Description("Slipp")]
    long_release = 3
  }
}
