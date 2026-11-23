using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;

namespace Fdw.Services.Data;

/// <summary>
/// The default <see cref="IVariationChainAccess"/>: the user who owns a data view owns its chain, and a Scoped variation's chain is shown to nobody (FDW-5446).
/// </summary>
/// <remarks>
/// A Scoped variation is owned by a workspace, which this package cannot see into; the workspace package registers an answer that can, in place of this one.
/// A caller who is not a signed-in user is not the owner of anything, which is the answer and not a guess.
/// </remarks>
public sealed class DataViewChainAccess : IVariationChainAccess
{
    private readonly IAuthenticationContextAccessor _caller;

    /// <summary>Initializes a new instance of the <see cref="DataViewChainAccess"/> class.</summary>
    /// <param name="caller">The ambient authentication context of the request.</param>
    public DataViewChainAccess(IAuthenticationContextAccessor caller)
    {
        _caller = caller;
    }

    /// <inheritdoc />
    public Task<IGenericResult<bool>> MayShowChain(IDataSetVariationImplementationConfiguration variation, CancellationToken cancellationToken = default)
        => Task.FromResult(GenericResult<bool>.Success(
            variation.OwnerWorkspaceId is null
            && variation.OwnerUserId is { } owner
            && _caller.Current is { } current
            && Guid.TryParse(current.UserId, out var user)
            && user == owner));
}
