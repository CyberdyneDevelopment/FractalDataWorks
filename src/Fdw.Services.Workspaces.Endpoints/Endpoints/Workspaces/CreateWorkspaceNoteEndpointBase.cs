using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Workspaces.Results;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Users;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Writes a note inside a workspace.</summary>
/// <remarks>
/// Returns the created note in full rather than a bare id: the client appends it to the list
/// without refetching, and with only an id it would have to invent the timestamp and author name
/// to draw the row it just created — an invented row that would then differ from the one a refresh
/// shows.
/// </remarks>
public abstract class CreateWorkspaceNoteEndpointBase : CrudCreateEndpointBase<CreateWorkspaceNoteRequest, WorkspaceNoteResponse>
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

    private readonly IWorkspaceConfigurationProvider _workspaces;
    private readonly NoteConfigurationProvider _notes;
    private readonly UserConfigurationProvider _users;
    private readonly IAuthenticationContextAccessor _authContext;
    private readonly IDataSetConfigurationProvider _dataSets;
    private readonly IWorkspaceAccessPolicy _access;

    /// <inheritdoc />
    protected CreateWorkspaceNoteEndpointBase(
        ILogger<CreateWorkspaceNoteEndpointBase> logger,
        IWorkspaceConfigurationProvider workspaces,
        NoteConfigurationProvider notes,
        UserConfigurationProvider users,
        IAuthenticationContextAccessor authContext,
        IDataSetConfigurationProvider dataSets,
        IWorkspaceAccessPolicy access) : base(logger)
    {
        _workspaces = workspaces;
        _notes = notes;
        _users = users;
        _authContext = authContext;
        _dataSets = dataSets;
        _access = access;
    }

    /// <summary>Gets the resource name used for policy generation.</summary>
    protected override string ResourceName => "workspaces";

    /// <inheritdoc />
    /// <remarks>A note changes the workspace everyone in it reads, so <see cref="IWorkspaceAccessPolicy.MayWrite"/> decides: a Consumer, whose role is to read the project and not change it, cannot add one (FDW-936).</remarks>
    protected override bool ResourceDecidesAccess => true;

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

        if (string.Equals(subjectKind, "Workspace", StringComparison.Ordinal))
        {
            var workspace = await _workspaces.Get(subjectId, ct).ConfigureAwait(false);
            return workspace.IsSuccess && workspace.Value is { Name.Length: > 0 } found ? found.Name : null;
        }

        return null;
    }

    /// <inheritdoc />
    protected override string Route => "/workspaces/{Name}/notes";

    /// <inheritdoc />
    protected override string EndpointSummary => "Write a note in a workspace";

    /// <inheritdoc />
    protected override string GetResourceName(CreateWorkspaceNoteRequest request) => request.Name;

    /// <inheritdoc />
    protected override Task<IGenericResult<bool>> CheckExists(CreateWorkspaceNoteRequest request, CancellationToken ct)
        // Notes are not unique by anything -- two people may write the same sentence, and the same
        // person may write it twice. There is nothing here to collide with.
        => Task.FromResult(GenericResult<bool>.Success(false));

    /// <inheritdoc />
    protected override async Task<IGenericResult<WorkspaceNoteResponse>> Create(
        CreateWorkspaceNoteRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Body))
        {
            return GenericResult<WorkspaceNoteResponse>.Failure(
                WorkspacesResultCodes.ByName("WorkspaceNoteInvalid"), Logger,
                ResultDetails.Create("name", request.Name, "reason", "the note has no body"));
        }

        if (request.Body.Length > MaxBodyLength)
        {
            return GenericResult<WorkspaceNoteResponse>.Failure(
                WorkspacesResultCodes.ByName("WorkspaceNoteInvalid"), Logger,
                ResultDetails.Create(
                    "name", request.Name,
                    "reason", $"the note is {request.Body.Length} characters and the limit is {MaxBodyLength}"));
        }

        if (string.IsNullOrWhiteSpace(request.SubjectKind))
        {
            return GenericResult<WorkspaceNoteResponse>.Failure(
                WorkspacesResultCodes.ByName("WorkspaceNoteInvalid"), Logger,
                ResultDetails.Create("name", request.Name, "reason", "the note names no subject kind"));
        }

        var workspace = await _workspaces.Get(request.Name, ct).ConfigureAwait(false);
        if (workspace.IsFailure) return workspace.ToNewResult<WorkspaceNoteResponse>();
        if (workspace.Value is null) return WorkspaceAccessRefusals.Absent<WorkspaceNoteResponse>(request.Name, Logger);

        var permitted = await _access.MayWrite(workspace.Value, ct).ConfigureAwait(false);
        if (permitted.IsFailure) return permitted.ToNewResult<WorkspaceNoteResponse>();

        // The author comes from the token and the time from this clock. A client that could send
        // either could lie about both, and a note's value is that it records who saw what, when.
        if (_authContext.Current is not { } caller || !Guid.TryParse(caller.UserId, out var authorUserId))
        {
            return GenericResult<WorkspaceNoteResponse>.Failure(
                WorkspacesResultCodes.ByName("WorkspaceOwnerUnresolved"), Logger,
                ResultDetails.Create("name", request.Name, "reason", "the note's author could not be resolved"));
        }

        var now = DateTimeOffset.UtcNow;
        var config = new NoteImplementationConfiguration
        {
            // Why CreateVersion7: the database mints no Id, and a time-ordered id keeps insert
            // order and sort order the same thing.
            Id = Guid.CreateVersion7(),
            // The implementation the Note domain provider is registered under; '' is registered for nothing.
            Implementation = "Note",
            // workspace.Note.Name is NOT NULL but a note has no name of its own -- it is the body.
            // The id is used rather than a slice of the body, which would look like a title and
            // become one.
            Name = Guid.CreateVersion7().ToString(),
            WorkspaceId = workspace.Value.Id,
            SubjectType = request.SubjectKind,
            SubjectId = request.SubjectKey,
            Body = request.Body,
            AuthorUserId = authorUserId,
            CreateDate = now,
        };

        var saved = await _notes.Save(config, "Note", config.Implementation, config.Name, ct).ConfigureAwait(false);
        if (saved.IsFailure) return saved.ToNewResult<WorkspaceNoteResponse>();

        var author = await _users.Get(authorUserId, ct).ConfigureAwait(false);

        return GenericResult<WorkspaceNoteResponse>.Success(new WorkspaceNoteResponse
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
