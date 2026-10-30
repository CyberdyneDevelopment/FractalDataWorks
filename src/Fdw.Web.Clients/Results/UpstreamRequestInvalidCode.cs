using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Web.Clients.Abstractions.Results;

/// <summary>The downstream service refused the request as invalid (400).</summary>
[TypeOption(typeof(ClientResultCodes), "UpstreamRequestInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class UpstreamRequestInvalidCode : ClientResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="UpstreamRequestInvalidCode"/> class.</summary>
    public UpstreamRequestInvalidCode()
        : base(20000, "UpstreamRequestInvalid", ResultSeverities.ByName("Error"),
            "{method} {uri} answered {statusCode}: {detail}", isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the upstream refused the request, and nothing else the message holds.</summary>
    /// <param name="details">The details the client captured from the upstream's answer.</param>
    /// <returns>The reason the upstream's problem body gave, or null when it gave none.</returns>
    /// <remarks>
    /// The message names the upstream's address, which is not for a caller. The reason is captured only from a problem
    /// body's <c>detail</c> (ApiClientBase), and the upstream is an FDW host that shows a reason only for a validation
    /// refusal that opted in, so what is shown here is what that host chose to show.
    /// </remarks>
    public override string? FormatClientMessage(IResultDetails? details = null)
        => RelayedReason(details);
}
