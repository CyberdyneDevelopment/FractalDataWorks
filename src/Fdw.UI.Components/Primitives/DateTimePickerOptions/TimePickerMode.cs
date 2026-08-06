using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Primitives.DateTimePickerOptions;

/// <summary>
/// Time only.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DateTimePickerModes), "Time", RestrictToCurrentCompilation = true)]
public sealed class TimePickerMode : DateTimePickerModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TimePickerMode"/> class.
    /// </summary>
    public TimePickerMode() : base(1, "Time", includesDate: false, includesTime: true, htmlInputType: "time", displayFormat: "HH:mm") { }
}
