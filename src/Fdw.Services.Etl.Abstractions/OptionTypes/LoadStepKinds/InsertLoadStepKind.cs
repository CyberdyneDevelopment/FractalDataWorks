using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>The output rows are inserted into the destination.</summary>
[TypeOption(typeof(LoadStepKinds), "Insert")]
public sealed class InsertLoadStepKind : LoadStepKindBase
{
    /// <summary>Initializes a new instance of the <see cref="InsertLoadStepKind"/> class.</summary>
    public InsertLoadStepKind() : base("Insert", "The output rows are inserted into the destination.", usesKeyColumns: false, usesCompareColumns: false, usesWriteColumns: true, allowsVersioning: false, allowsSoftDelete: false, usesSourceDataSet: false, usesRows: true, acceptsRejectedRecords: true)
    {
    }
}
