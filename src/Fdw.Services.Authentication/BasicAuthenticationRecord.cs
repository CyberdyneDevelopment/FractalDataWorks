using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Authentication;

/// <summary>
/// Data record for the <c>auth.BasicAuthentication</c> table.
/// Represents basic (username/password) authentication credentials for a connection.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class BasicAuthenticationRecord
{

    /// <summary>Gets or sets the parent authentication identifier.</summary>
    public Guid AuthenticationId { get; set; }


    /// <summary>Gets or sets the username.</summary>
    public string? Username { get; set; }

    /// <summary>Gets or sets the password.</summary>
    public string? Password { get; set; }

    /// <summary>Gets or sets the secret manager name for resolving the password.</summary>
    public string? SecretManagerName { get; set; }

    /// <summary>Gets or sets the secret key name within the secret manager.</summary>
    public string? SecretKeyName { get; set; }

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
