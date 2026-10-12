using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>A named executable container of the destination data store is run with parameters.</summary>
[TypeOption(typeof(LoadStepKinds), "Execute")]
public sealed class ExecuteLoadStepKind : LoadStepKindBase
{
    /// <summary>Initializes a new instance of the <see cref="ExecuteLoadStepKind"/> class.</summary>
    public ExecuteLoadStepKind() : base("Execute", "A named executable container of the destination data store is run with parameters.", usesKeyColumns: false, usesCompareColumns: false, usesWriteColumns: false, allowsVersioning: false, allowsSoftDelete: false, usesSourceDataSet: false, usesDestinationContainer: true, usesRows: false, acceptsRejectedRecords: false)
    {
    }
}
