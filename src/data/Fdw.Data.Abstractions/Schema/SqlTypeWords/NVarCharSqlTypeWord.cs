using System.Data;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>The SQL NVarChar type.</summary>
[TypeOption(typeof(SqlTypeWords), "NVarChar")]
[ExcludeFromCodeCoverage]
public sealed class NVarCharSqlTypeWord : SqlTypeWordBase
{
    /// <summary>Initializes a new instance of the <see cref="NVarCharSqlTypeWord"/> class.</summary>
    public NVarCharSqlTypeWord()
        : base("NVarChar", SqlDbType.NVarChar, ["nvarchar", "text"])
    {
    }
}
