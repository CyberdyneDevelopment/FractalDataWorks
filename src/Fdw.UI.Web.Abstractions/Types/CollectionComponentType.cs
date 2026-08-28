using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Web.Abstractions;

/// <summary>
/// Nested collection component type.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ComponentTypes), "Collection", RestrictToCurrentCompilation = true)]
public sealed class CollectionComponentType : ComponentTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionComponentType"/> class.
    /// </summary>
    public CollectionComponentType() : base(11, "Collection", "Collection", "Complex", "Nested collection") { }
}