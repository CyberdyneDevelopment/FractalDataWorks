using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// PostgreSQL native type <c>date</c> — maps to abstract type <see cref="DataTypes.Date"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(PostgreSqlNativeTypes), "date")]
public sealed class DateType : PostgreSqlNativeTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="DateType"/> class.</summary>
    public DateType()
        : base(id: 9, name: "date")
    {
    }

    /// <inheritdoc/>
    public override IDataType AbstractType => DataTypes.Date;
}
