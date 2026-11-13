using System;

namespace Fdw.Services.Data.Endpoints;

/// <summary>One note on a field out of a Graph data set, as a request states it (FDW-5294): a question or a reply in the field's discussion.</summary>
/// <remarks>The position in the field's list is the note's order. The kind and the body are checked by the note's own validator when the data set is saved.</remarks>
public sealed class GraphFieldNoteRequest
{
    /// <summary>Gets or sets what the note is: the name of a <c>GraphFieldNoteKinds</c> option (Question or Reply).</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets what the note says.</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the note has been answered; only a kind that can be answered may be.</summary>
    public bool IsAnswered { get; set; }

    /// <summary>Gets or sets the note's stable id (FDW-5452). Answered with every note. On an update, a note that carries the id of a stored note keeps its id, author and time; a note that carries none is a new note and the server mints its id; an id that matches no stored note is refused.</summary>
    public Guid? Id { get; set; }

    /// <summary>Gets or sets the user id of the person who wrote the note. Set by the server and ignored in a request.</summary>
    public Guid? AuthorUserId { get; set; }

    /// <summary>Gets or sets when the note was written. Set by the server and ignored in a request.</summary>
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>Gets or sets the user id of the person who answered the note. Set by the server and ignored in a request.</summary>
    public Guid? AnsweredByUserId { get; set; }

    /// <summary>Gets or sets when the note was answered. Set by the server and ignored in a request.</summary>
    public DateTimeOffset? AnsweredAt { get; set; }
}
