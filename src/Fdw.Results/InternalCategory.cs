using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Results;

/// <summary>
/// Category 9 (90000–99999): internal / unexpected — unexpected faults, not-implemented,
/// and conversion/parse/translation failures.
/// </summary>
[TypeOption(typeof(ResultCategories), "Internal", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InternalCategory : ResultCategoryBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InternalCategory"/> class.
    /// </summary>
    public InternalCategory()
        : base(id: 9, name: "Internal", isFailure: true, isRetryable: false, httpStatus: 500, clientMessage: "An unexpected error occurred", clientAction: "Contact your administrator")
    {
    }
}
