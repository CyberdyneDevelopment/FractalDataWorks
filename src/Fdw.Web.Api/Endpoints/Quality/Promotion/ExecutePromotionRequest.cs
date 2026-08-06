using System;

namespace Fdw.Services.Quality.Endpoints.Promotion;

/// <summary>
/// Request to execute an approved promotion request.
/// </summary>
public class ExecutePromotionRequest
{
    /// <summary>Gets or sets the promotion request ID to execute.</summary>
    public Guid Id { get; set; }
}
