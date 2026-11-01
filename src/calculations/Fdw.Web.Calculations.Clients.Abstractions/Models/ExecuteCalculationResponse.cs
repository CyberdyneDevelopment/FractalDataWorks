using System;

namespace Fdw.Web.Calculations.Clients.Models;

/// <summary>
/// Response containing the result of a calculation execution.
/// </summary>
public sealed class ExecuteCalculationResponse
{
    /// <summary>
    /// Gets or sets the type of calculation that was executed.
    /// </summary>
    public string CalculationType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the computed result value.
    /// </summary>
    public decimal Result { get; set; }

    /// <summary>
    /// Gets or sets the number of input values that were processed.
    /// </summary>
    public int InputCount { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the calculation was executed.
    /// </summary>
    public DateTimeOffset ExecutedAt { get; set; }
}
