using System;

namespace Fdw.Documents.Records.Abstractions;

/// <summary>
/// A node of an organization's record tree (FDW-3395): something documents attach to, such as a project, a site or a case.
/// </summary>
/// <remarks>What a kind means is the organization's document configuration, not code: FDW gives the tree, the organization names its kinds.</remarks>
public interface IRecordNode
{
    /// <summary>Gets the node's public id: a UUIDv7 the application set, never generated for it.</summary>
    Guid Id { get; }

    /// <summary>Gets the organization the node belongs to.</summary>
    Guid OrganizationId { get; }

    /// <summary>Gets the record kind, as the document configuration names it.</summary>
    string Kind { get; }

    /// <summary>Gets the public id of the parent node, or null for a node at the top of the tree.</summary>
    Guid? ParentId { get; }

    /// <summary>Gets the node's name.</summary>
    string Name { get; }
}
