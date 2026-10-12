using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Sqlite.Results;

/// <summary>
/// SQLite query execution failed.
/// </summary>
[TypeOption(typeof(SqliteDataResultCodes), "ExecutionFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ExecutionFailedCode : SqliteDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionFailedCode"/> class.
    /// </summary>
    public ExecutionFailedCode()
        : base(21017, "ExecutionFailed",
            ResultSeverities.ByName("Error"),
            "SQLite execution failed",
            isRetryable: true)
    {
    }
}
