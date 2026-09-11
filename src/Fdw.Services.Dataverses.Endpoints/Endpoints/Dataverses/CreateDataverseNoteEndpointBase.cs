using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Dataverses.Results;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Users;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Dataverses.Endpoints;

/// <summary>Writes a note inside a dataverse.</summary>
/// <remarks>
/// Returns the created note in full rather than a bare id: the client appends it to the list
/// without refetching, and with only an id it would have to invent the timestamp and author name
/// to draw the row it just created — an invented row that would then differ from the one a refresh
/// shows.
/// </remarks>
public abstract class CreateDataverseNoteEndpointBase : CrudCreateEndpointBase<CreateDataverseNoteRequest, DataverseNoteResponse>
{
    /// <summary>The longest note body accepted, in characters.</summary>
    /// <remarks>
    /// Stated in the contract so a client can count against it, and enforced as a REJECTION rather
    /// than a truncation. A silent truncation is unrecoverable: the writer sees the note posted and
    /// does not see that the last sentence — usually the point — is gone.
    ///
    /// The column is varchar(MAX), so this limit is a product decision rather than a storage one.
    /// </remarks>
    public const int MaxBodyLength = 4000;

    private readonly IDataverseConfigurationProvider _dataverses;
    private readonly NoteConfigurationProvider _notes;
    private readonly UserConfigurationProvider _users;
    private readonly IAuthenticationContextAccessor _authContext;
    private readonly IDataSetConfigurationProvider _dataSets;

    /// <inheritdoc />
    protected CreateDataverseNoteEndpointBase(
        ILogger<CreateDataverseNoteEndpointBase> logger,
        IDataverseConfigurationProvider dataverses,
        NoteConfigurationProvider notes,
        UserConfigurationProvider users,
        IAuthenticationContextAccessor authContext,
        IDataSetConfigurationProvider dataSets) : base(logger)
    {
        _dataverses = dataverses;
        _notes = notes;
        _users = users;
        _authContext = authContext;
        _dataSets = dataSets;
    }

    /// <summary>Gets the resource name used for policy generation.</summary>
    protected override string ResourceName => "dataverses";

    /// <summary>Resolves a human label for a subject, for the kinds that cost one lookup.</summary>
    /// <remarks>Same rule as the list endpoint: present means resolved, absent never claims deletion.</remarks>
    /// <param name="subjectKind">The subject's kind.</param>
    /// <param name="subjectId">The subject's identity.</param>
    /// <param name="ct">Cancellation token.</param>
    protected async Task<string?> ResolveSubjectLabel(string subjectKind, Guid subjectId, CancellationToken ct)
    {
        if (string.Equals(subjectKind, "DataSet", StringComparison.Ordinal))
        {
            var dataSet = await _dataSets.Get(subjectId, ct).ConfigureAwait(false);
            return dataSet.IsSuccess && dataSet.Value is { Name.Length: > 0 } found ? found.Name : null;
        }

        if (string.Equals(subjectKind, "Dataverse", StringComparison.Ordinal))
        {
            var dataverse = await _dataverses.Get(subjectId, ct).ConfigureAwait(false);
            return dataverse.IsSuccess && dataverse.Value is { Name.Length: > 0 } found ? found.Name : null;
        }

        return null;
    }

    /// <inheritdoc />
    protected override string Route => "/dataverses/{Name}/notes";

    /// <inheritdoc />
    protected override string EndpointSummary => "Write a note in a dataverse";

    /// <inheritdoc />
    protected override string GetResourceName(CreateDataverseNoteRequest request) => request.Name;

    /// <inheritdoc />
    protected override Task<IGenericResult<bool>> CheckExists(CreateDataverseNoteRequest request, CancellationToken ct)
        // Notes are not unique by anything -- two people may write the same sentence, and the same
        // person may write it twice. There is nothing here to collide with.
        => Task.FromResult(GenericResult<bool>.Success(false));

    /// <inheritdoc />
    protected override async Task<IGenericResult<DataverseNoteResponse>> Create(
        CreateDataverseNoteRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Body))
        {
            return GenericResult<DataverseNoteResponse>.Failure(
                DataversesResultCodes.ByName("DataverseNoteInvalid"), Logger,
                ResultDetails.Create("name", request.Name, "reason", "the note has no body"));
        }

        if (request.Body.Length > MaxBodyLength)
        {
            return GenericResult<DataverseNoteResponse>.Failure(
                DataversesResultCodes.ByName("DataverseNoteInvalid"), Logger,
                ResultDetails.Create(
                    "name", request.Name,
                    "reason", $"the note is {request.Body.Length} characters and the limit is {MaxBodyLength}"));
        }

        if (string.IsNullOrWhiteSpace(request.SubjectKind))
        {
            return GenericResult<DataverseNoteResponse>.Failure(
                DataversesResultCodes.ByName("DataverseNoteInvalid"), Logger,
                ResultDetails.Create("name", request.Name, "reason", "the note names no subject kind"));
        }

        var dataverse = await _dataverses.Get(request.Name, ct).ConfigureAwait(false);
        if (dataverse.IsFailure) return dataverse.ToNewResult<DataverseNoteResponse>();
        if (dataverse.Value is null)
        {
            return GenericResult<DataverseNoteResponse>.Failure(
                DataversesResultCodes.ByName("DataverseLoadReturnedNoValue"), Logger,
                ResultDetails.Create("name", request.Name));
        }

        // The author comes from the token and the time from this clock. A client that could send
        // either could lie about both, and a note's value is that it records who saw what, when.
        if (_authContext.Current is not { } caller || !Guid.TryParse(caller.UserId, out var authorUserId))
        {
            return GenericResult<DataverseNoteResponse>.Failure(
                DataversesResultCodes.ByName("DataverseOwnerUnresolved"), Logger,
                ResultDetails.Create("name", request.Name, "reason", "the note's author could not be resolved"));
        }

        var now = DateTimeOffset.UtcNow;
        var config = new NoteImplementationConfiguration
        {
            // Why CreateVersion7: the database mints no Id, and a time-ordered id keeps insert
            // order and sort order the same thing.
            Id = Guid.CreateVersion7(),
            // dataverse.Note.Name is NOT NULL but a note has no name of its own -- it is the body.
            // The id is used rather than a slice of the body, which would look like a title and
            // become one.
            Name = Guid.CreateVersion7().ToString(),
            DataverseId = dataverse.Value.Id,
            SubjectType = request.SubjectKind,
            SubjectId = request.SubjectKey,
            Body = request.Body,
            AuthorUserId = authorUserId,
            CreateDate = now,
        };

        var saved = await _notes.Save(config, "Note", config.Implementation, config.Name, ct).ConfigureAwait(false);
        if (saved.IsFailure) return saved.ToNewResult<DataverseNoteResponse>();

        var author = await _users.Get(authorUserId, ct).ConfigureAwait(false);

        return GenericResult<DataverseNoteResponse>.Success(new DataverseNoteResponse
        {
            Id = config.Id,
            Body = config.Body,
            At = config.CreateDate,
            AuthorUserId = config.AuthorUserId,
            AuthorName = author.IsSuccess && author.Value is { Name.Length: > 0 } found ? found.Name : null,
            SubjectKind = config.SubjectType,
            SubjectKey = config.SubjectId,
            // Resolved here too so the row the client appends matches the one a refresh
            // shows. Sending it on the list and not the create would make the new note the
            // only one without a label, which reads as an unresolvable subject rather than
            // as an asymmetry in the API.
            SubjectLabel = await ResolveSubjectLabel(config.SubjectType, config.SubjectId, ct)
                .ConfigureAwait(false),
            PromotedToRequestId = config.PromotedToRequestId,
        });
    }
}
