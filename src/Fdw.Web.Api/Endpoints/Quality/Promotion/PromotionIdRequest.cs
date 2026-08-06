using System;

namespace Fdw.Services.Quality.Endpoints.Promotion;

/// <summary>
/// Request for promotion operations that require an ID.
/// </summary>
public class PromotionIdRequest
{
    /// <summary>Gets or sets the promotion request ID.</summary>
    public Guid Id { get; set; }
}
