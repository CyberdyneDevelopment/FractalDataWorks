using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>A field of a data set.</summary>
[TypeOption(typeof(MapKindOptions), "DataSetField")]
[ExcludeFromCodeCoverage]
public sealed class DataSetFieldMapKind : MapKindOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="DataSetFieldMapKind"/> class.</summary>
    public DataSetFieldMapKind()
        : base("DataSetField")
    {
    }
}
