using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>Rows are matched on the key columns: new keys are inserted and changed rows are updated.</summary>
[TypeOption(typeof(LoadModes), "Upsert")]
public sealed class UpsertLoadMode : LoadModeBase
{
    /// <summary>Initializes a new instance of the <see cref="UpsertLoadMode"/> class.</summary>
    public UpsertLoadMode() : base("Upsert", usesKeyColumns: true, usesCompareColumns: true, usesWriteColumns: true, allowsVersioning: true, allowsSoftDelete: true, truncatesFirst: false)
    {
    }
}
