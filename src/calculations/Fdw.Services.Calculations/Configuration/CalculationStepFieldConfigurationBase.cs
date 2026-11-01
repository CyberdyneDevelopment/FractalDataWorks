using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;

namespace Fdw.Services.Calculations.Configuration;

/// <summary>
/// The columns every step field carries: a field a step groups or orders by, with a role discriminator
/// so one table covers both GROUP BY and ORDER BY semantics per step.
/// </summary>
/// <remarks>
/// Why a base: a step field belongs to its step, and each implementation keeps its steps in its own
/// table (FDW-944), so each implementation's step has its own field type deriving from this one.
/// </remarks>
[ExcludeFromCodeCoverage]
public abstract class CalculationStepFieldConfigurationBase : IGenericConfiguration
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the field role/name. Mirrors <see cref="StepFieldRole"/> to satisfy IGenericConfiguration.</summary>
    public string Name { get => StepFieldRole; set => StepFieldRole = value; }

    /// <summary>Gets or sets the role of this field within the step: 'GroupBy' or 'OrderBy'.</summary>
    public string StepFieldRole { get; set; } = string.Empty;

    /// <summary>Gets or sets the ordinal position within the role.</summary>
    public int Ordinal { get; set; }

    /// <summary>Gets or sets the sort direction for OrderBy fields ('Asc' or 'Desc'). Null for GroupBy fields.</summary>
    public string? Direction { get; set; }
}
