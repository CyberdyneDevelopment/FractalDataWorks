using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Web.RestEndpoints.ErrorMapping;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Makes a workspace the owner of one path or one container of a shared data store (FDW-6967).
/// </summary>
/// <typeparam name="TRequest">The request of the route: <see cref="AssignDataStoreItemOwnerRequest"/> for a path, <see cref="AssignDataStoreContainerOwnerRequest"/>
/// for a container.</typeparam>
/// <remarks>
/// Signed in is the only gate here: <see cref="IDataStoreItemOwnership.Assign"/> decides, because who may lend the item depends on who owns it now
/// (the store's rule for an unowned item, its path's owner for a container under an owned path) and the caller must also be able to change the
/// workspace that will own it. The endpoint validates the request, calls the service and sends its answer.
/// <para>A path and a container each have an endpoint of their own, <see cref="AssignDataStorePathOwnerEndpointBase"/> and
/// <see cref="AssignDataStoreContainerOwnerEndpointBase"/>, one route each: a WebMcp tool is served only for an endpoint with one route.</para>
/// </remarks>
public abstract class AssignDataStoreItemOwnerEndpointBase<TRequest> : Endpoint<TRequest, DataStoreItemOwnerResponse>
    where TRequest : AssignDataStoreItemOwnerRequest
{
    private readonly IDataStoreItemOwnership _ownership;

    /// <summary>Initializes a new instance of the <see cref="AssignDataStoreItemOwnerEndpointBase{TRequest}"/> class.</summary>
    /// <param name="ownership">Records the owner and decides whether the caller may.</param>
    protected AssignDataStoreItemOwnerEndpointBase(IDataStoreItemOwnership ownership)
    {
        _ownership = ownership;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        // POST, not PUT (FDW058): assigning an owner attaches one to the path or container.
        Post(Route);
        Summary(s =>
        {
            s.Summary = "Make a workspace the owner of a data store path or container";
            s.Description = "Records the named workspace as the owner of the path or container. The store stays shared; writes to an owned item are decided at its owner's scope.";
        });
        ConfigureEndpoint();
    }

    /// <summary>Gets the one route this endpoint serves.</summary>
    protected abstract string Route { get; }

    /// <summary>The path or container the request's route gives.</summary>
    /// <param name="req">The request.</param>
    /// <returns>The address of the item to own.</returns>
    protected abstract DataStoreItemAddress Address(TRequest req);

    /// <summary>Lets a concrete endpoint add its own settings, such as its tags.</summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc />
    public override async Task HandleAsync(TRequest req, CancellationToken ct)
    {
        var assigned = await _ownership.Assign(Address(req), req.Workspace, ct).ConfigureAwait(false);
        if (assigned.IsFailure)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(assigned, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(DataStoreItemOwnerResponse.Of(assigned.Value!), ct).ConfigureAwait(false);
    }
}
