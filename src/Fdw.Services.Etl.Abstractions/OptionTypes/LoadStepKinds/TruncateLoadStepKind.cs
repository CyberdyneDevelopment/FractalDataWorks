using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>The destination is emptied.</summary>
[TypeOption(typeof(LoadStepKinds), "Truncate")]
public sealed class TruncateLoadStepKind : LoadStepKindBase
{
    /// <summary>Initializes a new instance of the <see cref="TruncateLoadStepKind"/> class.</summary>
    public TruncateLoadStepKind() : base("Truncate", "The destination is emptied.", usesKeyColumns: false, usesCompareColumns: false, usesWriteColumns: false, allowsVersioning: false, allowsSoftDelete: false, usesSourceDataSet: false, usesDestinationContainer: false, usesRows: false, acceptsRejectedRecords: false)
    {
    }
}
