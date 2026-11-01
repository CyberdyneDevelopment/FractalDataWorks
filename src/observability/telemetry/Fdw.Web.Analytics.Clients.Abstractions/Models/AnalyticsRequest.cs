using System;

namespace Fdw.Web.Analytics.Clients.Models;

/// <summary>
/// Represents a request for analytics data over a specified time range.
/// </summary>
public sealed class AnalyticsRequest
{
    /// <summary>
    /// Gets or sets the start date of the analytics period.
    /// </summary>
    public DateTimeOffset StartDate { get; set; } = DateTimeOffset.UtcNow.AddDays(-7);

    /// <summary>
    /// Gets or sets the end date of the analytics period.
    /// </summary>
    public DateTimeOffset EndDate { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the optional calculation type to filter by.
    /// </summary>
    public string? CalculationType { get; set; }
}
