using System.Data;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>The SQL Date type.</summary>
[TypeOption(typeof(SqlTypeWords), "Date")]
[ExcludeFromCodeCoverage]
public sealed class DateSqlTypeWord : SqlTypeWordBase
{
    /// <summary>Initializes a new instance of the <see cref="DateSqlTypeWord"/> class.</summary>
    public DateSqlTypeWord()
        : base("Date", SqlDbType.Date, ["date"])
    {
    }
}
