using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Abstractions;

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

    /// <inheritdoc />
    public override Citation Cites => Citation.Uncited("no check yet: the data store has no foreign key to its Id and its provider is not registered as a cited domain; FDW-6184");
}
