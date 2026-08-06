using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Calculations.Configuration;

/// <summary>
/// ManagedConfiguration for the <c>calc.CalculationStepOperand</c> table.
/// Represents a single operand bound to a calculation step. Each operand resolves
/// its value from one of three sources: an input alias, a prior step alias, or a literal value.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Calculation")]
public partial class CalculationStepOperandConfiguration : IGenericConfiguration
{

    /// <summary>Gets or sets the unique identifier for this operand.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the parent CalculationStep's logical Id (FK to calc.CalculationStep.Id).</summary>
    public Guid CalculationStepId { get; set; }


    /// <summary>Gets or sets the name of this operand (matches the operation parameter name).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets the configuration section name.</summary>
    public string SectionName => "Calculations";

    /// <summary>Gets the service type discriminator.</summary>
    public string ServiceType => "StepOperand";

    /// <summary>Gets the service option type discriminator. Not applicable for operands.</summary>
    public string? ServiceOptionType => null;

    /// <summary>
    /// Gets or sets the operand source type.
    /// Valid values: <c>"Input"</c> (from a calculation input), <c>"StepReference"</c>
    /// (from a prior step's output alias), <c>"Literal"</c> (an inline scalar value).
    /// </summary>
    public string OperandType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the input alias when <see cref="OperandType"/> is <c>"Input"</c>.
    /// References a <see cref="Abstractions.CalculationInput"/> by alias.
    /// </summary>
    public string? InputAlias { get; set; }

    /// <summary>
    /// Gets or sets the step alias when <see cref="OperandType"/> is <c>"StepReference"</c>.
    /// References a prior <see cref="CalculationStepConfiguration.OutputAlias"/>.
    /// </summary>
    public string? StepAlias { get; set; }

    /// <summary>
    /// Gets or sets the field name to extract from the referenced input or step output.
    /// </summary>
    public string? FieldName { get; set; }

    /// <summary>
    /// Gets or sets the literal scalar value when <see cref="OperandType"/> is <c>"Literal"</c>.
    /// Stored as a string and converted at execution time.
    /// </summary>
    public string? LiteralValue { get; set; }

    /// <summary>
    /// Gets or sets the ordinal position of this operand within the parent step.
    /// </summary>
    public int Ordinal { get; set; }

}
