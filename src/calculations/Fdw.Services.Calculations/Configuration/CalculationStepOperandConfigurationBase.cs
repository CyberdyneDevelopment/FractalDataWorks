using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;

namespace Fdw.Services.Calculations.Configuration;

/// <summary>
/// The columns every step operand carries. An operand resolves its value from one of three sources:
/// an input alias, a prior step alias, or a literal value.
/// </summary>
/// <remarks>
/// Why a base: an operand belongs to its step, and each implementation keeps its steps in its own
/// table (FDW-944), so each implementation's step has its own operand type deriving from this one.
/// </remarks>
[ExcludeFromCodeCoverage]
public abstract class CalculationStepOperandConfigurationBase : IGenericConfiguration
{
    /// <summary>Gets or sets the unique identifier for this operand.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name of this operand (matches the operation parameter name).</summary>
    public string Name { get; set; } = string.Empty;

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
    /// References a prior <see cref="CalculationStepConfigurationBase.OutputAlias"/>.
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
