using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// PostgreSQL native type <c>time</c> — maps to abstract type <see cref="DataTypes.Time"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(PostgreSqlNativeTypes), "time")]
public sealed class TimeType : PostgreSqlNativeTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="TimeType"/> class.</summary>
    public TimeType()
        : base(id: 10, name: "time")
    {
    }

    /// <inheritdoc/>
    public override IDataType AbstractType => DataTypes.Time;
}
