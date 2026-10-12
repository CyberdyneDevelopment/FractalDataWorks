using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Calculations.Endpoints.CalculationEntities;

/// <summary>
/// Detail DTO for calculation entity get/create/update responses.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CalculationEntityDetailDto
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the calculation entity name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the calculation entity type name.</summary>
    public string CalculationEntityType { get; set; } = string.Empty;

    /// <summary>Gets or sets the declared inputs for this calculation.</summary>
    public IList<CalculationEntityInputDto> Inputs { get; set; } = [];

    /// <summary>Gets or sets the target DataSet name for output.</summary>
    public string? OutputDataSetName { get; set; }

    /// <summary>Gets or sets the result field name written to the output DataSet.</summary>
    public string? ResultFieldName { get; set; }

    /// <summary>Gets or sets the data type name for the result.</summary>
    public string ResultDataTypeName { get; set; } = "Decimal";

    /// <summary>Gets or sets whether this entity is enabled.</summary>
    public bool IsEnabled { get; set; }
}
