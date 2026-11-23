using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;

namespace Fdw.Services.Workspaces;

/// <summary>
/// The <see cref="IVariationChainAccess"/> of a host that has workspaces (FDW-5446): the owner of a Scoped variation is a caller who may change the workspace
/// that owns it, and the owner of a data view is the user who owns it.
/// </summary>
/// <remarks>
/// "Acting in the workspace" is <see cref="IWorkspaceAccessPolicy.MayWrite"/>: the people who can change what the workspace holds, its variations included. A member
/// who only reads it, or an observer, holds the variation without owning it, and a tenant administrator is not an owner by that role alone. A workspace the variation
/// names that cannot be found has no one acting in it. An access decision that could not be made is a failure and is never read as no.
/// </remarks>
public sealed class VariationChainAccess : IVariationChainAccess
{
    private readonly IWorkspaceConfigurationProvider _workspaces;
    private readonly IWorkspaceAccessPolicy _access;
    private readonly IAuthenticationContextAccessor _caller;

    /// <summary>Initializes a new instance of the <see cref="VariationChainAccess"/> class.</summary>
    /// <param name="workspaces">Reads the workspace that owns a Scoped variation, with its members.</param>
    /// <param name="access">Decides whether the caller may change that workspace.</param>
    /// <param name="caller">The ambient authentication context of the request.</param>
    public VariationChainAccess(IWorkspaceConfigurationProvider workspaces, IWorkspaceAccessPolicy access, IAuthenticationContextAccessor caller)
    {
        _workspaces = workspaces;
        _access = access;
        _caller = caller;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<bool>> MayShowChain(IDataSetVariationImplementationConfiguration variation, CancellationToken cancellationToken = default)
    {
        if (variation.OwnerWorkspaceId is not { } workspaceId)
            return GenericResult<bool>.Success(OwnedByCaller(variation));

        var workspace = await _workspaces.Get(workspaceId, cancellationToken).ConfigureAwait(false);
        if (workspace.IsFailure) return workspace.ToNewResult<bool>();
        if (workspace.Value is not { } found) return GenericResult<bool>.Success(false);

        var permitted = await _access.MayWrite(found, cancellationToken).ConfigureAwait(false);
        return WorkspaceAccessRefusals.IsUndetermined(permitted)
            ? permitted.ToNewResult<bool>()
            : GenericResult<bool>.Success(permitted.IsSuccess);
    }

    // A data view belongs to the user who saved it; a caller who is not a signed-in user owns nothing.
    private bool OwnedByCaller(IDataSetVariationImplementationConfiguration variation)
        => variation.OwnerUserId is { } owner
           && _caller.Current is { } current
           && Guid.TryParse(current.UserId, out var user)
           && user == owner;
}
