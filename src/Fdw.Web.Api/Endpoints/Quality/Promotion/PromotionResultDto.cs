using System;
using System.Collections.Generic;

namespace Fdw.Services.Quality.Endpoints.Promotion;

/// <summary>
/// DTO representing the result of executing a promotion.
/// </summary>
public class PromotionResultDto
{
    /// <summary>Gets or sets the promotion request identifier.</summary>
    public Guid RequestId { get; set; }

    /// <summary>Gets or sets the source environment name.</summary>
    public string SourceEnvironment { get; set; } = string.Empty;

    /// <summary>Gets or sets the target environment name.</summary>
    public string TargetEnvironment { get; set; } = string.Empty;

    /// <summary>Gets or sets the total number of items promoted.</summary>
    public int TotalItems { get; set; }

    /// <summary>Gets or sets the number of successfully promoted items.</summary>
    public int SuccessfulItems { get; set; }

    /// <summary>Gets or sets the number of failed items.</summary>
    public int FailedItems { get; set; }

    /// <summary>Gets or sets when the promotion completed.</summary>
    public DateTimeOffset CompletedAt { get; set; }

    /// <summary>Gets or sets the per-item results.</summary>
    public IReadOnlyList<PromotionItemResultDto> Items { get; set; } = [];
}
