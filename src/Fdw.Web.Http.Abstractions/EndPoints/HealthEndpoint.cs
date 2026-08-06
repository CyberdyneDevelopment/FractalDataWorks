using Fdw.Collections.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Web.Http.Abstractions.EndPoints;

/// <summary>
/// System health and monitoring endpoint type.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointTypes), "HealthEndpoint", RestrictToCurrentCompilation = true)]
public sealed class HealthEndpoint : EndpointTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HealthEndpoint"/> class.
    /// </summary>
    public HealthEndpoint() : base(
        id: 6,
        name: "HealthEndpoint",
        description: "System health and monitoring endpoints",
        defaultHttpMethods: ["GET"],
        requiresAuthentication: false,
        cachingStrategy: "NoCache",
        isReadOnly: true,
        supportsCaching: false,
        defaultCacheDurationSeconds: null,
        requiresValidation: false,
        securityMethodName: "None",
        rateLimitPolicyName: "Concurrency",
        timeoutMs: 5000,
        maxBodySize: 1024,
        allowedRoles: [])
    {
    }
}
