using System;

namespace Fdw.Web.Calculations.Clients.Models;

/// <summary>
/// Summary representation of a calculation definition for list views.
/// </summary>
public sealed class CalculationSummaryPayload
{
    /// <summary>
    /// Gets or sets the unique identifier for the calculation definition.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the calculation definition.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the target DataSet this calculation applies to.
    /// </summary>
    public string TargetDataSet { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the field where the calculation result is stored.
    /// </summary>
    public string ResultFieldName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the calculation is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the calculation was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}
