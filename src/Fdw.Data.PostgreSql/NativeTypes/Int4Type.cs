using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// PostgreSQL native type <c>int4</c> — maps to abstract type <see cref="DataTypes.Int32"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(PostgreSqlNativeTypes), "int4")]
public sealed class Int4Type : PostgreSqlNativeTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="Int4Type"/> class.</summary>
    public Int4Type()
        : base(id: 2, name: "int4")
    {
    }

    /// <inheritdoc/>
    public override IDataType AbstractType => DataTypes.Int32;
}
