using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Results;

/// <summary>
/// Category 15 (150000–159999): the request was refused because the endpoint is already serving as many requests of its kind as it allows (FDW-2335). It answers 429 and is retryable; nothing failed, so it does not count toward a failure threshold.
/// </summary>
[TypeOption(typeof(ResultCategories), "TooManyRequests", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TooManyRequestsCategory : ResultCategoryBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TooManyRequestsCategory"/> class.
    /// </summary>
    public TooManyRequestsCategory()
        : base(id: 15, name: "TooManyRequests", isFailure: true, isRetryable: true, countsTowardFailureThresholds: false, httpStatus: 429, clientMessage: "The endpoint is busy; try again shortly", clientAction: "Wait, then send the request again")
    {
    }
}
