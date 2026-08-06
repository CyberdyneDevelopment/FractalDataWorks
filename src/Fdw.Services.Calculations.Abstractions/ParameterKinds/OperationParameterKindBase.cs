using Fdw.Collections;

namespace Fdw.Services.Calculations.Abstractions;

/// <summary>
/// Base class for operation parameter kinds (CRTP pattern).
/// Each subclass represents a distinct shape of parameter a calculation operation can accept.
/// </summary>
public abstract class OperationParameterKindBase : TypeOptionBase<int, OperationParameterKindBase>, IOperationParameterKind
{
    /// <summary>
    /// Gets a human-readable description of this parameter kind.
    /// </summary>
    public new string Description { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OperationParameterKindBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this parameter kind.</param>
    /// <param name="name">The name of this parameter kind.</param>
    /// <param name="description">A human-readable description of this parameter kind.</param>
    protected OperationParameterKindBase(int id, string name, string description)
        : base(id, name)
    {
        Description = description;
    }
}
