using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Results;

/// <summary>
/// Category 1 (10000–19999): non-error outcomes — Success, Informational, and Cancelled.
/// </summary>
[TypeOption(typeof(ResultCategories), "Success", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SuccessCategory : ResultCategoryBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SuccessCategory"/> class.
    /// </summary>
    public SuccessCategory()
        : base(id: 1, name: "Success", isFailure: false, isRetryable: false, httpStatus: 200, clientMessage: "The request succeeded", clientAction: null)
    {
    }
}
