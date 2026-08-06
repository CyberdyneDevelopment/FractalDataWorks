using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// PostgreSQL native type <c>uuid</c> — maps to abstract type <see cref="DataTypes.Guid"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(PostgreSqlNativeTypes), "uuid")]
public sealed class UuidType : PostgreSqlNativeTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="UuidType"/> class.</summary>
    public UuidType()
        : base(id: 16, name: "uuid")
    {
    }

    /// <inheritdoc/>
    public override IDataType AbstractType => DataTypes.Guid;
}
