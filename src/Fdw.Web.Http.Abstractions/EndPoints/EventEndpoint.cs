using Fdw.Collections.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Web.Http.Abstractions.EndPoints;

/// <summary>
/// Event publishing and subscription operations endpoint type.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointTypes), "EventEndpoint", RestrictToCurrentCompilation = true)]
public sealed class EventEndpoint : EndpointTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventEndpoint"/> class.
    /// </summary>
    public EventEndpoint() : base(
        id: 4,
        name: "EventEndpoint",
        description: "Event publishing and subscription operations",
        defaultHttpMethods: ["POST", "GET"],
        requiresAuthentication: true,
        cachingStrategy: "NoCache",
        isReadOnly: false,
        supportsCaching: false,
        defaultCacheDurationSeconds: null,
        requiresValidation: true,
        securityMethodName: "ApiKey",
        rateLimitPolicyName: "TokenBucket",
        timeoutMs: 30000,
        maxBodySize: 10485760,
        allowedRoles: ["System", "Admin"])
    {
    }
}
