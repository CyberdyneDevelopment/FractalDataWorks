using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A container states its own paging, and a data store of its type does not page its containers.
/// </summary>
/// <remarks>
/// Refused when the container is saved, so paging is never stored where nothing will read it. The message is written for the caller: it is shown to them
/// (HTTP 400). It holds no backslash, which the message generator does not escape.
/// </remarks>
[TypeOption(typeof(DataServiceResultCodes), "ContainerPagingNotForStore", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ContainerPagingNotForStoreCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="ContainerPagingNotForStoreCode"/> class.</summary>
    public ContainerPagingNotForStoreCode()
        : base(21080, "ContainerPagingNotForStore",
            ResultSeverities.ByName("Error"),
            "Container '{container}' states paging, and a data store of type '{storeType}' does not page its containers; only the containers of an HTTP data store do",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
