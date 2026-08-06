using System;

namespace Fdw.Web.Analytics.Clients.Models;

/// <summary>
/// Represents a request to retrieve the top calculations by usage.
/// </summary>
public sealed class TopCalculationsRequest
{
    /// <summary>
    /// Gets or sets the number of top calculations to return.
    /// </summary>
    public int Count { get; set; } = 10;

    /// <summary>
    /// Gets or sets the start date to consider for ranking calculations.
    /// </summary>
    public DateTimeOffset Since { get; set; } = DateTimeOffset.UtcNow.AddDays(-30);
}
