using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Universes.Results;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Users;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Universes.Endpoints;

/// <summary>Lists the notes raised inside one universe, newest first.</summary>
/// <remarks>
/// Scoped to the universe the note was RAISED IN, which is what universe.Note.UniverseId records.
/// A data set sitting in three universes does not scatter its notes across all three. Asking
/// "everything anyone has said about this data set" is a different question and wants its own
/// route rather than falling out of this one by accident.
/// </remarks>
public abstract class ListUniverseNotesEndpointBase : CrudGetEndpointBase<UniverseNameRequest, UniverseNoteListResponse>
{
    private readonly IUniverseConfigurationProvider _universes;
    private readonly NoteConfigurationProvider _notes;
    private readonly UserConfigurationProvider _users;
    private readonly IDataSetConfigurationProvider _dataSets;

    /// <inheritdoc />
    protected ListUniverseNotesEndpointBase(
        ILogger<ListUniverseNotesEndpointBase> logger,
        IUniverseConfigurationProvider universes,
        NoteConfigurationProvider notes,
        UserConfigurationProvider users,
        IDataSetConfigurationProvider dataSets) : base(logger)
    {
        _universes = universes;
        _notes = notes;
        _users = users;
        _dataSets = dataSets;
    }

    /// <summary>Gets the resource name used for policy generation.</summary>
    protected override string ResourceName => "universes";

    /// <inheritdoc />
    protected override string Route => "/universes/{Name}/notes";

    /// <inheritdoc />
    protected override string EndpointSummary => "List a universe's notes";

    /// <inheritdoc />
    protected override string EndpointDescription => "Returns the notes raised in this universe, newest first.";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(UniverseNameRequest request) => request.Name;

    /// <inheritdoc />
    protected override async Task<IGenericResult<UniverseNoteListResponse?>> FindByIdentifier(
        UniverseNameRequest request, CancellationToken ct)
    {
        var universe = await _universes.Get(request.Name, ct).ConfigureAwait(false);
        if (universe.IsFailure) return universe.ToNewResult<UniverseNoteListResponse?>();

        // Null is a real answer -- no universe by that name -- and the base turns it into a 404.
        // A universe with no notes is a different answer: an empty list.
        if (universe.Value is null) return GenericResult<UniverseNoteListResponse?>.Success(null);

        var notes = await _notes.Get(ct).ConfigureAwait(false);
        if (notes.IsFailure) return notes.ToNewResult<UniverseNoteListResponse?>();

        if (notes.Value is null)
        {
            return GenericResult<UniverseNoteListResponse?>.Failure(
                UniversesResultCodes.ByName("UniverseLoadReturnedNoValue"), Logger,
                ResultDetails.Create("name", request.Name));
        }

        var mine = notes.Value
            .Where(n => n.UniverseId == universe.Value.Id && n.IsCurrent && !n.IsDeleted)
            .OrderByDescending(n => n.CreateDate)
            .ToList();

        return GenericResult<UniverseNoteListResponse?>.Success(new UniverseNoteListResponse
        {
            Items = await ToResponses(mine, ct).ConfigureAwait(false),
        });
    }

    /// <summary>Projects notes onto their wire shape, resolving each distinct author once.</summary>
    /// <param name="notes">The notes to project.</param>
    /// <param name="ct">Cancellation token.</param>
    protected async Task<List<UniverseNoteResponse>> ToResponses(
        IReadOnlyList<NoteConfiguration> notes, CancellationToken ct)
    {
        // Resolved once per distinct author rather than once per note: a busy universe is mostly
        // one person's notes, and the same lookup repeated per row is the shape that turns a list
        // into N queries.
        var names = new Dictionary<Guid, string?>();
        foreach (var authorId in notes.Select(n => n.AuthorUserId).Distinct())
        {
            var user = await _users.ResolveUser(authorId.ToString(), ct).ConfigureAwait(false);

            // Null, never empty. The client draws initials from this, so an empty string gives an
            // empty avatar that reads as a rendering bug rather than as a departed colleague.
            names[authorId] = user.IsSuccess && user.Value is { Username.Length: > 0 } found
                ? found.Username
                : null;
        }

        var labels = new Dictionary<Guid, string?>();
        foreach (var note in notes)
        {
            if (labels.ContainsKey(note.SubjectId)) continue;
            labels[note.SubjectId] = await ResolveSubjectLabel(note.SubjectType, note.SubjectId, ct)
                .ConfigureAwait(false);
        }

        return notes.Select(n => new UniverseNoteResponse
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
    /// never "deleted". Field, Snapshot, Pipeline, OrchestrationNode and SavedView would each mean
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

        if (string.Equals(subjectKind, "Universe", StringComparison.Ordinal))
        {
            var universe = await _universes.Get(subjectId, ct).ConfigureAwait(false);
            return universe.IsSuccess && universe.Value is { Name.Length: > 0 } found ? found.Name : null;
        }

        return null;
    }
}
