using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// PostgreSQL native type <c>bytea</c> — maps to abstract type <see cref="DataTypes.Binary"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(PostgreSqlNativeTypes), "bytea")]
public sealed class ByteaType : PostgreSqlNativeTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="ByteaType"/> class.</summary>
    public ByteaType()
        : base(id: 15, name: "bytea")
    {
    }

    /// <inheritdoc/>
    public override IDataType AbstractType => DataTypes.Binary;
}
