using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Web.Abstractions;

/// <summary>
/// Radio button group component type.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ComponentTypes), "RadioGroup", RestrictToCurrentCompilation = true)]
public sealed class RadioGroupComponentType : ComponentTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RadioGroupComponentType"/> class.
    /// </summary>
    public RadioGroupComponentType() : base(9, "RadioGroup", "Radio Group", "Selection", "Radio button group") { }
}