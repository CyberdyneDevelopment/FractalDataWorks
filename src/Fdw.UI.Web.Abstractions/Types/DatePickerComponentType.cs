using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Web.Abstractions;

/// <summary>
/// Date selection picker component type.
/// </summary>
// Why: pure TypeOption leaf — literal constructor values only, no logic to test.
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ComponentTypes), "DatePicker", RestrictToCurrentCompilation = true)]
public sealed class DatePickerComponentType : ComponentTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DatePickerComponentType"/> class.
    /// </summary>
    public DatePickerComponentType() : base(5, "DatePicker", "Date Picker", "Input", "Date selection picker") { }
}