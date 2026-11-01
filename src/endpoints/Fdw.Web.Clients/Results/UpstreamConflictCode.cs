using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Web.Clients.Abstractions.Results;

/// <summary>The downstream service refused the request as conflicting with current state (409).</summary>
[TypeOption(typeof(ClientResultCodes), "UpstreamConflict", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class UpstreamConflictCode : ClientResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="UpstreamConflictCode"/> class.</summary>
    public UpstreamConflictCode()
        : base(40000, "UpstreamConflict", ResultSeverities.ByName("Error"),
            "{method} {uri} answered {statusCode}: {detail}", isRetryable: false)
    {
    }

    /// <inheritdoc/>
    public override string? FormatClientMessage(IResultDetails? details = null)
        => RelayedReason(details);
}
