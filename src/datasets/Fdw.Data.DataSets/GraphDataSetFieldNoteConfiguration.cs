using System;
using Fdw.Configuration;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>A note in the discussion on a field out of a Graph data set (FDW-5294): a question or a reply.</summary>
/// <remarks>
/// Maps to <c>data.GraphDataSetFieldNote</c>, owned by one <c>data.GraphDataSetField</c> row through <c>GraphDataSetFieldRowId</c>, carried on
/// <see cref="GraphDataSetFieldConfiguration.Notes"/>. <see cref="NoteKind"/> is the name of a <see cref="GraphFieldNoteKinds"/> option. Who wrote
/// the note and when are <see cref="AuthorUserId"/> and <see cref="AuthoredAt"/> (not the audit columns, which hold the SQL login and reset when a child row is inserted fresh). A field's state is worked out from its notes, not stored.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class GraphDataSetFieldNoteConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the stable logical identifier for this note, minted by the app before INSERT.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name of the <see cref="GraphFieldNoteKinds"/> option: what this note is.</summary>
    public string NoteKind { get; set; } = string.Empty;

    /// <summary>Gets or sets what the note says.</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>Gets or sets the position of the note among its field's notes.</summary>
    public int Ordinal { get; set; }

    /// <summary>Gets or sets whether a question has been marked answered; always false for a kind that cannot be answered.</summary>
    public bool IsAnswered { get; set; }

    /// <summary>Gets or sets the user id of the person who wrote the note (FDW-5452); set by the server when the note is made and carried across saves, null only for a note written before authors were recorded.</summary>
    public Guid? AuthorUserId { get; set; }

    /// <summary>Gets or sets when the note was written (FDW-5452); set with <see cref="AuthorUserId"/>, never both absent for a new note.</summary>
    public DateTimeOffset? AuthoredAt { get; set; }

    /// <summary>Gets or sets the user id of the person who marked the note answered (FDW-5452); null until it is, and for a note answered before this was recorded.</summary>
    public Guid? AnsweredByUserId { get; set; }

    /// <summary>Gets or sets when the note was marked answered (FDW-5452); set with <see cref="AnsweredByUserId"/>.</summary>
    public DateTimeOffset? AnsweredAt { get; set; }

    /// <summary>Gets or sets whether this is the current active version of the record.</summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>Gets or sets whether this record has been soft-deleted.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Gets or sets the original creation date from the source system (if migrated).</summary>
    public DateTimeOffset? SrcCreateDate { get; set; }

    /// <summary>Gets or sets the timestamp when the record was created.</summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>Gets or sets the database user who created the record.</summary>
    public string CreateBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the application user on whose behalf the record was created: who wrote the note.</summary>
    public string CreateOnBehalfOf { get; set; } = string.Empty;

    /// <summary>Gets or sets the timestamp when the record was last modified.</summary>
    public DateTimeOffset ModifyDate { get; set; }

    /// <summary>Gets or sets the database user who last modified the record.</summary>
    public string ModifyBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the application user on whose behalf the record was last modified.</summary>
    public string ModifyOnBehalfOf { get; set; } = string.Empty;
}
