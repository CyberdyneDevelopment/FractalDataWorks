using System;

namespace Fdw.Services.Quality.Endpoints.Promotion;

/// <summary>
/// Request body for approve/reject actions on a promotion request.
/// </summary>
public class PromotionActionRequest
{
    /// <summary>Gets or sets the promotion request ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the user performing the action.</summary>
    public string ActionBy { get; set; } = string.Empty;

    /// <summary>Gets or sets optional comments (required when rejecting).</summary>
    public string? Comments { get; set; }
}
