using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Authentication;

/// <summary>
/// Data record for the <c>auth.OAuth2Authentication</c> table.
/// Represents OAuth2 client credentials and token endpoint settings for a connection.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class OAuth2AuthenticationRecord
{

    /// <summary>Gets or sets the parent authentication identifier.</summary>
    public Guid AuthenticationId { get; set; }


    /// <summary>Gets or sets the OAuth2 client identifier.</summary>
    public string? ClientId { get; set; }

    /// <summary>Gets or sets the OAuth2 client secret.</summary>
    public string? ClientSecret { get; set; }

    /// <summary>Gets or sets the secret manager name for resolving the client secret.</summary>
    public string? SecretManagerName { get; set; }

    /// <summary>Gets or sets the secret key name within the secret manager.</summary>
    public string? SecretKeyName { get; set; }

    /// <summary>Gets or sets the token endpoint URL.</summary>
    public string? TokenUrl { get; set; }

    /// <summary>Gets or sets the requested scope.</summary>
    public string? Scope { get; set; }

    /// <summary>Gets or sets the OAuth2 grant type.</summary>
    public string GrantType { get; set; } = "client_credentials";

    /// <summary>Gets or sets whether this is the current version.</summary>
    public bool IsCurrent { get; set; }

    /// <summary>Gets or sets whether this record is soft-deleted.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Gets or sets the source system create date.</summary>
    public DateTimeOffset? SrcCreateDate { get; set; }

    /// <summary>Gets or sets the create date.</summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>Gets or sets who created this record.</summary>
    public string CreateBy { get; set; } = string.Empty;

    /// <summary>Gets or sets who this record was created on behalf of.</summary>
    public string CreateOnBehalfOf { get; set; } = string.Empty;

    /// <summary>Gets or sets the last modified date.</summary>
    public DateTimeOffset ModifyDate { get; set; }

    /// <summary>Gets or sets who last modified this record.</summary>
    public string ModifyBy { get; set; } = string.Empty;

    /// <summary>Gets or sets who this record was modified on behalf of.</summary>
    public string ModifyOnBehalfOf { get; set; } = string.Empty;
}
