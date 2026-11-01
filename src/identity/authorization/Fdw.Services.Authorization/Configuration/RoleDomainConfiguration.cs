using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Authorization.Configuration;

/// <summary>
/// The roles domain's record: the four properties every domain has, and the fields a list of roles shows.
/// </summary>
/// <remarks>
/// Maps to <c>authz.Role</c> in ConfigurationDb. A list of roles needs a display name, a description,
/// whether the role is tenant-scoped and when it was created -- not the role's permissions. Those live
/// here, on the domain row, so a page of roles is one read of one table. Reading a role stamps them onto
/// its implementation; saving one whose values differ writes a new version of this row from them. The
/// implementation's table does not hold them (FDW-978, FDW-1002).
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public partial class RoleDomainConfiguration : DomainConfiguration
{
    /// <summary>Gets or sets the display name.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets whether the role is one tenant's own definition.</summary>
    public bool IsTenantScoped { get; set; }

    /// <summary>Gets or sets when the role was created; carried across versions of the row.</summary>
    public DateTimeOffset CreatedAt { get; set; }
}
