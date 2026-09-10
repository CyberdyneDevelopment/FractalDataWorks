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
public partial class AuthenticationServiceConfiguration : DomainConfigurationBase, IAuthenticationServiceConfiguration
{
    /// <summary>Initializes a new instance of the <see cref="AuthenticationServiceConfiguration"/> class.</summary>
    public AuthenticationServiceConfiguration()
        : this("AuthenticationService", null, "AuthenticationServices")
    {
    }

    /// <summary>Initializes a new instance of the <see cref="AuthenticationServiceConfiguration"/> class.</summary>
    /// <param name="serviceType">The domain this configuration belongs to.</param>
    /// <param name="implementation">The implementation kind, or null before one is read.</param>
    /// <param name="sectionName">The section these rows are read from.</param>
    protected AuthenticationServiceConfiguration(string serviceType, string? implementation, string sectionName)
    {
        Implementation = implementation;
    }








}
