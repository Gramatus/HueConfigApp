using System.ComponentModel;

namespace ConsoleTestApp.ApiObjects.Sensors.State {
  public enum dButton {
    [Description("1")]
    On = 1,
    [Description("2")]
    DimUp = 2,
    [Description("3")]
    DimDown = 3,
    [Description("4")]
    Off = 4
  }
}
