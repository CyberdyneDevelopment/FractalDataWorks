using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>The output rows are matched on the key columns: new keys are inserted and changed rows updated.</summary>
[TypeOption(typeof(LoadStepKinds), "Upsert")]
public sealed class UpsertLoadStepKind : LoadStepKindBase
{
    /// <summary>Initializes a new instance of the <see cref="UpsertLoadStepKind"/> class.</summary>
    public UpsertLoadStepKind() : base("Upsert", "The output rows are matched on the key columns: new keys are inserted and changed rows updated.", usesKeyColumns: true, usesCompareColumns: true, usesWriteColumns: true, allowsVersioning: true, allowsSoftDelete: true, usesSourceDataSet: false, usesRows: true, acceptsRejectedRecords: false)
    {
    }
}
