using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// PostgreSQL native type <c>int2</c> — maps to abstract type <see cref="DataTypes.Int16"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(PostgreSqlNativeTypes), "int2")]
public sealed class Int2Type : PostgreSqlNativeTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="Int2Type"/> class.</summary>
    public Int2Type()
        : base(id: 3, name: "int2")
    {
    }

    /// <inheritdoc/>
    public override IDataType AbstractType => DataTypes.Int16;
}
