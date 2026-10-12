using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>The destination is truncated, then the rows are bulk inserted.</summary>
[TypeOption(typeof(LoadModes), "Replace")]
public sealed class ReplaceLoadMode : LoadModeBase
{
    /// <summary>Initializes a new instance of the <see cref="ReplaceLoadMode"/> class.</summary>
    public ReplaceLoadMode() : base("Replace", usesKeyColumns: false, usesCompareColumns: false, usesWriteColumns: false, allowsVersioning: false, allowsSoftDelete: false, truncatesFirst: true)
    {
    }
}
