using System;

namespace Fdw.Web.Analytics.Clients.Models;

/// <summary>
/// Represents a promotion request between two environments.
/// </summary>
public sealed class PromotionPayload
{
    /// <summary>
    /// Gets or sets the unique identifier of the promotion.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the promotion.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source environment name.
    /// </summary>
    public string SourceEnvironment { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target environment name.
    /// </summary>
    public string TargetEnvironment { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current status of the promotion (e.g., Pending, Approved, Rejected).
    /// </summary>
    public string Status { get; set; } = string.Empty;
}
