using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Web.RestEndpoints.ErrorMapping;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Gives one path or one container back to its data store (FDW-6967).
/// </summary>
/// <typeparam name="TRequest">The request of the route: <see cref="DataStoreItemOwnerRouteRequest"/> for a path, <see cref="DataStoreContainerOwnerRouteRequest"/>
/// for a container.</typeparam>
/// <remarks>
/// Signed in is the only gate here: <see cref="IDataStoreItemOwnership.Release"/> decides at the owner's scope alone (FDW-6964, decision 3), so a
/// store administrator who is not a member of the owner cannot release it.
/// <para>A path and a container each have an endpoint of their own, <see cref="ReleaseDataStorePathOwnerEndpointBase"/> and
/// <see cref="ReleaseDataStoreContainerOwnerEndpointBase"/>, one route each: a WebMcp tool is served only for an endpoint with one route.</para>
/// </remarks>
public abstract class ReleaseDataStoreItemOwnerEndpointBase<TRequest> : Endpoint<TRequest>
    where TRequest : DataStoreItemOwnerRouteRequest
{
    private readonly IDataStoreItemOwnership _ownership;

    /// <summary>Initializes a new instance of the <see cref="ReleaseDataStoreItemOwnerEndpointBase{TRequest}"/> class.</summary>
    /// <param name="ownership">Releases the owner and decides whether the caller may.</param>
    protected ReleaseDataStoreItemOwnerEndpointBase(IDataStoreItemOwnership ownership)
    {
        _ownership = ownership;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Delete(Route);
        Summary(s =>
        {
            s.Summary = "Give a data store path or container back to its store";
            s.Description = "Releases the workspace that owns the path or container; writes to it are decided by the store again.";
        });
        ConfigureEndpoint();
    }

    /// <summary>Gets the one route this endpoint serves.</summary>
    protected abstract string Route { get; }

    /// <summary>The path or container the request's route gives.</summary>
    /// <param name="req">The request.</param>
    /// <returns>The address of the item to release.</returns>
    protected abstract DataStoreItemAddress Address(TRequest req);

    /// <summary>Lets a concrete endpoint add its own settings, such as its tags.</summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc />
    public override async Task HandleAsync(TRequest req, CancellationToken ct)
    {
        var released = await _ownership.Release(Address(req), ct).ConfigureAwait(false);
        if (released.IsFailure)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(released, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }
}
