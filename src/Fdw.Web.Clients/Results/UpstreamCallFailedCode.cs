using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Web.Clients.Abstractions.Results;

/// <summary>
/// The downstream service answered with a status this client does not map to a specific band
/// (a 5xx it does not distinguish, or any other unrecognized status). The fallback everything else
/// used to be, unconditionally.
/// </summary>
[TypeOption(typeof(ClientResultCodes), "UpstreamCallFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class UpstreamCallFailedCode : ClientResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="UpstreamCallFailedCode"/> class.</summary>
    public UpstreamCallFailedCode()
        : base(70000, "UpstreamCallFailed", ResultSeverities.ByName("Error"),
            "{method} {uri} answered {statusCode}: {detail}", isRetryable: true)
    {
    }
}
