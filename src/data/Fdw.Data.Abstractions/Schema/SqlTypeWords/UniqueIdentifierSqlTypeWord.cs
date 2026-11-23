using System.Data;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>The SQL UniqueIdentifier type.</summary>
[TypeOption(typeof(SqlTypeWords), "UniqueIdentifier")]
[ExcludeFromCodeCoverage]
public sealed class UniqueIdentifierSqlTypeWord : SqlTypeWordBase
{
    /// <summary>Initializes a new instance of the <see cref="UniqueIdentifierSqlTypeWord"/> class.</summary>
    public UniqueIdentifierSqlTypeWord()
        : base("UniqueIdentifier", SqlDbType.UniqueIdentifier, ["guid", "uniqueidentifier"])
    {
    }
}
