using System;

namespace ConsoleTestApp.ApiObjects.Schedules {
  public class WeekdayBitmask {
    public static WeekDays workingDays { get => WeekDays.Monday | WeekDays.Tuesday | WeekDays.Wednesday | WeekDays.Thursday | WeekDays.Friday; }
    private byte ActualValue {
      get => Convert.ToByte(this._container.WeekdayPart.Substring(1, 3), 10);
      set => this._container.WeekdayPart = "W" + Program.PadString(Convert.ToString(value, 10), false, 3, '0') + "/";
    }
    private Alarm _container;
    public WeekDays SelectedDays {
      get => (WeekDays)Convert.ToInt32(this.ActualValue);
      set => this.ActualValue = Convert.ToByte(value);
    }
    public string strDecimal {
      get => Program.PadString(Convert.ToString(this.ActualValue, 10), false, 3, '0');
      set => this.ActualValue = Convert.ToByte(value, 10);
    }
    public string strBinary {
      get => Program.PadString(Convert.ToString(this.ActualValue, 2), false, 7, '0');
      set => this.ActualValue = Convert.ToByte(value, 2);
    }
    public WeekdayBitmask(Alarm container) {
      this._container = container;
    }
  }
}
