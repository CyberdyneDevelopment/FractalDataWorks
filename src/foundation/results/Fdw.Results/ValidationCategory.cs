using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Results;

/// <summary>
/// Category 2 (20000–29999): validation / bad input — caller supplied missing or invalid input.
/// </summary>
[TypeOption(typeof(ResultCategories), "Validation", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ValidationCategory : ResultCategoryBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationCategory"/> class.
    /// </summary>
    public ValidationCategory()
        : base(id: 2, name: "Validation", isFailure: true, isRetryable: false, httpStatus: 400, clientMessage: "The request was invalid", clientAction: "Check your input and try again")
    {
    }
}
