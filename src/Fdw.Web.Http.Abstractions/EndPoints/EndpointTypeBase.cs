using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Web.Http.Abstractions.EndPoints;

/// <summary>
/// Type option defining different types of endpoints supported by the Fdw Web Framework.
/// Each type provides semantic meaning and enables framework-specific behavior customization.
/// </summary>
[ExcludeFromCodeCoverage] // Abstract base class - concrete types (CRUD, Query, etc.) are also excluded
public abstract class EndpointTypeBase : TypeOptionBase<int, EndpointTypeBase>, IEndpointType
{
    /// <summary>
    /// Gets the default HTTP methods typically used by this endpoint type.
    /// </summary>
    public string[] DefaultHttpMethods { get; }

    /// <summary>
    /// Gets a value indicating whether this endpoint type typically requires authentication.
    /// </summary>
    public bool RequiresAuthentication { get; }

    /// <summary>
    /// Gets the recommended caching strategy for this endpoint type.
    /// </summary>
    public string CachingStrategy { get; }

    /// <summary>
    /// Gets a value indicating whether this endpoint type is read-only.
    /// Read-only endpoints typically don't modify data.
    /// </summary>
    public bool IsReadOnly { get; }

    /// <summary>
    /// Gets a value indicating whether this endpoint type supports caching.
    /// </summary>
    public bool SupportsCaching { get; }

    /// <summary>
    /// Gets the default cache duration in seconds for this endpoint type.
    /// Returns null if caching is not supported.
    /// </summary>
    public int? DefaultCacheDurationSeconds { get; }

    /// <summary>
    /// Gets a value indicating whether this endpoint type requires validation.
    /// </summary>
    public bool RequiresValidation { get; }

    /// <summary>
    /// Gets the default security method name for this endpoint type.
    /// </summary>
    public string SecurityMethodName { get; }

    /// <summary>
    /// Gets the default rate limit policy name for this endpoint type.
    /// </summary>
    public string RateLimitPolicyName { get; }

    /// <summary>
    /// Gets the default request timeout in milliseconds.
    /// </summary>
    public int TimeoutMs { get; }

    /// <summary>
    /// Gets the default maximum request body size in bytes.
    /// </summary>
    public long MaxBodySize { get; }

    /// <summary>
    /// Gets the roles allowed to access this endpoint type by default.
    /// </summary>
    public string[] AllowedRoles { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EndpointTypeBase"/> class.
    /// </summary>
    protected EndpointTypeBase(
        int id,
        string name,
        string description,
        string[] defaultHttpMethods,
        bool requiresAuthentication,
        string cachingStrategy,
        bool isReadOnly,
        bool supportsCaching,
        int? defaultCacheDurationSeconds,
        bool requiresValidation,
        string securityMethodName,
        string rateLimitPolicyName,
        int timeoutMs,
        long maxBodySize,
        string[] allowedRoles)
        : base(id, name, $"Endpoints:{name}", name, description, "Endpoint")
    {
        DefaultHttpMethods = defaultHttpMethods;
        RequiresAuthentication = requiresAuthentication;
        CachingStrategy = cachingStrategy;
        IsReadOnly = isReadOnly;
        SupportsCaching = supportsCaching;
        DefaultCacheDurationSeconds = defaultCacheDurationSeconds;
        RequiresValidation = requiresValidation;
        SecurityMethodName = securityMethodName;
        RateLimitPolicyName = rateLimitPolicyName;
        TimeoutMs = timeoutMs;
        MaxBodySize = maxBodySize;
        AllowedRoles = allowedRoles;
    }

}
