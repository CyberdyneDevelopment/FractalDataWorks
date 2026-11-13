using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// <c>DELETE /datasets/graph/{name}/fields/{field}/notes/{noteId}</c> (FDW-5452): removes one note from a field out of a Graph data set. The other notes are left as they are.
/// </summary>
/// <remarks>
/// Whoever may update a data set may remove a note: the route asks the update permission (<c>datasets:write</c>), no wider than replacing the field's list by an update, which
/// removes any note today. Whether another person's note may be removed by someone else is put to the owner (FDW-5481); a narrower answer changes one check here.
/// </remarks>
public abstract class RemoveGraphFieldNoteEndpointBase : CrudDeleteEndpointBase<GraphFieldNoteRouteRequest>
{
    private readonly IDataSetConfigurationProvider _dataSetProvider;

    /// <summary>Initializes a new instance of the <see cref="RemoveGraphFieldNoteEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dataSetProvider">Reads and saves data sets.</param>
    protected RemoveGraphFieldNoteEndpointBase(ILogger<RemoveGraphFieldNoteEndpointBase> logger, IDataSetConfigurationProvider dataSetProvider) : base(logger)
    {
        _dataSetProvider = dataSetProvider;
    }

    /// <inheritdoc />
    protected override string ResourceName => "datasets";

    /// <inheritdoc />
    protected override string DeletePolicy => "datasets:write";

    /// <inheritdoc />
    protected override string Route => "/datasets/graph/{Name}/fields/{Field}/notes/{NoteId}";

    /// <inheritdoc />
    protected override string EndpointSummary => "Remove a note from a field of a Graph data set";

    /// <inheritdoc />
    protected override string EndpointDescription => "Removes one note from a field out. The field's other notes are left as they are.";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(GraphFieldNoteRouteRequest request) => request.Name;

    /// <inheritdoc />
    protected override async Task<IGenericResult<bool>> CheckExistsForDelete(GraphFieldNoteRouteRequest request, CancellationToken ct)
    {
        var found = await _dataSetProvider.Get(request.Name, ct).ConfigureAwait(false);
        return found.IsFailure ? found.ToNewResult<bool>() : GenericResult<bool>.Success(found.Value is not null);
    }

    /// <inheritdoc />
    protected override Task<IGenericResult> Delete(GraphFieldNoteRouteRequest request, CancellationToken ct)
        => GraphDataSetEdits.RemoveNote(_dataSetProvider, request, Logger, ct);
}
