using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Graphs;

/// <summary>A setting that holds a list (Distinct).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(GraphSettingKinds), "Distinct")]
public sealed class DistinctGraphSettingKind : GraphSettingKindBase
{
    /// <summary>Initializes a new instance of the <see cref="DistinctGraphSettingKind"/> class.</summary>
    public DistinctGraphSettingKind()
        : base("Distinct", "String", string.Empty)
    {
    }
}
