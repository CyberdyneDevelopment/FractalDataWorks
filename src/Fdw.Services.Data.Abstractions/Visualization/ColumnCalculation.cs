using System;
using System.Collections.Generic;
using Fdw.Configuration;

namespace Fdw.Services.Data.Abstractions.Visualization;

/// <summary>
/// Defines a calculation to apply to a column in the pipeline.
/// </summary>
public sealed class ColumnCalculation
{
    /// <summary>Gets or sets the source column name.</summary>
    public string SourceColumn { get; set; } = string.Empty;

    /// <summary>Gets or sets the output column name for the calculated result.</summary>
    public string OutputColumn { get; set; } = string.Empty;

    /// <summary>Gets or sets the calculation operation name (e.g. "Sum", "Avg", "Count").</summary>
    [ValuesFrom("CalculationOperationTypes")]
    public string Operation { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the operation parameter values, keyed by <c>OperationParameterDefinition.Name</c>.
    /// Each value is the raw string representation (field name, scalar value, comma-separated
    /// field list, or DataSet name) collected by the UI builder and interpreted by the
    /// operation's <c>Calculate</c> method.
    /// </summary>
    // Why: string→string avoids a dependency on Services.Calculations.Abstractions here; the
    // operation implementation owns deserialization of each value from its string form.
    public IDictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>Gets or sets an optional parameter for the calculation (e.g., percentile value).</summary>
    public double? Parameter { get; set; }

    /// <summary>Gets or sets an optional partition column for windowed calculations.</summary>
    public string? PartitionColumn { get; set; }

    /// <summary>Gets or sets an optional ordering column for order-dependent calculations.</summary>
    public string? OrderByColumn { get; set; }
}
