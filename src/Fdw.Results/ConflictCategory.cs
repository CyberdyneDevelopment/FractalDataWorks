using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Results;

/// <summary>
/// Category 4 (40000–49999): conflict / state — already-exists, wrong state, or version conflict.
/// </summary>
[TypeOption(typeof(ResultCategories), "Conflict", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ConflictCategory : ResultCategoryBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConflictCategory"/> class.
    /// </summary>
    public ConflictCategory()
        : base(id: 4, name: "Conflict", isFailure: true, isRetryable: false, httpStatus: 409, clientMessage: "The request conflicted with the current state of the resource", clientAction: "Refresh and try again")
    {
    }
}
