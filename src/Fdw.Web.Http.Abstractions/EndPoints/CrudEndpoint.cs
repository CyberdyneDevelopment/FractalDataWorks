using Fdw.Collections.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Web.Http.Abstractions.EndPoints;

/// <summary>
/// Get, Read, Update, Delete operations endpoint type.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointTypes), "CrudEndpoint", RestrictToCurrentCompilation = true)]
public sealed class CrudEndpoint : EndpointTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CrudEndpoint"/> class.
    /// </summary>
    public CrudEndpoint() : base(
        id: 1,
        name: "CrudEndpoint",
        description: "Get, Read, Update, Delete operations for data management",
        defaultHttpMethods: ["GET", "POST", "PUT", "DELETE", "PATCH"],
        requiresAuthentication: true,
        cachingStrategy: "NoCache",
        isReadOnly: false,
        supportsCaching: false,
        defaultCacheDurationSeconds: null,
        requiresValidation: true,
        securityMethodName: "JWT",
        rateLimitPolicyName: "FixedWindow",
        timeoutMs: 30000,
        maxBodySize: 10485760,
        allowedRoles: ["User", "Admin"])
    {
    }
}
