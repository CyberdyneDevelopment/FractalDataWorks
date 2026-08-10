using System;
using System.Collections.Generic;

namespace Fdw.Services.Quality.Endpoints.Promotion;

/// <summary>
/// DTO representing a promotion request.
/// </summary>
public class PromotionResponse
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the display name of the promotion request.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the source environment name.</summary>
    public string SourceEnvironment { get; set; } = string.Empty;

    /// <summary>Gets or sets the target environment name.</summary>
    public string TargetEnvironment { get; set; } = string.Empty;

    /// <summary>Gets or sets the items being promoted.</summary>
    public IReadOnlyList<PromotionItemDto> Items { get; set; } = [];

    /// <summary>Gets or sets the status (Pending, Approved, InProgress, Completed, Rejected, Failed).</summary>
    public string Status { get; set; } = "Pending";

    /// <summary>Gets or sets the requesting user.</summary>
    public string RequestedBy { get; set; } = string.Empty;

    /// <summary>Gets or sets optional notes.</summary>
    public string? Notes { get; set; }

    /// <summary>Gets or sets when the promotion was requested.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets the approving user.</summary>
    public string? ApprovedBy { get; set; }

    /// <summary>Gets or sets when the promotion was approved.</summary>
    public DateTimeOffset? ApprovedAt { get; set; }

    /// <summary>Gets or sets when the promotion was completed.</summary>
    public DateTimeOffset? CompletedAt { get; set; }
}
