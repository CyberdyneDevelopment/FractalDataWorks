using System.Collections.Generic;

namespace Fdw.Services.Quality.Endpoints.Promotion;

/// <summary>
/// Request body for creating a promotion request.
/// </summary>
public class CreatePromotionRequest
{
    /// <summary>Gets or sets the display name of the promotion request.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the source environment name.</summary>
    public string SourceEnvironment { get; set; } = string.Empty;

    /// <summary>Gets or sets the target environment name.</summary>
    public string TargetEnvironment { get; set; } = string.Empty;

    /// <summary>Gets or sets the items to include in the promotion.</summary>
    public IReadOnlyList<CreatePromotionItemRequest> Items { get; set; } = [];

    /// <summary>Gets or sets the requesting user.</summary>
    public string RequestedBy { get; set; } = string.Empty;

    /// <summary>Gets or sets optional notes.</summary>
    public string? Notes { get; set; }
}
