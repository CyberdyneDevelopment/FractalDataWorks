using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Web.Clients.Abstractions.Results;

/// <summary>The downstream service found no resource at the requested path (404).</summary>
[TypeOption(typeof(ClientResultCodes), "UpstreamResourceNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class UpstreamResourceNotFoundCode : ClientResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="UpstreamResourceNotFoundCode"/> class.</summary>
    public UpstreamResourceNotFoundCode()
        : base(30000, "UpstreamResourceNotFound", ResultSeverities.ByName("Error"),
            "{method} {uri} answered {statusCode}: {detail}", isRetryable: false)
    {
    }
}
