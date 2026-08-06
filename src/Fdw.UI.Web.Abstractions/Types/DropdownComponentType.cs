using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Web.Abstractions;

/// <summary>
/// Single selection dropdown component type.
/// </summary>
// Why: pure TypeOption leaf — literal constructor values only, no logic to test.
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ComponentTypes), "Dropdown", RestrictToCurrentCompilation = true)]
public sealed class DropdownComponentType : ComponentTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DropdownComponentType"/> class.
    /// </summary>
    public DropdownComponentType() : base(7, "Dropdown", "Dropdown", "Selection", "Single selection dropdown") { }
}