using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Quality.Configuration;

/// <summary>
/// Configuration for promotion requests between environments.
/// Stored in quality.PromotionRequest table.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Promotion",
    ServiceType = "Request")]
// Why: IGenericConfiguration is required by ImplementationConfigurationProviderBase<T>
// for dual-source (ctrl+cfg) provider pattern.
public sealed partial class PromotionRequestConfiguration : IGenericConfiguration
{
    /// <inheritdoc />
    public string SectionName => "Promotions";

    /// <inheritdoc />
    // Why: Matches ServiceCategory from [ManagedConfiguration] attribute for IOptions binding path.
    public string ServiceType => "Promotion";

    /// <inheritdoc />
    public string? ServiceOptionType => null;

    /// <summary>
    /// Gets or sets the display name for this promotion request.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique identifier for this promotion request.
    /// </summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// Gets or sets the source environment name.
    /// </summary>
    public string SourceEnvironment { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target environment name.
    /// </summary>
    public string TargetEnvironment { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the collection of items to promote.
    /// </summary>
    public IList<PromotionRequestItemConfiguration> Items { get; set; } = [];

    /// <summary>
    /// Gets or sets the username who requested the promotion.
    /// </summary>
    public string RequestedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets optional notes about this promotion request.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the status of this request (e.g., "Pending", "Approved", "Rejected", "Completed").
    /// </summary>
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// Gets or sets the username who approved the promotion.
    /// </summary>
    public string? ApprovedBy { get; set; }

    /// <summary>
    /// Gets or sets when the promotion was approved.
    /// </summary>
    public DateTimeOffset? ApprovedAt { get; set; }

    /// <summary>
    /// Gets or sets when the promotion was completed.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets when the promotion request was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
