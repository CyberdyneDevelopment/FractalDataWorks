using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Web.Abstractions;

/// <summary>
/// TypeCollection reference dropdown component type.
/// </summary>
// Why: pure TypeOption leaf — literal constructor values only, no logic to test.
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ComponentTypes), "TypeCollectionDropdown", RestrictToCurrentCompilation = true)]
public sealed class TypeCollectionDropdownComponentType : ComponentTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TypeCollectionDropdownComponentType"/> class.
    /// </summary>
    public TypeCollectionDropdownComponentType() : base(12, "TypeCollectionDropdown", "TypeCollection Dropdown", "Complex", "TypeCollection reference dropdown") { }
}