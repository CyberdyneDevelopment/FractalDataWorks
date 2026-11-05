using System;

namespace Fdw.Documents.Records.Abstractions;

/// <summary>One entry of a document's state history (FDW-3395): the workflow stage it entered, when, by whom and why.</summary>
public interface IStateChange
{
    /// <summary>Gets the entry's public id: a UUIDv7 the application set, never generated for it.</summary>
    Guid Id { get; }

    /// <summary>Gets the public id of the document.</summary>
    Guid DocumentId { get; }

    /// <summary>Gets the workflow stage the document entered.</summary>
    string State { get; }

    /// <summary>Gets when the document entered it.</summary>
    DateTimeOffset EnteredAt { get; }

    /// <summary>Gets who moved the document into it.</summary>
    string EnteredBy { get; }

    /// <summary>Gets the comment given with the change, or null when none was given.</summary>
    string? Comment { get; }
}
