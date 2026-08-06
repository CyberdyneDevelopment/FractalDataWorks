using System;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// Configuration for an organization within a tenant.
/// Maps to <c>tenant.Organizations</c>. Each tenant has one or more orgs;
/// the default org (<see cref="IsDefault"/>=true) is the fallback when a request
/// carries no org claim or header.
/// </summary>
/// <remarks>
/// This class is a plain data carrier. The <c>[ManagedConfiguration]</c> and
/// <c>[GenerateMapper]</c> attributes live on the SQL-layer entity in
/// <c>Fdw.Services.Multitenancy.Sql</c> so the source generators
/// only run in the net10.0 layer.
/// </remarks>
public sealed class OrganizationConfiguration
{

    /// <summary>
    /// Gets or sets the logical organization identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the logical tenant identifier (FK to tenant.Tenants.Id).
    /// </summary>
    public Guid TenantId { get; set; }


    /// <summary>
    /// Gets or sets the display name of this organization.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL-friendly slug for this organization.
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this is the default org for its tenant.
    /// Exactly one org per tenant should have IsDefault=true at any time.
    /// Enforced by a filtered unique index on the DB side.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Gets or sets whether this is the admin org for its tenant.
    /// Exactly one org per tenant should have IsAdminOrg=true at any time.
    /// </summary>
    public bool IsAdminOrg { get; set; }

    /// <summary>
    /// Gets or sets whether this organization is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the optional visibility group identifier.
    /// </summary>
    public Guid? VisibilityGroupId { get; set; }

    /// <summary>
    /// Gets or sets whether this is the current row (version-on-write pattern).
    /// </summary>
    public bool IsCurrent { get; set; }

    /// <summary>
    /// Gets or sets whether this row has been soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    // -- Audit fields --

    /// <summary>Gets or sets the source create date (set by trigger).</summary>
    public DateTimeOffset SrcCreateDate { get; set; }

    /// <summary>Gets or sets the application create date.</summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>Gets or sets the user who created this record.</summary>
    public string? CreateBy { get; set; }

    /// <summary>Gets or sets the on-behalf-of user for create (impersonation).</summary>
    public string? CreateOnBehalfOf { get; set; }

    /// <summary>Gets or sets the last modification date.</summary>
    public DateTimeOffset? ModifyDate { get; set; }

    /// <summary>Gets or sets the user who last modified this record.</summary>
    public string? ModifyBy { get; set; }

    /// <summary>Gets or sets the on-behalf-of user for modify (impersonation).</summary>
    public string? ModifyOnBehalfOf { get; set; }
}
