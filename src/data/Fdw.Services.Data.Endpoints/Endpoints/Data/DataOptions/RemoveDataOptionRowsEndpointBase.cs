using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Data.Abstractions.DataOptionRows;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Removes several option rows of a container by id, all or none (FDW-5664):
/// <c>DELETE /datastores/{Name}/paths/{PathName}/containers/{ContainerName}/options?id=…&amp;id=…</c>.
/// </summary>
/// <remarks>An id that is not one of the container's rows refuses the whole remove, naming it; nothing is removed.</remarks>
public abstract class RemoveDataOptionRowsEndpointBase : CrudDeleteEndpointBase<RemoveDataOptionRowsRequest>
{
    private readonly IDataOptionRows _rows;

    /// <summary>Initializes a new instance of the <see cref="RemoveDataOptionRowsEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="rows">Removes the option rows.</param>
    protected RemoveDataOptionRowsEndpointBase(ILogger<RemoveDataOptionRowsEndpointBase> logger, IDataOptionRows rows) : base(logger)
    {
        _rows = rows;
    }

    /// <summary>Gets the resource name used for the documentation tag.</summary>
    protected override string ResourceName => "datastores";

    /// <summary>Gets the policy: removing option rows writes the data store's options, as adding them does.</summary>
    protected override string DeletePolicy => "datastores:write";

    /// <summary>Gets the nested-resource route.</summary>
    protected override string Route => "/datastores/{Name}/paths/{PathName}/containers/{ContainerName}/options";

    /// <summary>Gets the summary shown for the route.</summary>
    protected override string EndpointSummary => "Remove option rows of a container";

    /// <summary>Gets the description shown for the route.</summary>
    protected override string EndpointDescription => "Removes the rows the repeated id names in one write, or none when any id is not one of the container's rows.";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(RemoveDataOptionRowsRequest request) => $"{request.Name}/{request.PathName}/{request.ContainerName}/options";

    /// <summary>Answers that the rows are there to remove: the service refuses ids that are not the container's, naming them.</summary>
    protected override Task<IGenericResult<bool>> CheckExistsForDelete(RemoveDataOptionRowsRequest request, CancellationToken ct)
        => Task.FromResult<IGenericResult<bool>>(GenericResult<bool>.Success(true));

    /// <inheritdoc />
    protected override async Task<IGenericResult> Delete(RemoveDataOptionRowsRequest request, CancellationToken ct)
        => await _rows.Remove(
            new DataOptionRowsRemoval { Target = DataOptionRowMapping.Target(request.Name, request.PathName, request.ContainerName), Ids = request.Id },
            ct).ConfigureAwait(false);
}
