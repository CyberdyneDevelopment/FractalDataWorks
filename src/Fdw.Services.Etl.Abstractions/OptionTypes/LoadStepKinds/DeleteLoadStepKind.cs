using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>The rows with the keys of the output rows are removed, or flagged deleted.</summary>
[TypeOption(typeof(LoadStepKinds), "Delete")]
public sealed class DeleteLoadStepKind : LoadStepKindBase
{
    /// <summary>Initializes a new instance of the <see cref="DeleteLoadStepKind"/> class.</summary>
    public DeleteLoadStepKind() : base("Delete", "The rows with the keys of the output rows are removed, or flagged deleted.", usesKeyColumns: true, usesCompareColumns: false, usesWriteColumns: false, allowsVersioning: false, allowsSoftDelete: true, usesSourceDataSet: false, usesDestinationContainer: false, usesRows: true, acceptsRejectedRecords: false)
    {
    }
}
