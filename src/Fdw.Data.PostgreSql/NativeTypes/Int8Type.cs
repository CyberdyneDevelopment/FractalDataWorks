using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// PostgreSQL native type <c>int8</c> — maps to abstract type <see cref="DataTypes.Int64"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(PostgreSqlNativeTypes), "int8")]
public sealed class Int8Type : PostgreSqlNativeTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="Int8Type"/> class.</summary>
    public Int8Type()
        : base(id: 1, name: "int8")
    {
    }

    /// <inheritdoc/>
    public override IDataType AbstractType => DataTypes.Int64;
}
