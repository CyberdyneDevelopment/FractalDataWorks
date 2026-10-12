using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Users.Configuration;

/// <summary>
/// The users domain's record: the four properties every domain has, and the fields a list of users shows.
/// </summary>
/// <remarks>
/// Maps to <c>usr.Users</c> in ConfigurationDb. A list of users needs a username, an email address,
/// whether the account is active and when it was created -- not the account. Those live here, on the
/// domain row, so a page of users is one read of one table. Reading a user stamps them onto its
/// implementation; saving one whose values differ writes a new version of this row from them. The
/// implementation's table does not hold them.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public partial class UserDomainConfiguration : DomainConfiguration
{
    /// <summary>Gets or sets the email address.</summary>
    public string? Email { get; set; }

    /// <summary>Gets or sets whether the user account is active.</summary>
    public bool IsActive { get; set; }

    /// <summary>Gets or sets when the user record was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }
}
