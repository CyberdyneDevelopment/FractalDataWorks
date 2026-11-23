using System;

namespace Fdw.Services.Workspaces;

/// <summary>A workspace that owns a resource, and the row that says so (FDW-6967).</summary>
/// <param name="AttachmentId">The durable Id of the <c>Owns</c> row, which releasing the resource retires.</param>
/// <param name="ResourceId">The Id of the owned resource.</param>
/// <param name="WorkspaceId">The durable Id of the owning workspace.</param>
public sealed record WorkspaceResourceOwner(Guid AttachmentId, Guid ResourceId, Guid WorkspaceId);
