namespace Fdw.Web.Calculations.Clients.Models;

/// <summary>
/// Represents a period comparison type used for time-based calculations.
/// </summary>
public sealed class PeriodComparisonTypePayload
{
    /// <summary>
    /// Gets or sets the unique identifier for the period comparison type.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the period comparison type.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the period comparison type.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}
