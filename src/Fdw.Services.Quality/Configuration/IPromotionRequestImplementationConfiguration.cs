using Fdw.Configuration;
using Fdw.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Quality.Configuration;

/// <summary>The contract every PromotionRequest implementation carries.</summary>
public interface IPromotionRequestImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the PromotionRequest SourceEnvironment.</summary>
    string SourceEnvironment { get; set; }

    /// <summary>Gets or sets the PromotionRequest TargetEnvironment.</summary>
    string TargetEnvironment { get; set; }

    /// <summary>Gets or sets the PromotionRequest Items.</summary>
    IList<PromotionRequestItemConfiguration> Items { get; set; }

    /// <summary>Gets or sets the PromotionRequest RequestedBy.</summary>
    string RequestedBy { get; set; }

    /// <summary>Gets or sets the PromotionRequest Notes.</summary>
    string? Notes { get; set; }

    /// <summary>Gets or sets the PromotionRequest Status.</summary>
    string Status { get; set; }

    /// <summary>Gets or sets the PromotionRequest ApprovedBy.</summary>
    string? ApprovedBy { get; set; }

    /// <summary>Gets or sets the PromotionRequest ApprovedAt.</summary>
    DateTimeOffset? ApprovedAt { get; set; }

    /// <summary>Gets or sets the PromotionRequest CompletedAt.</summary>
    DateTimeOffset? CompletedAt { get; set; }

    /// <summary>Gets or sets the PromotionRequest CreatedAt.</summary>
    DateTimeOffset CreatedAt { get; set; }
}
