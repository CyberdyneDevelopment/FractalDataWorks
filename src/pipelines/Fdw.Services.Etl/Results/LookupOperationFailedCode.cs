using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Etl.Results;

/// <summary>
/// Lookup operation failed with exception.
/// </summary>
[TypeOption(typeof(EtlResultCodes), "LookupOperationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class LookupOperationFailedCode : EtlResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LookupOperationFailedCode"/> class.
    /// </summary>
    public LookupOperationFailedCode()
        : base(70001, "LookupOperationFailed",
            ResultSeverities.ByName("Error"),
            "Lookup operation failed: {Message}",
            isRetryable: false)
    {
    }
}