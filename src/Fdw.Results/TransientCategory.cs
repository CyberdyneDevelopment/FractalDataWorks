using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Results;

/// <summary>
/// Category 8 (80000–89999): timeout / transient — timed out, throttled, or otherwise retryable.
/// </summary>
[TypeOption(typeof(ResultCategories), "Transient", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TransientCategory : ResultCategoryBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TransientCategory"/> class.
    /// </summary>
    public TransientCategory()
        : base(id: 8, name: "Transient", isFailure: true, isRetryable: true, httpStatus: 503, clientMessage: "The service is temporarily unavailable", clientAction: "Retry in a few moments")
    {
    }
}
