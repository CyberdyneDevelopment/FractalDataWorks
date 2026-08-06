using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Components.Primitives.DateTimePickerOptions;

/// <summary>
/// TypeCollection for DateTimePicker component modes.
/// </summary>
/// <remarks>
/// Provides compile-time discovery and O(1) lookup for DateTimePicker modes.
/// Source generator creates static properties for each registered picker mode.
/// </remarks>
[TypeCollection(typeof(DateTimePickerModeBase), typeof(IDateTimePickerMode), typeof(DateTimePickerModes))]
public sealed partial class DateTimePickerModes : TypeCollectionBase<DateTimePickerModeBase, IDateTimePickerMode>
{
}
