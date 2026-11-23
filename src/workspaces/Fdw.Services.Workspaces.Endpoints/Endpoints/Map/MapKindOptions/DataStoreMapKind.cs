using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>A data store.</summary>
[TypeOption(typeof(MapKindOptions), "DataStore")]
[ExcludeFromCodeCoverage]
public sealed class DataStoreMapKind : MapKindOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="DataStoreMapKind"/> class.</summary>
    public DataStoreMapKind()
        : base("DataStore")
    {
    }
}
