using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Sqlite.Results;

/// <summary>
/// Primary key value is null — cannot build WHERE clause for UPDATE.
/// </summary>
[TypeOption(typeof(SqliteDataResultCodes), "NullPrimaryKeyValue", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NullPrimaryKeyValueCode : SqliteDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NullPrimaryKeyValueCode"/> class.
    /// </summary>
    public NullPrimaryKeyValueCode()
        : base(21007, "NullPrimaryKeyValue",
            ResultSeverities.ByName("Error"),
            "Primary key value is null — cannot build WHERE clause for UPDATE",
            isRetryable: false)
    {
    }
}
