using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>The output rows are matched on the key columns: new keys are inserted and changed rows updated.</summary>
[TypeOption(typeof(LoadStepKinds), "BulkUpsert")]
public sealed class BulkUpsertLoadStepKind : LoadStepKindBase
{
    /// <summary>Initializes a new instance of the <see cref="BulkUpsertLoadStepKind"/> class.</summary>
    public BulkUpsertLoadStepKind() : base("BulkUpsert", "The output rows are matched on the key columns: new keys are inserted and changed rows updated.", usesKeyColumns: true, usesCompareColumns: true, usesWriteColumns: true, allowsVersioning: true, allowsSoftDelete: true, usesSourceDataSet: false, usesDestinationContainer: false, usesRows: true, acceptsRejectedRecords: false)
    {
    }
}
