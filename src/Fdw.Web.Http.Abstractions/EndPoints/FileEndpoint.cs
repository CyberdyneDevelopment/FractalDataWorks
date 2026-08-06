using Fdw.Collections.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Web.Http.Abstractions.EndPoints;

/// <summary>
/// File upload, download, and manipulation operations endpoint type.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointTypes), "FileEndpoint", RestrictToCurrentCompilation = true)]
public sealed class FileEndpoint : EndpointTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileEndpoint"/> class.
    /// </summary>
    public FileEndpoint() : base(
        id: 5,
        name: "FileEndpoint",
        description: "File upload, download, and manipulation operations",
        defaultHttpMethods: ["GET", "POST", "PUT", "DELETE"],
        requiresAuthentication: true,
        cachingStrategy: "NoCache",
        isReadOnly: false,
        supportsCaching: false,
        defaultCacheDurationSeconds: null,
        requiresValidation: true,
        securityMethodName: "JWT",
        rateLimitPolicyName: "Concurrency",
        timeoutMs: 180000,
        maxBodySize: 104857600,
        allowedRoles: ["User", "Admin"])
    {
    }
}
