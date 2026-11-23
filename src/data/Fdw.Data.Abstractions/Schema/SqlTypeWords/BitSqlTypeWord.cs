using System.Data;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>The SQL Bit type.</summary>
[TypeOption(typeof(SqlTypeWords), "Bit")]
[ExcludeFromCodeCoverage]
public sealed class BitSqlTypeWord : SqlTypeWordBase
{
    /// <summary>Initializes a new instance of the <see cref="BitSqlTypeWord"/> class.</summary>
    public BitSqlTypeWord()
        : base("Bit", SqlDbType.Bit, ["bool", "boolean"])
    {
    }
}
