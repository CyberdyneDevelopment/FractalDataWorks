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
