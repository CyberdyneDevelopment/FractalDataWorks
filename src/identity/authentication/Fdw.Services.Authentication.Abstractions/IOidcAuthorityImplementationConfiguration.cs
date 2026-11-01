using System;
using System.Collections.Generic;
using Fdw.Configuration;

namespace Fdw.Services.Authentication.Abstractions;

/// <summary>The contract every OidcAuthority implementation carries.</summary>
public interface IOidcAuthorityImplementationConfiguration : IImplementationConfiguration
{


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

    /// <summary>Gets or sets the authentication type discriminator for how this platform authenticates to the provider's token endpoint.</summary>
    /// <remarks>
    /// Empty means "not yet supplied", never a chosen default. The value names an option of the
    /// client authentication types (<c>None</c> for a public client that proves itself with PKCE
    /// alone, <c>ClientSecret</c> for a confidential client), the same way a connection names its
    /// authentication type.
    /// </remarks>
    string ClientAuthenticationType { get; set; }

    /// <summary>Gets or sets the client authentication key-value configuration.</summary>
    /// <remarks>
    /// Which keys apply depends on <see cref="ClientAuthenticationType"/>. Values are names of
    /// secrets (a secret manager and a key within it), never secret values. Persisted in the child
    /// key-value table, not as a column on this row.
    /// </remarks>
    IDictionary<string, string?> AdditionalProperties { get; set; }

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
