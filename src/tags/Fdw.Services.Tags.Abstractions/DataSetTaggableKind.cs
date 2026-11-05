using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Tags.Abstractions;

/// <summary>A DataSet that carries tags in <c>data.DataSetTag</c>. A thing of this kind sits in a workspace, so an organization or workspace tag is checked against its place.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(TaggableKinds), "DataSet")]
public sealed class DataSetTaggableKind : TaggableKindBase
{
    /// <summary>Initializes a new instance of the <see cref="DataSetTaggableKind"/> class.</summary>
    public DataSetTaggableKind()
        : base("DataSet", "data", "DataSetTag", "DataSetId", "DataSet")
    {
    }
}
