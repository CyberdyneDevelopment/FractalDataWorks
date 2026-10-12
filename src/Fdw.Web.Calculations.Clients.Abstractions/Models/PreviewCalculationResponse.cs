using System;
using System.Collections.Generic;

namespace Fdw.Web.Calculations.Clients.Models;

/// <summary>
/// Response containing the results of a calculation preview with sample data.
/// </summary>
public sealed class PreviewCalculationResponse
{
    /// <summary>
    /// Gets or sets the type of calculation that was previewed.
    /// </summary>
    public string CalculationType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sample data values used in the preview.
    /// </summary>
    public IReadOnlyList<decimal> SampleData { get; set; } = Array.Empty<decimal>();

    /// <summary>
    /// Gets or sets the computed result from the sample data.
    /// </summary>
    public decimal Result { get; set; }

    /// <summary>
    /// Gets or sets the description of the calculation that was previewed.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
