using Fdw.Collections;

namespace Fdw.Services.Scheduling.Abstractions.OptionTypes;

/// <summary>
/// One rule for the fire times a schedule missed while the scheduler was down (FDW-3788).
/// </summary>
public interface IMissedFireRule : ITypeOption<int, IMissedFireRule>
{
    /// <summary>
    /// Gets whether the scheduler runs the schedule once, when it starts, for the fire times it missed, however many there were. False means the missed times are dropped
    /// and the scheduler only says how many.
    /// </summary>
    bool FiresOnce { get; }
}
