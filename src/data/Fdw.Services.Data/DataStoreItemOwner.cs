using System;

namespace Fdw.Services.Data;

/// <summary>The workspace that owns a path or a container of a shared data store (FDW-6967).</summary>
/// <param name="WorkspaceId">The owning workspace's durable Id.</param>
/// <param name="WorkspaceName">The owning workspace's name.</param>
/// <param name="OrganizationId">The organization the owning workspace belongs to, read through the workspace and never stored on the path or container.</param>
/// <param name="AttachmentId">The durable Id of the <c>Owns</c> row that records it, which a release retires.</param>
/// <param name="ThroughPath">True when a container is owned through the path it is under, not by an owner of its own.</param>
public sealed record DataStoreItemOwner(Guid WorkspaceId, string WorkspaceName, Guid OrganizationId, Guid AttachmentId, bool ThroughPath);
