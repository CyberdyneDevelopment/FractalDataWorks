using Fdw.Collections.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Web.Http.Abstractions.EndPoints;

/// <summary>
/// State-changing business operations endpoint type.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointTypes), "CommandEndpoint", RestrictToCurrentCompilation = true)]
public sealed class CommandEndpoint : EndpointTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandEndpoint"/> class.
    /// </summary>
    public CommandEndpoint() : base(
        id: 3,
        name: "CommandEndpoint",
        description: "State-changing business operations and actions",
        defaultHttpMethods: ["POST", "PUT", "PATCH"],
        requiresAuthentication: true,
        cachingStrategy: "NoCache",
        isReadOnly: false,
        supportsCaching: false,
        defaultCacheDurationSeconds: null,
        requiresValidation: true,
        securityMethodName: "JWT",
        rateLimitPolicyName: "TokenBucket",
        timeoutMs: 60000,
        maxBodySize: 10485760,
        allowedRoles: ["Admin"])
    {
    }
}
