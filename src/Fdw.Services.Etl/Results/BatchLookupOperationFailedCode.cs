using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Etl.Results;

/// <summary>
/// Batch lookup operation failed with exception.
/// </summary>
[TypeOption(typeof(EtlResultCodes), "BatchLookupOperationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class BatchLookupOperationFailedCode : EtlResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BatchLookupOperationFailedCode"/> class.
    /// </summary>
    public BatchLookupOperationFailedCode()
        : base(91000, "BatchLookupOperationFailed",
            ResultSeverities.ByName("Error"),
            "Batch lookup operation failed: {Message}",
            isRetryable: false)
    {
    }
}