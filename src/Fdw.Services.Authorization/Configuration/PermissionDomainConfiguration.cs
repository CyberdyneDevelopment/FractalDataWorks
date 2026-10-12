using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Authorization.Configuration;

/// <summary>
/// The permissions domain's record: the four properties every domain has, and everything a list of
/// permissions shows.
/// </summary>
/// <remarks>
/// Maps to <c>authz.Permission</c> in ConfigurationDb. A permission is nothing beyond these fields, so
/// they all live here, on the domain row: a page of permissions is one read of one table. Reading a
/// permission stamps them onto its implementation; saving one whose values differ writes a new version
/// of this row from them (FDW-978, FDW-1002).
/// <para>
/// <see cref="PermissionDomain"/> is the permission's own domain (for example "users"). It is not
/// <see cref="DomainConfiguration.Domain"/>, which names the domain this row belongs to and is always
/// "Permission".
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public partial class PermissionDomainConfiguration : DomainConfiguration
{
    /// <summary>Gets or sets the permission's own domain.</summary>
    public string PermissionDomain { get; set; } = string.Empty;

    /// <summary>Gets or sets the resource the permission is over.</summary>
    public string Resource { get; set; } = string.Empty;

    /// <summary>Gets or sets the action the permission allows.</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Gets or sets the scope: global or tenant.</summary>
    public string Scope { get; set; } = string.Empty;

    /// <summary>Gets or sets the display name.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the category the permission is grouped under.</summary>
    public string? Category { get; set; }
}
