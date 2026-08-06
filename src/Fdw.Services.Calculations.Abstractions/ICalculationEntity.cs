using System;
using System.Collections.Generic;
using Fdw.Configuration;

namespace Fdw.Services.Calculations.Abstractions;

/// <summary>
/// Represents a configured calculation entity — a named, typed computation with declared inputs and an output specification.
/// </summary>
public interface ICalculationEntity
{
    /// <summary>Gets the unique identifier of this calculation entity.</summary>
    Guid Id { get; }

    /// <summary>Gets the name of this calculation entity.</summary>
    string Name { get; }

    /// <summary>Gets an optional description of what this calculation does.</summary>
    string? Description { get; }

    /// <summary>Gets the name of the calculation entity type (e.g. "ScriptedCalculation").</summary>
    string CalculationEntityType { get; }

    /// <summary>
    /// Gets the name of the <c>CalculationSourceTypes</c> option that wrote this entity (provenance —
    /// e.g. "Configuration" for the built-in writer). Not a pointer to codified behavior.
    /// </summary>
    string CalculationSource { get; }

    /// <summary>Gets the declared inputs for this calculation.</summary>
    IReadOnlyList<CalculationInput> Inputs { get; }

    /// <summary>
    /// Gets the calculation steps (each carrying its Fields + Operands), composed from the aggregate.
    /// </summary>
    /// <remarks>
    /// Why: typed as <see cref="IGenericConfiguration"/> because the concrete step config
    /// (CalculationStepConfiguration) lives in the implementation package and cannot be referenced from
    /// this abstraction. At runtime each element is a CalculationStepConfiguration. A strongly-typed
    /// runtime step model can land with the step-execution feature.
    /// </remarks>
    IReadOnlyList<IGenericConfiguration> Steps { get; }

    /// <summary>Gets the output specification describing where to write the result.</summary>
    CalculationOutputSpec Output { get; }

    /// <summary>Gets a value indicating whether this calculation entity is active.</summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Gets the type-specific configuration loaded from the entity's typed configuration table.
    /// Null when the entity type has no typed configuration record or it was not loaded.
    /// </summary>
    IGenericConfiguration? TypedConfiguration { get; }
}
