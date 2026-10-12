using Fdw.Collections;

namespace Fdw.Services.Calculations.Abstractions;

/// <summary>
/// Base class for operation parameter kinds (CRTP pattern).
/// Each subclass represents a distinct shape of parameter a calculation operation can accept.
/// </summary>
public abstract class OperationParameterKindBase : TypeOptionBase<OperationParameterKindBase>, IOperationParameterKind
{
    /// <summary>
    /// Gets a human-readable description of this parameter kind.
    /// </summary>
    public new string Description { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OperationParameterKindBase"/> class.
    /// </summary>
    /// <param name="name">The name of this parameter kind.</param>
    /// <param name="description">A human-readable description of this parameter kind.</param>
    protected OperationParameterKindBase(string name, string description)
        : base(name)
    {
        Description = description;
    }
}
