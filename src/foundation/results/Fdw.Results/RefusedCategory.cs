using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Results;

/// <summary>
/// Category 12 (120000–129999): a request refused because of the state of something it uses, such as a connection that is disabled (FDW-4005). It is handled as a conflict is
/// (409, not retryable) but differs in the one outcome that made it a category: it does not count toward a failure threshold, since the fault is not the fault of what was refused.
/// A conflict raised during a run (an invalid state, a version conflict, a duplicate) stays in category 4 and stays counted.
/// </summary>
[TypeOption(typeof(ResultCategories), "Refused", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class RefusedCategory : ResultCategoryBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RefusedCategory"/> class.
    /// </summary>
    public RefusedCategory()
        : base(id: 12, name: "Refused", isFailure: true, isRetryable: false, countsTowardFailureThresholds: false, httpStatus: 409, clientMessage: "The request was refused because of the state of something it uses", clientAction: "Enable it, or wait until it is available")
    {
    }
}
