using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Primitives.DateTimePickerOptions;

/// <summary>
/// Date and time.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DateTimePickerModes), "DateTime", RestrictToCurrentCompilation = true)]
public sealed class DateTimeMode : DateTimePickerModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DateTimeMode"/> class.
    /// </summary>
    public DateTimeMode() : base(2, "DateTime", includesDate: true, includesTime: true, htmlInputType: "datetime-local", displayFormat: "yyyy-MM-ddTHH:mm") { }
}
