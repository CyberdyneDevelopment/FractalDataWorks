using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Web.Clients.Abstractions.Results;

/// <summary>The downstream service refused the request as forbidden (403).</summary>
[TypeOption(typeof(ClientResultCodes), "UpstreamForbidden", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class UpstreamForbiddenCode : ClientResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="UpstreamForbiddenCode"/> class.</summary>
    public UpstreamForbiddenCode()
        : base(100000, "UpstreamForbidden", ResultSeverities.ByName("Error"),
            "{method} {uri} answered {statusCode}: {detail}", isRetryable: false)
    {
    }
}
