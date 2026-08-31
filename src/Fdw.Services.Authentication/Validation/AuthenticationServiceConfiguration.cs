using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Authentication.Abstractions;

namespace Fdw.Services.Authentication.Validation;

/// <summary>
/// An authentication service a host trusts to have issued a token.
/// </summary>
/// <remarks>
/// The domain row: it names the service, says which kind it is, and carries the authority every kind
/// has — the issuer a token must name to be routed here. What that kind needs in order to check the
/// token lives on the implementation this holds.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "AuthenticationService")]
public partial class AuthenticationServiceConfiguration
    : IPlatformServiceConfiguration<IAuthenticationServiceImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="AuthenticationServiceConfiguration"/> class.</summary>
    public AuthenticationServiceConfiguration()
        : this("AuthenticationService", null, "AuthenticationServices")
    {
    }

    /// <summary>Initializes a new instance of the <see cref="AuthenticationServiceConfiguration"/> class.</summary>
    /// <param name="serviceType">The domain this configuration belongs to.</param>
    /// <param name="serviceOptionType">The implementation kind, or null before one is read.</param>
    /// <param name="sectionName">The section these rows are read from.</param>
    protected AuthenticationServiceConfiguration(string serviceType, string? serviceOptionType, string sectionName)
    {
        ServiceType = serviceType;
        ServiceOptionType = serviceOptionType;
        SectionName = sectionName;
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
    [ValuesFrom(typeof(AuthenticationServiceTypes))]
    public string? ServiceOptionType { get; set; }

    /// <inheritdoc />
    public string? Description { get; set; }

    /// <summary>Gets or sets whether this service is trusted.</summary>
    /// <remarks>
    /// A declared service that is not enabled is not registered, so a token naming its issuer routes
    /// to no scheme and is refused. Turning one off is how a host stops trusting an issuer without
    /// deleting what it knows about it.
    /// </remarks>
    public bool Enabled { get; set; }

    /// <summary>Gets or sets the issuer a token must name to be routed to this service.</summary>
    /// <remarks>
    /// Every kind has one, which is why it is here and not on the implementation: the issuer is what
    /// selects the scheme, and that selection happens before any kind-specific check runs.
    /// </remarks>
    public string? Authority { get; set; }

    /// <inheritdoc />
    /// <remarks>Held, never inherited — see <see cref="IAuthenticationServiceImplementationConfiguration"/>.</remarks>
    [NotMapped]
    public IAuthenticationServiceImplementationConfiguration? Configuration { get; set; }
}
