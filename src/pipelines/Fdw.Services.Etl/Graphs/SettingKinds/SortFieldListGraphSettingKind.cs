using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Graphs;

/// <summary>A setting that holds a list (SortFieldList).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(GraphSettingKinds), "SortFieldList")]
public sealed class SortFieldListGraphSettingKind : GraphSettingKindBase
{
    /// <summary>Initializes a new instance of the <see cref="SortFieldListGraphSettingKind"/> class.</summary>
    public SortFieldListGraphSettingKind()
        : base("SortFieldList", "String", string.Empty)
    {
    }
}
