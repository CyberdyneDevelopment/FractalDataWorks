using Fdw.Collections;

namespace Fdw.Services.Calculations.Abstractions;

/// <summary>
/// Represents a kind of parameter that a calculation operation accepts.
/// Describes the shape of the parameter (scalar, field, field array, or data set).
/// </summary>
public interface IOperationParameterKind : ITypeOption<int, OperationParameterKindBase>
{
    /// <summary>
    /// Gets a human-readable description of this parameter kind.
    /// </summary>
    string Description { get; }
}
