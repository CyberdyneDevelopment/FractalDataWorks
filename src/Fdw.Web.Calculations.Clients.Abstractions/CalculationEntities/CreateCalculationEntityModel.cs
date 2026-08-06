using System.Collections.Generic;

namespace Fdw.Web.Calculations.Clients.CalculationEntities;

/// <summary>
/// Request to create a new calculation entity.
/// Mirrors the server-side <c>CreateCalculationEntityRequest</c> endpoint contract.
/// </summary>
public sealed class CreateCalculationEntityModel
{
    /// <summary>
    /// Gets or sets the calculation entity name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the calculation entity type name (e.g. "Formula", "Windowed").
    /// </summary>
    public string CalculationEntityType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the declared inputs for this calculation entity.
    /// </summary>
    public IList<CalculationEntityInputModel> Inputs { get; set; } = new List<CalculationEntityInputModel>();

    /// <summary>
    /// Gets or sets the target DataSet name for output.
    /// </summary>
    public string? OutputDataSetName { get; set; }

    /// <summary>
    /// Gets or sets the result field name written to the output DataSet.
    /// </summary>
    public string? ResultFieldName { get; set; }

    /// <summary>
    /// Gets or sets the data type name for the result (e.g. "Decimal", "Int32").
    /// </summary>
    public string ResultDataTypeName { get; set; } = "Decimal";
}
