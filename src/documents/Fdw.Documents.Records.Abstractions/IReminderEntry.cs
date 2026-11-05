using System;

namespace Fdw.Documents.Records.Abstractions;

/// <summary>One entry of the reminder log (FDW-3395): a reminder sent about a requirement that is not yet satisfied.</summary>
public interface IReminderEntry
{
    /// <summary>Gets the entry's public id: a UUIDv7 the application set, never generated for it.</summary>
    Guid Id { get; }

    /// <summary>Gets the public id of the requirement the reminder was about.</summary>
    Guid RequirementId { get; }

    /// <summary>Gets when the reminder was sent.</summary>
    DateTimeOffset SentAt { get; }

    /// <summary>Gets who the reminder was sent to.</summary>
    string Recipient { get; }
}
