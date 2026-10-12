using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Web.Clients.Abstractions.Results;

/// <summary>The downstream service refused the request as unauthenticated (401).</summary>
[TypeOption(typeof(ClientResultCodes), "UpstreamAuthenticationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class UpstreamAuthenticationFailedCode : ClientResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="UpstreamAuthenticationFailedCode"/> class.</summary>
    public UpstreamAuthenticationFailedCode()
        : base(50000, "UpstreamAuthenticationFailed", ResultSeverities.ByName("Error"),
            "{method} {uri} answered {statusCode}: {detail}", isRetryable: false)
    {
    }
}
