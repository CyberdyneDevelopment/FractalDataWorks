using System;

namespace Fdw.Services.Data.Endpoints;

/// <summary>The workspace that owns a path or a container of a shared data store (FDW-6967).</summary>
public class DataStoreItemOwnerResponse
{
    /// <summary>Gets or sets the owning workspace's Id.</summary>
    public Guid WorkspaceId { get; set; }

    /// <summary>Gets or sets the owning workspace's name.</summary>
    public string WorkspaceName { get; set; } = string.Empty;

    /// <summary>Gets or sets the organization the owning workspace belongs to.</summary>
    public Guid OrganizationId { get; set; }

    /// <summary>Gets or sets a value indicating whether a container is owned through the path it is under, not by an owner of its own.</summary>
    public bool ThroughPath { get; set; }

    /// <summary>The response for an owner the ownership service read.</summary>
    /// <param name="owner">The owner; null when the item is the store's.</param>
    /// <returns>The response; null when the item is the store's.</returns>
    public static DataStoreItemOwnerResponse? From(DataStoreItemOwner? owner) => owner is null ? null : Of(owner);

    /// <summary>The response for an owner.</summary>
    /// <param name="owner">The owner.</param>
    /// <returns>The response.</returns>
    public static DataStoreItemOwnerResponse Of(DataStoreItemOwner owner) =>
        new()
        {
            WorkspaceId = owner.WorkspaceId,
            WorkspaceName = owner.WorkspaceName,
            OrganizationId = owner.OrganizationId,
            ThroughPath = owner.ThroughPath,
        };
}
