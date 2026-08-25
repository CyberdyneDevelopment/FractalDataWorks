using Fdw.Collections.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Web.Http.Abstractions.EndPoints;

/// <summary>
/// Read-only data retrieval operations endpoint type.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointTypes), "QueryEndpointBase", RestrictToCurrentCompilation = true)]
public sealed class QueryEndpointBase : EndpointTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QueryEndpointBase"/> class.
    /// </summary>
    public QueryEndpointBase() : base(
        id: 2,
        name: "QueryEndpointBase",
        description: "Read-only data retrieval and query operations",
        defaultHttpMethods: ["GET"],
        requiresAuthentication: false,
        cachingStrategy: "Cache",
        isReadOnly: true,
        supportsCaching: true,
        defaultCacheDurationSeconds: 300,
        requiresValidation: false,
        securityMethodName: "ApiKey",
        rateLimitPolicyName: "SlidingWindow",
        timeoutMs: 15000,
        maxBodySize: 1048576,
        allowedRoles: [])
    {
    }
}
