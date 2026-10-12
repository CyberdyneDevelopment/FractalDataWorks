using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Web.Clients.Abstractions.Results;

/// <summary>The downstream service answered that it is temporarily unavailable (503).</summary>
[TypeOption(typeof(ClientResultCodes), "UpstreamUnavailable", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class UpstreamUnavailableCode : ClientResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="UpstreamUnavailableCode"/> class.</summary>
    public UpstreamUnavailableCode()
        : base(80000, "UpstreamUnavailable", ResultSeverities.ByName("Error"),
            "{method} {uri} answered {statusCode}: {detail}", isRetryable: true)
    {
    }
}
