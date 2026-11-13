using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A request to set a container's own paging states no paging style.
/// </summary>
/// <remarks>
/// A container's own paging is the style it names and what it changes within it. Returning it to its connection's paging is a delete of its own paging, not an empty set, so a request with no style is refused rather than read as one. The
/// message is written for the caller: it is shown to them (HTTP 400). It holds no backslash, which the message generator does not escape.
/// </remarks>
[TypeOption(typeof(DataServiceResultCodes), "ContainerPagingNothingStated", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ContainerPagingNothingStatedCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="ContainerPagingNothingStatedCode"/> class.</summary>
    public ContainerPagingNothingStatedCode()
        : base(21081, "ContainerPagingNothingStated",
            ResultSeverities.ByName("Error"),
            "The request states no paging style for container '{container}'; name the style the container pages in (the connection's own is allowed), or delete the container's own paging to return it to its connection's",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
