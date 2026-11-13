using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// <c>POST /datasets/graph/{name}/fields/{field}/notes</c> (FDW-5452): adds one note to a field out of a Graph data set and answers it, with its stable id, its author and when
/// it was written. The other notes of the field, and the other writers' notes, are left as they are.
/// </summary>
/// <remarks>The note is saved by the update every change to a data set takes, so it is versioned like any other. A caller that cannot be named is refused, never stored as no author.</remarks>
public abstract class AddGraphFieldNoteEndpointBase : CrudCreateEndpointBase<AddGraphFieldNoteRequest, GraphFieldNoteRequest>
{
    private readonly IDataSetConfigurationProvider _dataSetProvider;

    /// <summary>Initializes a new instance of the <see cref="AddGraphFieldNoteEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dataSetProvider">Reads and saves data sets.</param>
    protected AddGraphFieldNoteEndpointBase(ILogger<AddGraphFieldNoteEndpointBase> logger, IDataSetConfigurationProvider dataSetProvider) : base(logger)
    {
        _dataSetProvider = dataSetProvider;
    }

    /// <inheritdoc />
    protected override string ResourceName => "datasets";

    /// <inheritdoc />
    protected override string Route => "/datasets/graph/{Name}/fields/{Field}/notes";

    /// <inheritdoc />
    protected override string EndpointSummary => "Add a note to a field of a Graph data set";

    /// <inheritdoc />
    protected override string EndpointDescription => "Adds one question or reply to a field out and answers it with its id, author and time. The field's other notes are left as they are.";

    /// <inheritdoc />
    protected override string GetResourceName(AddGraphFieldNoteRequest request) => request.Name;

    /// <inheritdoc />
    protected override Task<IGenericResult<bool>> CheckExists(AddGraphFieldNoteRequest request, CancellationToken ct)
        => Task.FromResult<IGenericResult<bool>>(GenericResult<bool>.Success(false)); // Each add is a new note: there is no duplicate to find.

    /// <inheritdoc />
    protected override Task<IGenericResult<GraphFieldNoteRequest>> Create(AddGraphFieldNoteRequest request, CancellationToken ct)
        => GraphDataSetEdits.AddNote(_dataSetProvider, request, GraphDataSetStamp.Saving(User), Logger, ct);
}
