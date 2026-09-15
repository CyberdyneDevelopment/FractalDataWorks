using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Authentication.Abstractions;

namespace Fdw.Services.Authentication.Validation;

/// <summary>
/// What the ApiKey kind needs to validate an opaque credential this host minted itself.
/// </summary>
/// <remarks>
/// The implementation row for an <c>ApiKey</c> authentication service. Carries nothing beyond the
/// domain contract — see <see cref="IApiKeyAuthenticationConfiguration"/> for why.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "AuthenticationService")]
public partial class ApiKeyAuthenticationConfiguration : IApiKeyAuthenticationConfiguration
{
    /// <inheritdoc/>
    public bool Enabled { get; set; }

    /// <inheritdoc/>
    public string? Authority { get; set; }

    /// <inheritdoc/>
    public string? Description { get; set; }

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Implementation { get; set; } = string.Empty;

    /// <summary>Initializes a new instance of the <see cref="ApiKeyAuthenticationConfiguration"/> class.</summary>
    public ApiKeyAuthenticationConfiguration()
    {
    }

    /// <inheritdoc />
    public Guid Id { get; set; }

    /// <inheritdoc />
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc />
    public Guid AuthenticationServiceId { get; set; }
}
