using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Web.Abstractions;

/// <summary>
/// Date and time selection picker component type.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ComponentTypes), "DateTimePicker", RestrictToCurrentCompilation = true)]
public sealed class DateTimePickerComponentType : ComponentTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DateTimePickerComponentType"/> class.
    /// </summary>
    public DateTimePickerComponentType() : base(6, "DateTimePicker", "Date-Time Picker", "Input", "Date and time selection picker") { }
}