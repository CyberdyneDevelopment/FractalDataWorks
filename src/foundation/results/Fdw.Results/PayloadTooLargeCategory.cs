using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Results;

/// <summary>
/// Category 13 (130000–139999): a request whose body is larger than the endpoint accepts (FDW-2335). It answers 413 and is not retryable as sent. It is the caller's request that is too
/// large, so it is handled as a validation failure is, with its own status, and counts toward a failure threshold as validation does.
/// </summary>
[TypeOption(typeof(ResultCategories), "PayloadTooLarge", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PayloadTooLargeCategory : ResultCategoryBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PayloadTooLargeCategory"/> class.
    /// </summary>
    public PayloadTooLargeCategory()
        : base(id: 13, name: "PayloadTooLarge", isFailure: true, isRetryable: false, countsTowardFailureThresholds: true, httpStatus: 413, clientMessage: "The request body is too large", clientAction: "Send a smaller body")
    {
    }
}
