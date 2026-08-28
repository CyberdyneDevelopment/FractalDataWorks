using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Universes.Abstractions;

/// <summary>
/// A saved visualisation attached to a universe.
/// </summary>
/// <remarks>
/// Declared here because the universes domain owns the saved view type itself.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(UniverseResourceKinds), "SavedView")]
public sealed class SavedViewResourceKind : UniverseResourceKindBase
{
    /// <summary>Initializes a new instance of the <see cref="SavedViewResourceKind"/> class.</summary>
    public SavedViewResourceKind()
        : base(
            id: 13,
            name: "SavedView",
            displayName: "Saved view",
            description: "A stored visualisation of a data set, which lineage can point at.",
            canBeOwned: true)
    {
    }
}
