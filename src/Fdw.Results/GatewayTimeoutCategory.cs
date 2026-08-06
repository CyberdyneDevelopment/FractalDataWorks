using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Results;

/// <summary>
/// Category 11 (110000–119999): a downstream dependency did not respond in time.
/// Distinct from <see cref="TransientCategory"/> (503, temporarily unavailable) — this is 504 Gateway Timeout.
/// </summary>
[TypeOption(typeof(ResultCategories), "GatewayTimeout", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GatewayTimeoutCategory : ResultCategoryBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GatewayTimeoutCategory"/> class.
    /// </summary>
    public GatewayTimeoutCategory()
        : base(id: 11, name: "GatewayTimeout", isFailure: true, isRetryable: true, httpStatus: 504, clientMessage: "The request timed out waiting on a downstream service", clientAction: "Retry in a few moments")
    {
    }
}
