using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using FastEndpoints;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// <c>POST /datasets/graph/{name}/fields/{field}/notes/{noteId}/answer</c> (FDW-5452): marks one note answered, recording who answered it and when, and answers the note.
/// </summary>
/// <remarks>
/// Whether a note can be answered is the kind's own statement (<c>CanBeAnswered</c>); a reply is refused by name. A note already answered keeps who answered it and when, and
/// nothing is saved. The other notes are left as they are.
/// </remarks>
public abstract class AnswerGraphFieldNoteEndpointBase : CrudCreateEndpointBase<GraphFieldNoteRouteRequest, GraphFieldNoteRequest>
{
    private readonly IDataSetConfigurationProvider _dataSetProvider;

    /// <summary>Initializes a new instance of the <see cref="AnswerGraphFieldNoteEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dataSetProvider">Reads and saves data sets.</param>
    protected AnswerGraphFieldNoteEndpointBase(ILogger<AnswerGraphFieldNoteEndpointBase> logger, IDataSetConfigurationProvider dataSetProvider) : base(logger)
    {
        _dataSetProvider = dataSetProvider;
    }

    /// <inheritdoc />
    protected override string ResourceName => "datasets";

    /// <inheritdoc />
    protected override string Route => "/datasets/graph/{Name}/fields/{Field}/notes/{NoteId}/answer";

    /// <inheritdoc />
    protected override string EndpointSummary => "Answer a note on a field of a Graph data set";

    /// <inheritdoc />
    protected override string EndpointDescription => "Marks one note answered and records who answered it and when. A reply cannot be answered.";

    /// <inheritdoc />
    protected override string GetResourceName(GraphFieldNoteRouteRequest request) => request.Name;

    /// <inheritdoc />
    protected override Task<IGenericResult<bool>> CheckExists(GraphFieldNoteRouteRequest request, CancellationToken ct)
        => Task.FromResult<IGenericResult<bool>>(GenericResult<bool>.Success(false)); // Answering is not creating: there is no duplicate to find.

    /// <inheritdoc />
    protected override Task SendCreatedResponse(GraphFieldNoteRequest detail, CancellationToken ct) => Send.ResponseAsync(detail, 200, ct);

    /// <inheritdoc />
    protected override Task<IGenericResult<GraphFieldNoteRequest>> Create(GraphFieldNoteRouteRequest request, CancellationToken ct)
        => GraphDataSetEdits.AnswerNote(_dataSetProvider, request, GraphDataSetStamp.Saving(User), Logger, ct);
}
