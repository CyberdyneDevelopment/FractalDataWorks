using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>The output rows update the rows with matching keys.</summary>
[TypeOption(typeof(LoadStepKinds), "Update")]
public sealed class UpdateLoadStepKind : LoadStepKindBase
{
    /// <summary>Initializes a new instance of the <see cref="UpdateLoadStepKind"/> class.</summary>
    public UpdateLoadStepKind() : base("Update", "The output rows update the rows with matching keys.", usesKeyColumns: true, usesCompareColumns: false, usesWriteColumns: true, allowsVersioning: false, allowsSoftDelete: true, usesSourceDataSet: false, usesDestinationContainer: false, usesRows: true, acceptsRejectedRecords: false)
    {
    }
}
