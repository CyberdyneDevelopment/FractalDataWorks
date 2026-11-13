using System;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Names one note on a field out of a Graph data set (FDW-5452), by data set, field and the note's stable id: the route of answering or removing it.</summary>
public sealed class GraphFieldNoteRouteRequest
{
    /// <summary>Gets or sets the name of the Graph data set.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the field out.</summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>Gets or sets the note's stable id.</summary>
    public Guid NoteId { get; set; }
}
