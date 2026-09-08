using System;

namespace Fdw.Services.Universes.Endpoints;

/// <summary>A note raised inside a universe.</summary>
/// <remarks>
/// A note has almost no fields by design. There is no title, no status, no severity, no assignee
/// and no edit route: the moment a note has a status it is a ticket, and people stop writing them —
/// which costs every observation that was never worth filing but was worth saying.
/// <see cref="PromotedToRequestId"/> is the one escape hatch, and it points OUTWARD at the thing
/// that does have a status.
/// </remarks>
public class UniverseNoteResponse
{
    /// <summary>Gets or sets the note's identity.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the text. The entire content of the note.</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>Gets or sets when it was written, from the server clock.</summary>
    public DateTimeOffset At { get; set; }

    /// <summary>Gets or sets the author's stable identity.</summary>
    public Guid AuthorUserId { get; set; }

    /// <summary>Gets or sets the author's display name, or null when the account no longer resolves.</summary>
    /// <remarks>
    /// Null and never empty. A client draws initials from this, so an empty string gives an empty
    /// avatar that reads as a rendering bug rather than as a departed colleague — the two have to
    /// stay distinguishable.
    /// </remarks>
    public string? AuthorName { get; set; }

    /// <summary>Gets or sets what kind of thing the note is about.</summary>
    public string SubjectKind { get; set; } = string.Empty;

    /// <summary>Gets or sets the subject's identity within that kind.</summary>
    /// <remarks>
    /// A Guid, not a name: universe.Note.SubjectId is a non-nullable uniqueidentifier, so the
    /// column cannot carry a name for any kind.
    /// </remarks>
    public Guid SubjectKey { get; set; }

    /// <summary>Gets or sets a human label for the subject, when this kind can be resolved cheaply.</summary>
    /// <remarks>
    /// PRESENT MEANS RESOLVED. Absent never claims the subject was deleted — only that nothing
    /// looked it up. Resolved for DataSet and Universe, which are one lookup each; omitted for the
    /// kinds that would mean reaching into a further five domains from this endpoint.
    ///
    /// Deliberately not null-for-unresolvable: null would have to mean either "gone" or "not
    /// looked at", and a client cannot tell those apart. A real deletion signal, if one is ever
    /// wanted, is its own field rather than an overload of this one.
    /// </remarks>
    public string? SubjectLabel { get; set; }

    /// <summary>Gets or sets the request this note was promoted into, or null when it never was.</summary>
    /// <remarks>Null rather than a bool-and-id pair: the id's absence IS "never promoted".</remarks>
    public Guid? PromotedToRequestId { get; set; }
}
