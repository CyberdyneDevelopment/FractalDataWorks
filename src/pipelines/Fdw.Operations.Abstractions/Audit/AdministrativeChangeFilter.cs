using System;

namespace Fdw.Operations.Abstractions.Audit;

/// <summary>
/// Which administrative changes to read. Every term is optional and narrows the read; the caller's own tenant scope always
/// applies on top, so a filter can only ask for less than the caller may see, never more.
/// </summary>
public sealed class AdministrativeChangeFilter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AdministrativeChangeFilter"/> class.
    /// </summary>
    /// <param name="page">The page to read, from 1.</param>
    /// <param name="pageSize">How many records a page holds; at least 1.</param>
    /// <param name="actorUserId">Only changes made by this person.</param>
    /// <param name="tenantId">Only changes made in this tenant.</param>
    /// <param name="organizationId">Only changes made in this organization.</param>
    /// <param name="from">Only changes recorded at or after this time.</param>
    /// <param name="to">Only changes recorded before this time.</param>
    public AdministrativeChangeFilter(
        int page,
        int pageSize,
        Guid? actorUserId,
        Guid? tenantId,
        Guid? organizationId,
        DateTimeOffset? from,
        DateTimeOffset? to)
    {
        Page = page;
        PageSize = pageSize;
        ActorUserId = actorUserId;
        TenantId = tenantId;
        OrganizationId = organizationId;
        From = from;
        To = to;
    }

    /// <summary>Gets the page to read, from 1.</summary>
    public int Page { get; }

    /// <summary>Gets how many records a page holds.</summary>
    public int PageSize { get; }

    /// <summary>Gets the person whose changes to read.</summary>
    public Guid? ActorUserId { get; }

    /// <summary>Gets the tenant whose changes to read.</summary>
    public Guid? TenantId { get; }

    /// <summary>Gets the organization whose changes to read.</summary>
    public Guid? OrganizationId { get; }

    /// <summary>Gets the earliest time to read from.</summary>
    public DateTimeOffset? From { get; }

    /// <summary>Gets the time to read up to.</summary>
    public DateTimeOffset? To { get; }
}
