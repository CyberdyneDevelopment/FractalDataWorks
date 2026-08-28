using System;

namespace Fdw.Services.Messaging;

/// <summary>
/// Query parameters for filtering messages.
/// </summary>
public sealed class MessageQuery
{
    /// <summary>Gets or sets the user identifier to query messages for.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets an optional tenant identifier filter.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Gets or sets an optional message type filter.</summary>
    public string? MessageType { get; set; }

    /// <summary>Gets or sets an optional severity filter.</summary>
    public string? Severity { get; set; }

    /// <summary>Gets or sets an optional status filter.</summary>
    public string? Status { get; set; }

    /// <summary>
    /// Gets or sets an optional thread filter, matched against <see cref="MessagePayload.ReferenceId"/>.
    /// </summary>
    /// <remarks>
    /// A conversation between a person and an agent is a set of messages sharing one ReferenceId.
    /// Filtering on it is what lets a participant read back the thread it is part of rather than
    /// the whole inbox, and <c>msg.Message</c> already carries a filtered index over the column.
    /// </remarks>
    public string? ReferenceId { get; set; }

    /// <summary>Gets or sets the number of records to skip.</summary>
    public int Skip { get; set; }

    /// <summary>Gets or sets the number of records to take. Defaults to 50.</summary>
    public int Take { get; set; } = 50;
}
