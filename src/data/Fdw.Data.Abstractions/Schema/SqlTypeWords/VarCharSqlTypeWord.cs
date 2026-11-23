using System.Data;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>The SQL VarChar type.</summary>
[TypeOption(typeof(SqlTypeWords), "VarChar")]
[ExcludeFromCodeCoverage]
public sealed class VarCharSqlTypeWord : SqlTypeWordBase
{
    /// <summary>Initializes a new instance of the <see cref="VarCharSqlTypeWord"/> class.</summary>
    public VarCharSqlTypeWord()
        : base("VarChar", SqlDbType.VarChar, ["string", "varchar"])
    {
    }
}
