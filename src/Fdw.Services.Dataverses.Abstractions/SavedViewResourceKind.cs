using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>
/// A saved visualisation attached to a dataverse.
/// </summary>
/// <remarks>
/// Declared here because the dataverses domain owns the saved view type itself.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataverseResourceKinds), "SavedView")]
public sealed class SavedViewResourceKind : DataverseResourceKindBase
{
    /// <summary>Initializes a new instance of the <see cref="SavedViewResourceKind"/> class.</summary>
    public SavedViewResourceKind()
        : base("SavedView", canBeOwned: true)
    {
    }
}
