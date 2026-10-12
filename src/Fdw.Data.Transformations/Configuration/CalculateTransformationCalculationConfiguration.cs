using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Data.Transformations.Configuration;

/// <summary>
/// One computed column of a Calculate transformation.
/// Rows live in <c>transform.CalculateTransformationConfigurationCalculation</c>.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class CalculateTransformationCalculationConfiguration : IGenericConfiguration
{
    /// <inheritdoc/>
    /// <remarks>The table has no Id of its own; a row is identified by its owner and position.</remarks>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the row's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the field the result is written to.</summary>
    public string OutputField { get; set; } = string.Empty;

    /// <summary>Gets or sets the expression over the input record.</summary>
    public string Expression { get; set; } = string.Empty;

    /// <summary>Gets or sets the column's position among the transformation's calculations.</summary>
    public int ExecutionOrder { get; set; }
}
