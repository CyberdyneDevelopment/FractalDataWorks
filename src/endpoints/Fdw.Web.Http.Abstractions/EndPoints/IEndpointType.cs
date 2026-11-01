using Fdw.Collections;

namespace Fdw.Web.Http.Abstractions.EndPoints;

/// <summary>
/// Interface for endpoint type enhanced enums.
/// Provides abstraction for endpoint classification and behavior.
/// </summary>
public interface IEndpointType : ITypeOption<int>
{
    /// <summary>
    /// Gets a value indicating whether this endpoint type is read-only.
    /// Read-only endpoints typically don't modify data.
    /// </summary>
    bool IsReadOnly { get; }

    /// <summary>
    /// Gets a value indicating whether this endpoint type supports caching.
    /// </summary>
    bool SupportsCaching { get; }

    /// <summary>
    /// Gets the default cache duration in seconds for this endpoint type.
    /// Returns null if caching is not supported.
    /// </summary>
    int? DefaultCacheDurationSeconds { get; }

    /// <summary>
    /// Gets a value indicating whether this endpoint type requires validation.
    /// </summary>
    bool RequiresValidation { get; }

    /// <summary>
    /// Gets the default HTTP methods typically used by this endpoint type.
    /// </summary>
    string[] DefaultHttpMethods { get; }

    /// <summary>
    /// Gets a value indicating whether this endpoint type typically requires authentication.
    /// </summary>
    bool RequiresAuthentication { get; }

    /// <summary>
    /// Gets the recommended caching strategy for this endpoint type.
    /// </summary>
    string CachingStrategy { get; }

    /// <summary>
    /// Gets the default security method name for this endpoint type.
    /// </summary>
    string SecurityMethodName { get; }

    /// <summary>
    /// Gets the default rate limit policy name for this endpoint type.
    /// </summary>
    string RateLimitPolicyName { get; }

    /// <summary>
    /// Gets the default request timeout in milliseconds.
    /// </summary>
    int TimeoutMs { get; }

    /// <summary>
    /// Gets the default maximum request body size in bytes.
    /// </summary>
    long MaxBodySize { get; }

    /// <summary>
    /// Gets the roles allowed to access this endpoint type by default.
    /// </summary>
    string[] AllowedRoles { get; }
}
