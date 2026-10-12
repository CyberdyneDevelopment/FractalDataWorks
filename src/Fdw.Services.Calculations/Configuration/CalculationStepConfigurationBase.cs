using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Calculations.Configuration;

/// <summary>
/// The columns every calculation step carries, whichever implementation owns it.
/// A step references a <see cref="CalculationOperationTypes"/> operation and produces an output alias
/// that subsequent steps can consume.
/// </summary>
/// <remarks>
/// Why a base and not one shared type: a step belongs to the implementation that runs it, so each
/// implementation keeps its steps in its own table, held by its RowId (FDW-944). A typed child's table
/// is its type's ConfigurationCommand container, so each implementation has its own step type deriving
/// from this one, carrying its own field and operand types.
/// </remarks>
[ExcludeFromCodeCoverage]
public abstract class CalculationStepConfigurationBase : IGenericConfiguration
{
    /// <summary>Gets or sets the unique identifier for this calculation step.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name of this calculation step.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the operation type name from <see cref="CalculationOperationTypes"/>
    /// (e.g., "Add", "Sum", "RowNumber").
    /// </summary>
    [ValuesFrom(typeof(CalculationOperationTypes))]
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ordinal position of this step within the parent calculation.
    /// Steps execute in ascending ordinal order.
    /// </summary>
    public int Ordinal { get; set; }

    /// <summary>
    /// Gets or sets the alias name assigned to this step's output.
    /// Subsequent steps can reference this alias as an input.
    /// </summary>
    public string OutputAlias { get; set; } = string.Empty;
}
