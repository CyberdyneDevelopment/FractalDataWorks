using System.Data;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>The SQL DateTimeOffset type.</summary>
[TypeOption(typeof(SqlTypeWords), "DateTimeOffset")]
[ExcludeFromCodeCoverage]
public sealed class DateTimeOffsetSqlTypeWord : SqlTypeWordBase
{
    /// <summary>Initializes a new instance of the <see cref="DateTimeOffsetSqlTypeWord"/> class.</summary>
    public DateTimeOffsetSqlTypeWord()
        : base("DateTimeOffset", SqlDbType.DateTimeOffset, ["datetimeoffset"])
    {
    }
}
