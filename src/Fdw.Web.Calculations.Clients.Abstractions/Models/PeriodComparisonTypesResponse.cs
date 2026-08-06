using System;
using System.Collections.Generic;

namespace Fdw.Web.Calculations.Clients.Models;

/// <summary>
/// Response containing the available period comparison types.
/// </summary>
public sealed class PeriodComparisonTypesResponse
{
    /// <summary>
    /// Gets or sets the collection of available period comparison types.
    /// </summary>
    public IReadOnlyList<PeriodComparisonTypePayload> Types { get; set; } = Array.Empty<PeriodComparisonTypePayload>();
}
