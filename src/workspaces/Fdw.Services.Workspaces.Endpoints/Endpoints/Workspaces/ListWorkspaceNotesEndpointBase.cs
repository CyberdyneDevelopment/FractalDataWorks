using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Workspaces.Results;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Users;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Lists the notes raised inside one workspace, newest first.</summary>
/// <remarks>
/// Scoped to the workspace the note was RAISED IN, which is what workspace.Note.WorkspaceId records.
/// A data set sitting in three workspaces does not scatter its notes across all three. Asking
/// "everything anyone has said about this data set" is a different question and wants its own
/// route rather than falling out of this one by accident.
/// </remarks>
public abstract class ListWorkspaceNotesEndpointBase : CrudGetEndpointBase<WorkspaceNameRequest, WorkspaceNoteListResponse>
{
    private readonly IWorkspaceConfigurationProvider _workspaces;
    private readonly NoteConfigurationProvider _notes;
    private readonly UserConfigurationProvider _users;
    private readonly IDataSetConfigurationProvider _dataSets;
    private readonly IWorkspaceAccessPolicy _access;

    /// <inheritdoc />
    protected ListWorkspaceNotesEndpointBase(
        ILogger<ListWorkspaceNotesEndpointBase> logger,
        IWorkspaceConfigurationProvider workspaces,
        NoteConfigurationProvider notes,
        UserConfigurationProvider users,
        IDataSetConfigurationProvider dataSets,
        IWorkspaceAccessPolicy access) : base(logger)
    {
        _workspaces = workspaces;
        _notes = notes;
        _users = users;
        _dataSets = dataSets;
        _access = access;
    }

    /// <summary>Gets the resource name used for policy generation.</summary>
    protected override string ResourceName => "workspaces";

    /// <inheritdoc />
    /// <remarks>Viewing a workspace is decided by <see cref="IWorkspaceAccessPolicy.MayView"/>, not by a platform permission alone (FDW-936).</remarks>
    protected override bool ResourceDecidesAccess => true;

    /// <inheritdoc />
    protected override string Route => "/workspaces/{Name}/notes";

    /// <inheritdoc />
    protected override string EndpointSummary => "List a workspace's notes";

    /// <inheritdoc />
    protected override string EndpointDescription => "Returns the notes raised in this workspace, newest first.";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(WorkspaceNameRequest request) => request.Name;

    /// <inheritdoc />
    protected override async Task<IGenericResult<WorkspaceNoteListResponse?>> FindByIdentifier(
        WorkspaceNameRequest request, CancellationToken ct)
    {
        var workspace = await _workspaces.Get(request.Name, ct).ConfigureAwait(false);
        if (workspace.IsFailure) return workspace.ToNewResult<WorkspaceNoteListResponse?>();

        // Null is a real answer -- no workspace by that name -- and the base turns it into a 404.
        // A workspace with no notes is a different answer: an empty list.
        if (workspace.Value is null) return GenericResult<WorkspaceNoteListResponse?>.Success(null);

        var permitted = await _access.MayView(workspace.Value, ct).ConfigureAwait(false);
        if (permitted.IsFailure)
        {
            // Hidden answers as absent; an undetermined decision is passed on.
            return WorkspaceAccessRefusals.Hides(permitted)
                ? GenericResult<WorkspaceNoteListResponse?>.Success(null)
                : permitted.ToNewResult<WorkspaceNoteListResponse?>();
        }

        var notes = await _notes.Get(ct).ConfigureAwait(false);
        if (notes.IsFailure) return notes.ToNewResult<WorkspaceNoteListResponse?>();

        if (notes.Value is null)
        {
            return GenericResult<WorkspaceNoteListResponse?>.Failure(
                WorkspacesResultCodes.ByName("WorkspaceLoadReturnedNoValue"), Logger,
                ResultDetails.Create("name", request.Name));
        }

        var mine = notes.Value
            .Where(n => n.WorkspaceId == workspace.Value.Id && n.IsCurrent && !n.IsDeleted)
            .OrderByDescending(n => n.CreateDate)
            .ToList();

        return GenericResult<WorkspaceNoteListResponse?>.Success(new WorkspaceNoteListResponse
        {
            Items = await ToResponses(mine, ct).ConfigureAwait(false),
        });
    }

    /// <summary>Projects notes onto their wire shape, resolving each distinct author once.</summary>
    /// <param name="notes">The notes to project.</param>
    /// <param name="ct">Cancellation token.</param>
    protected async Task<List<WorkspaceNoteResponse>> ToResponses(
        IReadOnlyList<INoteImplementationConfiguration> notes, CancellationToken ct)
    {
        // Resolved once per distinct author rather than once per note: a busy workspace is mostly
        // one person's notes, and the same lookup repeated per row is the shape that turns a list
        // into N queries.
        var names = new Dictionary<Guid, string?>();
        foreach (var authorId in notes.Select(n => n.AuthorUserId).Distinct())
        {
            var user = await _users.Get(authorId, ct).ConfigureAwait(false);

            // Null, never empty. The client draws initials from this, so an empty string gives an
            // empty avatar that reads as a rendering bug rather than as a departed colleague.
            names[authorId] = user.IsSuccess && user.Value is { Name.Length: > 0 } found
                ? found.Name
                : null;
        }

        var labels = new Dictionary<Guid, string?>();
        foreach (var note in notes)
        {
            if (labels.ContainsKey(note.SubjectId)) continue;
            labels[note.SubjectId] = await ResolveSubjectLabel(note.SubjectType, note.SubjectId, ct)
                .ConfigureAwait(false);
        }

        return notes.Select(n => new WorkspaceNoteResponse
        {
            Id = n.Id,
            Body = n.Body,
            At = n.CreateDate,
            AuthorUserId = n.AuthorUserId,
            AuthorName = names.TryGetValue(n.AuthorUserId, out var name) ? name : null,
            SubjectKind = n.SubjectType,
            SubjectKey = n.SubjectId,
            SubjectLabel = labels.TryGetValue(n.SubjectId, out var label) ? label : null,
            PromotedToRequestId = n.PromotedToRequestId,
        }).ToList();
    }

    /// <summary>Resolves a human label for a subject, for the kinds that cost one lookup.</summary>
    /// <remarks>
    /// Returns null for every other kind, and the DTO documents that absent means UNRESOLVED and
    /// never "deleted". Field, Snapshot, Pipeline, OrchestrationNode and DataView would each mean
    /// reaching into another domain from this endpoint; they are left unresolved rather than
    /// half-resolved, so a label that IS present always means something was found.
    /// </remarks>
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
}
