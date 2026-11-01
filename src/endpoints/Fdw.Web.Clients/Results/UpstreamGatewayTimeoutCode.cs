using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Web.Clients.Abstractions.Results;

/// <summary>The downstream service answered that a further downstream call timed out (504).</summary>
[TypeOption(typeof(ClientResultCodes), "UpstreamGatewayTimeout", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class UpstreamGatewayTimeoutCode : ClientResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="UpstreamGatewayTimeoutCode"/> class.</summary>
    public UpstreamGatewayTimeoutCode()
        : base(110000, "UpstreamGatewayTimeout", ResultSeverities.ByName("Error"),
            "{method} {uri} answered {statusCode}: {detail}", isRetryable: true)
    {
    }
}
