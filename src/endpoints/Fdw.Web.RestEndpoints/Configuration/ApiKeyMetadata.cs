using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Web.RestEndpoints.Configuration;

/// <summary>
/// Metadata associated with an API key.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ApiKeyMetadata
{
    /// <summary>
    /// Gets or sets the name or description of the API key.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the roles associated with this API key.
    /// </summary>
    public string[] Roles { get; init; } = [];

    /// <summary>
    /// Gets or sets the scopes associated with this API key.
    /// </summary>
    public string[] Scopes { get; init; } = [];

    /// <summary>
    /// Gets or sets the expiration date for this API key.
    /// </summary>
    public DateTime? ExpirationDate { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether this API key is active.
    /// </summary>
    public bool IsActive { get; init; } = true;
}