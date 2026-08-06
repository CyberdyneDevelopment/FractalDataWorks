using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Calculations.Endpoints.CalculationEntities;

/// <summary>
/// Request to update an existing calculation entity.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class UpdateCalculationEntityRequest
{
    /// <summary>Gets or sets the calculation entity ID to update.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the updated name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the updated description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the updated calculation entity type name.</summary>
    public string CalculationEntityType { get; set; } = string.Empty;

    /// <summary>Gets or sets the updated input declarations.</summary>
    public IList<CalculationEntityInputDto> Inputs { get; set; } = [];

    /// <summary>Gets or sets the target DataSet name for output.</summary>
    public string? OutputDataSetName { get; set; }

    /// <summary>Gets or sets the result field name written to the output DataSet.</summary>
    public string? ResultFieldName { get; set; }

    /// <summary>Gets or sets the data type name for the result (e.g. "Decimal", "Int32").</summary>
    public string ResultDataTypeName { get; set; } = "Decimal";

    /// <summary>Gets or sets whether the entity should be enabled.</summary>
    public bool IsEnabled { get; set; } = true;
}
