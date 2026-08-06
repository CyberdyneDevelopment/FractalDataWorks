using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// PostgreSQL native type <c>timestamp</c> — maps to abstract type <see cref="DataTypes.DateTime"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(PostgreSqlNativeTypes), "timestamp")]
public sealed class TimestampType : PostgreSqlNativeTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="TimestampType"/> class.</summary>
    public TimestampType()
        : base(id: 7, name: "timestamp")
    {
    }

    /// <inheritdoc/>
    public override IDataType AbstractType => DataTypes.DateTime;
}
