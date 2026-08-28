using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Web.Abstractions;

/// <summary>
/// Multiple selection component type.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ComponentTypes), "MultiSelect", RestrictToCurrentCompilation = true)]
public sealed class MultiSelectComponentType : ComponentTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MultiSelectComponentType"/> class.
    /// </summary>
    public MultiSelectComponentType() : base(8, "MultiSelect", "Multi-Select", "Selection", "Multiple selection") { }
}