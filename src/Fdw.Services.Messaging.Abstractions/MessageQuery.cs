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

    /// <summary>
    /// Gets or sets the exclusive lower cursor — return only turns after this message.
    /// </summary>
    /// <remarks>
    /// Keyset paging, for tailing a live thread. Skip/Take cannot do that job: a message arriving
    /// mid-scroll shifts every offset, so the next page double-renders a turn or drops one. Mutually
    /// exclusive with <see cref="Before"/>, and an unknown cursor fails rather than quietly
    /// restarting from the beginning of the thread.
    /// </remarks>
    public Guid? After { get; set; }

    /// <summary>
    /// Gets or sets the exclusive upper cursor — return only turns before this message.
    /// </summary>
    /// <remarks>Scrollback, the mirror of <see cref="After"/>. Returns the LAST Take before it.</remarks>
    public Guid? Before { get; set; }

    /// <summary>Gets or sets the number of records to skip.</summary>
    public int Skip { get; set; }

    /// <summary>Gets or sets the number of records to take. Defaults to 50.</summary>
    public int Take { get; set; } = 50;
}
