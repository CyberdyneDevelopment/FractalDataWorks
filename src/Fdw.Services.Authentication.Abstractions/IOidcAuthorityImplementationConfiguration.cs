using System;
using Fdw.Configuration;

namespace Fdw.Services.Authentication.Abstractions;

/// <summary>The contract every OidcAuthority implementation carries.</summary>
public interface IOidcAuthorityImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the domain record's durable id.</summary>
    Guid OidcAuthorityId { get; set; }


    /// <summary>Gets or sets the OidcAuthority Issuer.</summary>
    string Issuer { get; set; }

    /// <summary>Gets or sets the OidcAuthority AuthorizationEndpoint.</summary>
    string AuthorizationEndpoint { get; set; }

    /// <summary>Gets or sets the OidcAuthority TokenEndpoint.</summary>
    string TokenEndpoint { get; set; }

    /// <summary>Gets or sets the OidcAuthority JwksUri.</summary>
    string JwksUri { get; set; }

    /// <summary>Gets or sets the OidcAuthority ClientId.</summary>
    string ClientId { get; set; }

    /// <summary>Gets or sets the OidcAuthority ClientSecretName.</summary>
    string? ClientSecretName { get; set; }

    /// <summary>Gets or sets the OidcAuthority RedirectUri.</summary>
    string RedirectUri { get; set; }

    /// <summary>Gets or sets the OidcAuthority Scopes.</summary>
    string Scopes { get; set; }

    /// <summary>Gets or sets the OidcAuthority SubjectClaim.</summary>
    string SubjectClaim { get; set; }

    /// <summary>Gets or sets the OidcAuthority ValidAudiences.</summary>
    string ValidAudiences { get; set; }

    /// <summary>Gets or sets the OidcAuthority ValidAlgorithms.</summary>
    string ValidAlgorithms { get; set; }

    /// <summary>Gets or sets the OidcAuthority AssertableMethods.</summary>
    string AssertableMethods { get; set; }

    /// <summary>Gets or sets the OidcAuthority ClockSkewSeconds.</summary>
    int ClockSkewSeconds { get; set; }

    /// <summary>Gets or sets whether this is the current active version of the record.</summary>
    bool IsCurrent { get; set; }

    /// <summary>Gets or sets whether this record has been soft-deleted.</summary>
    bool IsDeleted { get; set; }
}
