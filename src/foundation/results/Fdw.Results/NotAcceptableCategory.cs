using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Results;

/// <summary>
/// Category 14 (140000–149999): the request asks for a representation (a record format, by ?format= or an Accept header) that no writer serves (FDW-2335). It answers 406 and is not retryable as sent.
/// </summary>
[TypeOption(typeof(ResultCategories), "NotAcceptable", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NotAcceptableCategory : ResultCategoryBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotAcceptableCategory"/> class.
    /// </summary>
    public NotAcceptableCategory()
        : base(id: 14, name: "NotAcceptable", isFailure: true, isRetryable: false, countsTowardFailureThresholds: true, httpStatus: 406, clientMessage: "No available representation satisfies the request", clientAction: "Ask for one of the available formats")
    {
    }
}
