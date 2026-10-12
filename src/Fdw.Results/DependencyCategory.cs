using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Results;

/// <summary>
/// Category 7 (70000–79999): dependency / connection / IO — failure talking to an external system.
/// Retryable.
/// </summary>
[TypeOption(typeof(ResultCategories), "Dependency", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DependencyCategory : ResultCategoryBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DependencyCategory"/> class.
    /// </summary>
    public DependencyCategory()
        : base(id: 7, name: "Dependency", isFailure: true, isRetryable: true, httpStatus: 502, clientMessage: "A required downstream service is unavailable", clientAction: "Retry in a few moments")
    {
    }
}
