using System.Data;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>The SQL Decimal type.</summary>
[TypeOption(typeof(SqlTypeWords), "Decimal")]
[ExcludeFromCodeCoverage]
public sealed class DecimalSqlTypeWord : SqlTypeWordBase
{
    /// <summary>Initializes a new instance of the <see cref="DecimalSqlTypeWord"/> class.</summary>
    public DecimalSqlTypeWord()
        : base("Decimal", SqlDbType.Decimal, ["decimal", "money"])
    {
    }
}
