using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// PostgreSQL native type <c>timestamptz</c> — maps to abstract type <see cref="DataTypes.DateTimeOffset"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(PostgreSqlNativeTypes), "timestamptz")]
public sealed class TimestampTzType : PostgreSqlNativeTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="TimestampTzType"/> class.</summary>
    public TimestampTzType()
        : base(id: 8, name: "timestamptz")
    {
    }

    /// <inheritdoc/>
    public override IDataType AbstractType => DataTypes.DateTimeOffset;
}
