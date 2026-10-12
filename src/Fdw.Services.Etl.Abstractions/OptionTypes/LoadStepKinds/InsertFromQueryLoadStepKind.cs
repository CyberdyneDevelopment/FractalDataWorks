using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>Rows are copied set-based from a source data set on the same connection.</summary>
[TypeOption(typeof(LoadStepKinds), "InsertFromQuery")]
public sealed class InsertFromQueryLoadStepKind : LoadStepKindBase
{
    /// <summary>Initializes a new instance of the <see cref="InsertFromQueryLoadStepKind"/> class.</summary>
    public InsertFromQueryLoadStepKind() : base("InsertFromQuery", "Rows are copied set-based from a source data set on the same connection.", usesKeyColumns: false, usesCompareColumns: false, usesWriteColumns: true, allowsVersioning: false, allowsSoftDelete: false, usesSourceDataSet: true, usesDestinationContainer: false, usesRows: false, acceptsRejectedRecords: false)
    {
    }
}
