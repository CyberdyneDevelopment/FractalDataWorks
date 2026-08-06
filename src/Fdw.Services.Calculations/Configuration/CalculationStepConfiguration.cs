using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Calculations.Configuration;

/// <summary>
/// ManagedConfiguration for the <c>calc.CalculationStep</c> table.
/// Represents a single composable step within a calculation entity pipeline.
/// Each step references a <see cref="CalculationOperationTypes"/> operation and
/// produces an output alias that subsequent steps can consume.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Calculation")]
public partial class CalculationStepConfiguration : IGenericConfiguration
{

    /// <summary>Gets or sets the unique identifier for this calculation step.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the parent CalculationEntity's logical Id (FK to calc.CalculationEntity.Id).</summary>
    public Guid CalculationEntityId { get; set; }


    /// <summary>Gets or sets the name of this calculation step.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets the configuration section name.</summary>
    public string SectionName => "Calculations";

    /// <summary>Gets the service type discriminator.</summary>
    public string ServiceType => "Step";

    /// <summary>Gets the service option type discriminator. Not applicable for steps.</summary>
    public string? ServiceOptionType => null;

    /// <summary>
    /// Gets or sets the operation type name from <see cref="CalculationOperationTypes"/>
    /// (e.g., "Add", "Sum", "RowNumber").
    /// </summary>
    [ValuesFrom(typeof(CalculationOperationTypes))]
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ordinal position of this step within the parent calculation entity.
    /// Steps execute in ascending ordinal order.
    /// </summary>
    public int Ordinal { get; set; }

    /// <summary>
    /// Gets or sets the alias name assigned to this step's output.
    /// Subsequent steps can reference this alias as an input.
    /// </summary>
    public string OutputAlias { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the fields referenced by this step (GROUP BY and ORDER BY roles).
    /// Composed from <c>calc.CalculationStepField</c> by the keystone cascade. Callers filter by
    /// <see cref="CalculationStepFieldConfiguration.StepFieldRole"/>.
    /// </summary>
#pragma warning disable MA0016
    public List<CalculationStepFieldConfiguration> Fields { get; set; } = [];

    /// <summary>
    /// Gets or sets the operands bound to this step. Composed from <c>calc.CalculationStepOperand</c>
    /// by the keystone cascade; cascade-saved on insert.
    /// </summary>
    public List<CalculationStepOperandConfiguration> Operands { get; set; } = [];
#pragma warning restore MA0016
}
