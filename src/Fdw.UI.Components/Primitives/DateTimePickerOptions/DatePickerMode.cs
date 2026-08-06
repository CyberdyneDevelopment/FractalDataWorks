using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Primitives.DateTimePickerOptions;

/// <summary>
/// Date only.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DateTimePickerModes), "Date", RestrictToCurrentCompilation = true)]
public sealed class DatePickerMode : DateTimePickerModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DatePickerMode"/> class.
    /// </summary>
    public DatePickerMode() : base(0, "Date", includesDate: true, includesTime: false, htmlInputType: "date", displayFormat: "yyyy-MM-dd") { }
}
