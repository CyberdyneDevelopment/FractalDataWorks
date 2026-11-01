using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.MsSql.Results;

/// <summary>
/// General execution exception occurred.
/// </summary>
[TypeOption(typeof(MsSqlResultCodes), "ExecutionException", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ExecutionExceptionCode : MsSqlResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionExceptionCode"/> class.
    /// </summary>
    public ExecutionExceptionCode()
        : base(
            91000,
            "ExecutionException",
            ResultSeverities.ByName("Error"),
            "Execution exception on '{CommandText}': {ExceptionMessage}",
            isRetryable: false)
    {
    }
}
