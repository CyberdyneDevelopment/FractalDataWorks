using System.Data;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>The SQL Int type.</summary>
[TypeOption(typeof(SqlTypeWords), "Int")]
[ExcludeFromCodeCoverage]
public sealed class IntSqlTypeWord : SqlTypeWordBase
{
    /// <summary>Initializes a new instance of the <see cref="IntSqlTypeWord"/> class.</summary>
    public IntSqlTypeWord()
        : base("Int", SqlDbType.Int, ["int", "int32"])
    {
    }
}
