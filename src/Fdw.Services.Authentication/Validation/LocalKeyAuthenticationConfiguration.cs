using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Authentication.Abstractions;

namespace Fdw.Services.Authentication.Validation;

/// <summary>
/// What the LocalKey kind needs to check a token this host issued.
/// </summary>
/// <remarks>
/// The implementation row for a <c>LocalKey</c> authentication service. It carries the audience a
/// token must name; the issuer is on the domain row because every kind has one and it is what selects
/// the scheme. The signing key is not here — it comes from the secret manager through the same
/// credential provider the issuer signs with, so the two sides cannot drift onto different keys.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "AuthenticationService")]
public partial class LocalKeyAuthenticationConfiguration : IAuthenticationServiceImplementationConfiguration
{
    /// <summary>Initializes a new instance of the <see cref="LocalKeyAuthenticationConfiguration"/> class.</summary>
    public LocalKeyAuthenticationConfiguration()
    {
        ServiceType = "AuthenticationService";
        ServiceOptionType = "LocalKey";
        SectionName = "AuthenticationServices";
    }

    /// <inheritdoc />
    public Guid Id { get; set; }

    /// <inheritdoc />
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc />
    public string SectionName { get; set; }

    /// <inheritdoc />
    public string ServiceType { get; set; }

    /// <inheritdoc />
    public string? ServiceOptionType { get; set; }

    /// <inheritdoc />
    public Guid AuthenticationServiceId { get; set; }

    /// <summary>Gets or sets the audience a token must name.</summary>
    public string Audience { get; set; } = string.Empty;
}
