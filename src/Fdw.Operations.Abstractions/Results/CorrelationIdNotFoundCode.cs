using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Results;

/// <summary>
/// No executions found for correlation ID.
/// </summary>
[TypeOption(typeof(OperationsResultCodes), "CorrelationIdNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CorrelationIdNotFoundCode : OperationsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CorrelationIdNotFoundCode"/> class.
    /// </summary>
    public CorrelationIdNotFoundCode()
        : base(
            30000,
            "CorrelationIdNotFound",
            ResultSeverities.ByName("Warning"),
            "No executions found for correlation ID '{CorrelationId}'",
            isRetryable: false)
    {
    }
}