using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.MsSql.Results;

/// <summary>
/// Bulk copy operation failed.
/// </summary>
[TypeOption(typeof(MsSqlResultCodes), "BulkCopyFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class BulkCopyFailedCode : MsSqlResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BulkCopyFailedCode"/> class.
    /// </summary>
    public BulkCopyFailedCode()
        : base(
            70001,
            "BulkCopyFailed",
            ResultSeverities.ByName("Error"),
            "Bulk copy failed: {ErrorMessage}",
            isRetryable: true)
    {
    }
}
