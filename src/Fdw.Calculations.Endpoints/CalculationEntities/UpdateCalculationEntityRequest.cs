using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Fdw.Calculations.Endpoints.CalculationEntities;

/// <summary>
/// Request to update an existing calculation entity.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class UpdateCalculationEntityRequest
{
    /// <summary>Gets or sets the calculation entity ID to update.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the updated name. Omitted leaves it unchanged.</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets the updated description. Omitted leaves it unchanged.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the updated calculation entity type name. Omitted leaves it unchanged.</summary>
    public string? CalculationEntityType { get; set; }

    /// <summary>Gets or sets the updated input declarations. Omitted (null) leaves them unchanged;
    /// an empty list is a real request to clear them.</summary>
    public IList<CalculationEntityInputDto>? Inputs { get; set; }

    /// <summary>Gets or sets the target DataSet name for output. Omitted leaves it unchanged.</summary>
    public string? OutputDataSetName { get; set; }

    /// <summary>Gets or sets the result field name written to the output DataSet. Omitted leaves it unchanged.</summary>
    public string? ResultFieldName { get; set; }

    /// <summary>Gets or sets the data type name for the result (e.g. "Decimal", "Int32"). Omitted leaves it unchanged.</summary>
    public string? ResultDataTypeName { get; set; }

    /// <summary>Gets or sets whether the entity should be enabled. Omitted leaves it unchanged.</summary>
    public bool? IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the calculation type's own settings, as a JSON object read into the configuration
    /// class the calculation's type declares. Omitted keeps the current settings, like every other
    /// field here.
    /// </summary>
    public JsonElement? Configuration { get; set; }
}
