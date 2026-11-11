using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Tags.Abstractions;
using Fdw.Services.Tags.Configuration;
using Fdw.Services.Tags.Results;
using Fdw.Services.Workspaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Tags;

/// <summary>
/// The scope rule for tags: a tenant tag goes on any thing of the tenant; an organization tag goes on a thing placed in one of the organization's
/// workspaces; a workspace tag goes on a thing placed in that workspace. A kind that sits in no workspace takes tenant tags only.
/// </summary>
public sealed class TagScope : ITagScope
{
    private const int WorkspaceLevels = 1;

    private readonly IWorkspaceConfigurationProvider _workspaces;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="TagScope"/> class.</summary>
    /// <param name="workspaces">Reads workspaces with the resources placed in them.</param>
    /// <param name="logger">The logger.</param>
    public TagScope(IWorkspaceConfigurationProvider workspaces, ILogger<TagScope>? logger = null)
    {
        _workspaces = workspaces;
        _logger = logger ?? NullLogger<TagScope>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Allows(TagDomainConfiguration tag, ITaggableKind kind, Guid thingId, CancellationToken cancellationToken = default)
    {
        if (string.Equals(tag.OwnerKind, TagOwnerKinds.Tenant.Name, StringComparison.Ordinal)) return GenericResult.Success();
        if (kind.WorkspaceResourceKind.Length == 0) return Refuse(tag, kind, thingId, "this kind is in no workspace, so it takes tenant tags only");

        var held = string.Equals(tag.OwnerKind, TagOwnerKinds.Workspace.Name, StringComparison.Ordinal)
            ? await HeldByWorkspace(tag.OwnerId, kind, thingId, cancellationToken).ConfigureAwait(false)
            : await HeldByOrganization(tag.OwnerId, kind, thingId, cancellationToken).ConfigureAwait(false);

        return held.IsFailure ? held
            : held.Value ? GenericResult.Success()
            : Refuse(tag, kind, thingId, "the thing is not placed in the workspace or organization that owns the tag");
    }

    private async Task<IGenericResult<bool>> HeldByWorkspace(Guid workspaceId, ITaggableKind kind, Guid thingId, CancellationToken cancellationToken)
    {
        var workspace = await _workspaces.Get(workspaceId, WorkspaceLevels, cancellationToken).ConfigureAwait(false);
        return workspace.IsSuccess && workspace.Value is { } found
            ? GenericResult<bool>.Success(found.Resources.Any(resource => Matches(resource, kind, thingId)))
            : workspace.ToNewResult<bool>();
    }

    private async Task<IGenericResult<bool>> HeldByOrganization(Guid organizationId, ITaggableKind kind, Guid thingId, CancellationToken cancellationToken)
    {
        var workspaces = await _workspaces.Get(WorkspaceLevels, cancellationToken).ConfigureAwait(false);
        return workspaces.IsSuccess && workspaces.Value is { } all
            ? GenericResult<bool>.Success(all.Any(workspace => workspace.OrganizationId == organizationId
                && workspace.Resources.Any(resource => Matches(resource, kind, thingId))))
            : workspaces.ToNewResult<bool>();
    }

    private static bool Matches(WorkspaceResourceConfiguration resource, ITaggableKind kind, Guid thingId)
        => resource.ResourceId == thingId && string.Equals(resource.ResourceType, kind.WorkspaceResourceKind, StringComparison.Ordinal);

    private IGenericResult Refuse(TagDomainConfiguration tag, ITaggableKind kind, Guid thingId, string reason)
        => GenericResult<bool>.Failure(
            TagsResultCodes.ByName("TagNotInScope"), _logger,
            TagDetails.Of("name", tag.Name, "ownerKind", tag.OwnerKind, "ownerId", tag.OwnerId.ToString("D"), "kind", kind.Name, "thingId", thingId.ToString("D"), "reason", reason));
}
