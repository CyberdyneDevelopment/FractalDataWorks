using System;
using FastEndpoints;

namespace Fdw.Services.Messaging.Endpoints.Models;

/// <summary>
/// Request for listing messages with optional filters.
/// </summary>
public class ListMessagesRequest
{
    /// <summary>
    /// Gets or sets the optional message type filter.
    /// </summary>
    [QueryParam]
    public string? MessageType { get; set; }

    /// <summary>
    /// Gets or sets the optional severity filter.
    /// </summary>
    [QueryParam]
    public string? Severity { get; set; }

    /// <summary>
    /// Gets or sets the optional status filter.
    /// </summary>
    [QueryParam]
    public string? Status { get; set; }

    /// <summary>
    /// Gets or sets the optional thread filter. Returns only the messages sharing this reference,
    /// which is how one side of an agent conversation reads the thread back.
    /// </summary>
    [QueryParam]
    public string? ReferenceId { get; set; }

    /// <summary>
    /// Gets or sets the exclusive lower cursor — return only turns after this message.
    /// Use instead of skip when tailing a live thread.
    /// </summary>
    [QueryParam]
    public Guid? After { get; set; }

    /// <summary>
    /// Gets or sets the exclusive upper cursor — return the last page of turns before this message.
    /// </summary>
    [QueryParam]
    public Guid? Before { get; set; }

    /// <summary>
    /// Gets or sets the number of items to skip. Default is 0.
    /// </summary>
    [QueryParam]
    public int Skip { get; set; }

    /// <summary>
    /// Gets or sets the number of items to take. Default is 50.
    /// </summary>
    [QueryParam]
    public int Take { get; set; } = 50;
}
