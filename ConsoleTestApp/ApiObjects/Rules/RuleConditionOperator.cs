using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleTestApp.ApiObjects.Rules {
  public enum RuleConditionOperator {
    /// <summary>Equals</summary>
    eq,
    /// <summary>Greater than</summary>
    gt,
    /// <summary>Less than</summary>
    lt,
    /// <summary>Value changed</summary>
    dx,
    /// <summary>Delayed value changed</summary>
    ddx,
    /// <summary>Stable for a given time</summary>
    stable,
    /// <summary>Not stable for a given time</summary>
    not_stable,
    /// <summary>Current time in given time interval</summary>
    time_in,
    /// <summary>Current time not in given time interval</summary>
    time_not_in
  }
}
