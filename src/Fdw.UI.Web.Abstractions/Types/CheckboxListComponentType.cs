using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Web.Abstractions;

/// <summary>
/// Checkbox list component type.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ComponentTypes), "CheckboxList", RestrictToCurrentCompilation = true)]
public sealed class CheckboxListComponentType : ComponentTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CheckboxListComponentType"/> class.
    /// </summary>
    public CheckboxListComponentType() : base(10, "CheckboxList", "Checkbox List", "Selection", "Checkbox list") { }
}