using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Results;

/// <summary>
/// Category 3 (30000–39999): not found / missing — a requested resource does not exist.
/// </summary>
/// <remarks>
/// Named "Missing" rather than "NotFound" on purpose: "NotFound" is the TypeCollection sentinel
/// name, so a real option of that name would be indistinguishable from a not-found lookup.
/// </remarks>
[TypeOption(typeof(ResultCategories), "Missing", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MissingCategory : ResultCategoryBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MissingCategory"/> class.
    /// </summary>
    public MissingCategory()
        : base(id: 3, name: "Missing", isFailure: true, isRetryable: false, httpStatus: 404, clientMessage: "The requested resource was not found", clientAction: null)
    {
    }
}
