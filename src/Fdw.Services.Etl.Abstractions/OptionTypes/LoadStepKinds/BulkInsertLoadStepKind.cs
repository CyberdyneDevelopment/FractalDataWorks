using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>The output rows are bulk inserted into the destination.</summary>
[TypeOption(typeof(LoadStepKinds), "BulkInsert")]
public sealed class BulkInsertLoadStepKind : LoadStepKindBase
{
    /// <summary>Initializes a new instance of the <see cref="BulkInsertLoadStepKind"/> class.</summary>
    public BulkInsertLoadStepKind() : base("BulkInsert", "The output rows are bulk inserted into the destination.", usesKeyColumns: false, usesCompareColumns: false, usesWriteColumns: true, allowsVersioning: false, allowsSoftDelete: false, usesSourceDataSet: false, usesDestinationContainer: false, usesRows: true, acceptsRejectedRecords: true)
    {
    }
}
