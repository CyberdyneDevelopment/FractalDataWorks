using System.Data;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>The SQL SmallInt type.</summary>
[TypeOption(typeof(SqlTypeWords), "SmallInt")]
[ExcludeFromCodeCoverage]
public sealed class SmallIntSqlTypeWord : SqlTypeWordBase
{
    /// <summary>Initializes a new instance of the <see cref="SmallIntSqlTypeWord"/> class.</summary>
    public SmallIntSqlTypeWord()
        : base("SmallInt", SqlDbType.SmallInt, ["short", "int16"])
    {
    }
}
