using System;
using Fdw.Web.Endpoints.Contracts;

namespace Fdw.Operations.Endpoints.Audit;

/// <summary>
/// Which administrative changes to list. Every term is optional and narrows the list; the caller's own tenant scope always
/// applies on top.
/// </summary>
public class ListAdministrativeChangesRequest : PaginatedRequest
{
    /// <summary>Gets or sets the person whose changes to list.</summary>
    public Guid? ActorUserId { get; set; }

    /// <summary>Gets or sets the tenant whose changes to list.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Gets or sets the organization whose changes to list.</summary>
    public Guid? OrganizationId { get; set; }

    /// <summary>Gets or sets the earliest time to list from.</summary>
    public DateTimeOffset? From { get; set; }

    /// <summary>Gets or sets the time to list up to.</summary>
    public DateTimeOffset? To { get; set; }
}
