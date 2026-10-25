using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>The source column that feeds the target column when the names differ.</summary>
[TypeOption(typeof(ValueSourceKinds), "Column")]
public sealed class ColumnValueSourceKind : ValueSourceKindBase
{
    /// <summary>Initializes a new instance of the <see cref="ColumnValueSourceKind"/> class.</summary>
    public ColumnValueSourceKind() : base("Column", mapsSourceColumn: true, requiresSourceField: true, requiresSequenceName: false, requiresSequenceOrder: false, requiresConstant: false)
    {
    }
}
