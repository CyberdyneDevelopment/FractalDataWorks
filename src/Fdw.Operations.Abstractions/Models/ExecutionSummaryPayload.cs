namespace Fdw.Operations.Clients.Models;

using System;

/// <summary>
/// Summary execution information for listing.
/// </summary>
// Why: pure data-transfer POCO, auto-properties only, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public class ExecutionSummaryPayload
{
    /// <summary>Gets or sets the execution ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the item type.</summary>
    public required string ItemType { get; set; }

    /// <summary>Gets or sets the execution name.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the current state.</summary>
    public required string State { get; set; }

    /// <summary>Gets or sets the correlation ID.</summary>
    public string? CorrelationId { get; set; }

    /// <summary>Gets or sets when the execution was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets when the execution completed.</summary>
    public DateTimeOffset? CompletedAt { get; set; }
}
