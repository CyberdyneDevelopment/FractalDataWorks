using System;

namespace Fdw.Web.Calculations.Clients.Models;

/// <summary>
/// Detailed representation of a calculation definition including formula and metadata.
/// </summary>
public sealed class CalculationDetailPayload
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
    /// Gets or sets the data type of the calculation result.
    /// </summary>
    public string ResultDataType { get; set; } = "decimal";

    /// <summary>
    /// Gets or sets the calculation formula expression.
    /// </summary>
    public string Formula { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional description of the calculation.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the calculation is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the calculation was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the calculation was last modified.
    /// </summary>
    public DateTimeOffset? ModifiedAt { get; set; }

    /// <summary>
    /// Gets or sets the ETag for optimistic concurrency control.
    /// </summary>
    public string? ETag { get; set; }
}
