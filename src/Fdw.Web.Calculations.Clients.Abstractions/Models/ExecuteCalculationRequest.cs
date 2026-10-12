using System;
using System.Collections.Generic;

namespace Fdw.Web.Calculations.Clients.Models;

/// <summary>
/// Request to execute a calculation with the specified input values.
/// </summary>
public sealed class ExecuteCalculationRequest
{
    /// <summary>
    /// Gets or sets the type of calculation to execute.
    /// </summary>
    public string CalculationType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the input values for the calculation.
    /// </summary>
    public IReadOnlyList<decimal> Values { get; set; } = Array.Empty<decimal>();

    /// <summary>
    /// DataSet name. When supplied without inline <see cref="Values"/>, the endpoint pulls rows
    /// from the named DataSet and projects <see cref="FieldName"/> as the numeric input. Empty
    /// when the caller supplies inline Values. Returns 404 if a non-empty DataSet name does not
    /// resolve to a registered DataSet.
    /// </summary>
    public string DataSetName { get; set; } = string.Empty;

    /// <summary>
    /// Field name to project from <see cref="DataSetName"/> rows. Required when DataSetName is
    /// supplied without inline Values; values are coerced to decimal for the calculation.
    /// </summary>
    public string FieldName { get; set; } = string.Empty;
}
