using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Results;

/// <summary>
/// Category 10 (100000–109999): the caller is authenticated but not permitted to perform the action.
/// Distinct from <see cref="AuthCategory"/> (401, not authenticated) — this is 403 Forbidden.
/// </summary>
[TypeOption(typeof(ResultCategories), "Forbidden", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ForbiddenCategory : ResultCategoryBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ForbiddenCategory"/> class.
    /// </summary>
    public ForbiddenCategory()
        : base(id: 10, name: "Forbidden", isFailure: true, isRetryable: false, httpStatus: 403, clientMessage: "You don't have permission to perform this action", clientAction: "Request access")
    {
    }
}
