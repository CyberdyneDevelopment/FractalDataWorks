using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Dataverses.Abstractions;

namespace Fdw.Data.DataSets;

/// <summary>
/// A data set attached to a dataverse.
/// </summary>
/// <remarks>
/// Declared here rather than in the dataverses package because this package owns the data set. A
/// host that has not referenced data sets cannot attach one to a dataverse, and the resource-kind
/// collection is therefore an accurate description of what that host can actually do.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataverseResourceKinds), "DataSet")]
public sealed class DataSetResourceKind : DataverseResourceKindBase
{
    /// <summary>Initializes a new instance of the <see cref="DataSetResourceKind"/> class.</summary>
    public DataSetResourceKind()
        : base("DataSet", canBeOwned: true)
    {
    }
}
