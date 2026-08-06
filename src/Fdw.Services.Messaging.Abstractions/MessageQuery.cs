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

    /// <summary>Gets or sets the number of records to skip.</summary>
    public int Skip { get; set; }

    /// <summary>Gets or sets the number of records to take. Defaults to 50.</summary>
    public int Take { get; set; } = 50;
}
