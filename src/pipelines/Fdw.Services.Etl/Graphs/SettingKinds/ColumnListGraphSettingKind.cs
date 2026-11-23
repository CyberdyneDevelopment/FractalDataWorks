using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Graphs;

/// <summary>A setting that holds a list (ColumnList).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(GraphSettingKinds), "ColumnList")]
public sealed class ColumnListGraphSettingKind : GraphSettingKindBase
{
    /// <summary>Initializes a new instance of the <see cref="ColumnListGraphSettingKind"/> class.</summary>
    public ColumnListGraphSettingKind()
        : base("ColumnList", "String", string.Empty)
    {
    }
}
