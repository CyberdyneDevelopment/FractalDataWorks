using System;
using System.Collections.Generic;

namespace Fdw.Web.Calculations.Clients.Models;

/// <summary>
/// Response containing the available calculation types.
/// </summary>
public sealed class CalculationTypesResponse
{
    /// <summary>
    /// Gets or sets the collection of available calculation types.
    /// </summary>
    public IReadOnlyList<CalculationTypePayload> Types { get; set; } = Array.Empty<CalculationTypePayload>();
}
