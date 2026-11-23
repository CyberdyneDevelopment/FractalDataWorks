using System.Data;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>The SQL TinyInt type.</summary>
[TypeOption(typeof(SqlTypeWords), "TinyInt")]
[ExcludeFromCodeCoverage]
public sealed class TinyIntSqlTypeWord : SqlTypeWordBase
{
    /// <summary>Initializes a new instance of the <see cref="TinyIntSqlTypeWord"/> class.</summary>
    public TinyIntSqlTypeWord()
        : base("TinyInt", SqlDbType.TinyInt, ["byte"])
    {
    }
}
