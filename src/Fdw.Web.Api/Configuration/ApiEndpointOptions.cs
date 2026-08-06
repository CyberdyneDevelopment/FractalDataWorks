using System;
using System.Collections.Generic;

namespace Fdw.Web.Api.Configuration;

/// <summary>
/// Configuration options for API endpoint routing, authorization policies, and domain filtering.
/// </summary>
public class ApiEndpointOptions
{
    /// <summary>Gets or sets the route prefix applied to all API endpoints.</summary>
    public string RoutePrefix { get; set; } = string.Empty;

    /// <summary>Gets or sets the list of domain names that are disabled and should not register endpoints.</summary>
    public IList<string> DisabledDomains { get; set; } = [];

    /// <summary>Gets or sets the dictionary of authorization policy overrides keyed by resource name.</summary>
    public IDictionary<string, string> PolicyOverrides { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>Gets or sets whether detailed error information is included in API responses.</summary>
    public bool EnableDetailedErrors { get; set; }

    /// <summary>
    /// Determines whether the specified domain is enabled for endpoint registration.
    /// </summary>
    public virtual bool IsDomainEnabled(string domainName)
    {
        return !DisabledDomains.Any(d => string.Equals(d, domainName, System.StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Returns the authorization policy for a resource, using an override if configured.
    /// </summary>
    public virtual string GetPolicy(string resourceName, string defaultPolicy)
    {
        return PolicyOverrides.TryGetValue(resourceName, out var overridePolicy)
            ? overridePolicy
            : defaultPolicy;
    }
}
