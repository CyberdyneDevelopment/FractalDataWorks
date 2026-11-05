using System;

namespace Fdw.Documents.Records.Abstractions;

/// <summary>
/// The revision of an organization's document records (FDW-3395), for optimistic concurrency: every write names the revision it read, and
/// is refused when the organization's records moved on since.
/// </summary>
/// <remarks>It has no public id of its own: there is one per organization, keyed by the organization.</remarks>
public interface IOrganizationRevision
{
    /// <summary>Gets the organization.</summary>
    Guid OrganizationId { get; }

    /// <summary>Gets the revision; each write that lands raises it.</summary>
    long Revision { get; }
}
