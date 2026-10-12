using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Web.Clients.Abstractions.Results;

/// <summary>
/// The downstream service answered with an internal error (500). Its number sits in the Internal band, so a host
/// that relays this failure answers 500 as the downstream did, not a 502 that names the proxy as the one that failed.
/// </summary>
[TypeOption(typeof(ClientResultCodes), "UpstreamInternalError", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class UpstreamInternalErrorCode : ClientResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="UpstreamInternalErrorCode"/> class.</summary>
    public UpstreamInternalErrorCode()
        : base(90000, "UpstreamInternalError", ResultSeverities.ByName("Error"),
            "{method} {uri} answered {statusCode}: {detail}", isRetryable: false)
    {
    }
}
