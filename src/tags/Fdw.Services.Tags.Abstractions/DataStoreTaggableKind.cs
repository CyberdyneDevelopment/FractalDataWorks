using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Tags.Abstractions;

/// <summary>A DataStore that carries tags in <c>data.DataStoreTag</c>. A thing of this kind sits in a workspace, so an organization or workspace tag is checked against its place.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(TaggableKinds), "DataStore")]
public sealed class DataStoreTaggableKind : TaggableKindBase
{
    /// <summary>Initializes a new instance of the <see cref="DataStoreTaggableKind"/> class.</summary>
    public DataStoreTaggableKind()
        : base("DataStore", "data", "DataStoreTag", "DataStoreId", "DataStore")
    {
    }
}
