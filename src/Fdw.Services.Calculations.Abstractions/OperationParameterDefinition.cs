using Fdw.Configuration;

namespace Fdw.Services.Calculations.Abstractions;

/// <summary>
/// Defines a single parameter that a <see cref="ICalculationOperation"/> accepts.
/// Each operation declares its parameters via an <see cref="System.Collections.Generic.IReadOnlyList{T}"/>
/// of these definitions, enabling UI-driven binding and validation.
/// </summary>
public sealed class OperationParameterDefinition
{
    /// <summary>
    /// Gets the parameter name used as a dictionary key when passing values to
    /// <see cref="ICalculationOperation.Calculate"/>.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the kind of value this parameter expects (Scalar, Field, FieldArray, or DataSet).
    /// Must match a name in <see cref="OperationParameterKinds"/>.
    /// </summary>
    [ValuesFrom(typeof(OperationParameterKinds))]
    public string Kind { get; init; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether this parameter is required.
    /// When <see langword="true"/>, the parameter must be supplied for the operation to execute.
    /// </summary>
    public bool IsRequired { get; init; } = true;

    /// <summary>
    /// Gets the human-readable display name shown in the UI for this parameter.
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// Gets optional help text providing additional guidance for this parameter.
    /// </summary>
    public string? HelpText { get; init; }
}
