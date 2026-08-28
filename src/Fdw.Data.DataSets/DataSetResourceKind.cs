using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Universes.Abstractions;

namespace Fdw.Data.DataSets;

/// <summary>
/// A data set attached to a universe.
/// </summary>
/// <remarks>
/// Declared here rather than in the universes package because this package owns the data set. A
/// host that has not referenced data sets cannot attach one to a universe, and the resource-kind
/// collection is therefore an accurate description of what that host can actually do.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(UniverseResourceKinds), "DataSet")]
public sealed class DataSetResourceKind : UniverseResourceKindBase
{
    /// <summary>Initializes a new instance of the <see cref="DataSetResourceKind"/> class.</summary>
    public DataSetResourceKind()
        : base(
            id: 1,
            name: "DataSet",
            displayName: "Data set",
            description: "A data set, which a universe may own and which need not be bound to a source.",
            canBeOwned: true)
    {
    }
}
