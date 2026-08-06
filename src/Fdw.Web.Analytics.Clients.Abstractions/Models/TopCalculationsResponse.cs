using System;
using System.Collections.Generic;

namespace Fdw.Web.Analytics.Clients.Models;

/// <summary>
/// Represents the response containing the top calculations ranked by usage.
/// </summary>
public sealed class TopCalculationsResponse
{
    /// <summary>
    /// Gets or sets the list of top calculations with their execution statistics.
    /// </summary>
    public IReadOnlyList<CalculationTypeStats> Calculations { get; set; } = Array.Empty<CalculationTypeStats>();
}
