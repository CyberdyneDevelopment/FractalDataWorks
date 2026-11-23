using System.Data;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>The SQL BigInt type.</summary>
[TypeOption(typeof(SqlTypeWords), "BigInt")]
[ExcludeFromCodeCoverage]
public sealed class BigIntSqlTypeWord : SqlTypeWordBase
{
    /// <summary>Initializes a new instance of the <see cref="BigIntSqlTypeWord"/> class.</summary>
    public BigIntSqlTypeWord()
        : base("BigInt", SqlDbType.BigInt, ["long", "int64"])
    {
    }
}
