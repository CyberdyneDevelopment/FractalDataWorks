using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Graphs;

/// <summary>A setting that holds a list (FieldMapPairs).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(GraphSettingKinds), "FieldMapPairs")]
public sealed class FieldMapPairsGraphSettingKind : GraphSettingKindBase
{
    /// <summary>Initializes a new instance of the <see cref="FieldMapPairsGraphSettingKind"/> class.</summary>
    public FieldMapPairsGraphSettingKind()
        : base("FieldMapPairs", "String", string.Empty)
    {
    }
}
