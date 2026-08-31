using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Authentication.Abstractions;

namespace Fdw.Services.Authentication.Validation;

/// <summary>
/// What the JwtBearer kind needs to check a token another issuer signed.
/// </summary>
/// <remarks>
/// <para>
/// The implementation row for a <c>JwtBearer</c> authentication service. It carries the audience a
/// token must name and the roles this host honours; the issuer is on the domain row because every
/// kind has one and it is what selects the scheme.
/// </para>
/// <para>
/// <see cref="Roles"/> is the authorization half. A token from a remote issuer carries that issuer's
/// claims, not FDW's, so nothing in it says what the caller may do here. The roles are declared on
/// this side, against the issuer: holding a token from THIS issuer, for THIS audience, is what confers
/// them. That is a statement about a machine-to-machine issuer dedicated to one service — the issuer is
/// the identity — and it is why the audience is required rather than optional: without it the roles
/// would be conferred on every token the issuer mints, for any client.
/// </para>
/// <para>
/// The roles are FDW role names. They expand to permissions through <c>authz.RolePermission</c>, the
/// same expansion a signed-in user's roles go through, so a change to what a role grants reaches both
/// at once.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "AuthenticationService")]
public partial class JwtBearerAuthenticationConfiguration : IJwtBearerAuthenticationConfiguration
{
    /// <summary>Initializes a new instance of the <see cref="JwtBearerAuthenticationConfiguration"/> class.</summary>
    public JwtBearerAuthenticationConfiguration()
    {
        ServiceType = "AuthenticationService";
        ServiceOptionType = "JwtBearer";
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

    /// <inheritdoc />
    public string Audience { get; set; } = string.Empty;

    /// <inheritdoc />
    public string Roles { get; set; } = string.Empty;
}
