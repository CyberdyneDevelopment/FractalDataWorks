using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.MsSql.Results;

/// <summary>
/// SQL execution failed due to a SqlException.
/// </summary>
[TypeOption(typeof(MsSqlResultCodes), "SqlExecutionFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SqlExecutionFailedCode : MsSqlResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqlExecutionFailedCode"/> class.
    /// </summary>
    public SqlExecutionFailedCode()
        : base(
            90001,
            "SqlExecutionFailed",
            ResultSeverities.ByName("Error"),
            "SQL execution failed on '{CommandText}': {ErrorMessage} (Error {ErrorNumber})",
            isRetryable: false)
    {
    }
}
