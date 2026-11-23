using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>A data set.</summary>
[TypeOption(typeof(MapKindOptions), "DataSet")]
[ExcludeFromCodeCoverage]
public sealed class DataSetMapKind : MapKindOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="DataSetMapKind"/> class.</summary>
    public DataSetMapKind()
        : base("DataSet")
    {
    }
}
