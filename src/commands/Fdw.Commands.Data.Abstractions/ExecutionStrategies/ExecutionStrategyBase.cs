namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// Base class for composite command execution strategies.
/// Replaces CompositeExecutionStrategy enum to add behavior and eliminate switch statements.
/// </summary>
/// <remarks>
/// <para>
/// Each strategy knows its own execution characteristics, enabling polymorphic behavior
/// without switch statements when executing composite commands.
/// </para>
/// <para>
/// Properties are set in constructor so TypeCollection source generator can read them
/// without instantiation.
/// </para>
/// </remarks>
public abstract class ExecutionStrategyBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionStrategyBase"/> class.
    /// </summary>
    /// <param name="id">Unique identifier for this strategy.</param>
    /// <param name="name">Name of the strategy (must match TypeOption attribute).</param>
    /// <param name="isSequential">Whether this strategy executes commands sequentially.</param>
    /// <param name="stopOnFailure">Whether this strategy stops execution on first failure.</param>
    /// <param name="supportsParallel">Whether this strategy supports parallel execution.</param>
    protected ExecutionStrategyBase(int id, string name, bool isSequential, bool stopOnFailure, bool supportsParallel)
    {
        Id = id;
        Name = name;
        IsSequential = isSequential;
        StopOnFailure = stopOnFailure;
        SupportsParallel = supportsParallel;
    }

    /// <summary>
    /// Gets the unique identifier for this strategy.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Gets the name of this strategy.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets a value indicating whether this strategy executes commands sequentially.
    /// No switch statements needed - direct property access!
    /// </summary>
    public bool IsSequential { get; }

    /// <summary>
    /// Gets a value indicating whether this strategy stops execution on first failure.
    /// No switch statements needed - direct property access!
    /// </summary>
    public bool StopOnFailure { get; }

    /// <summary>
    /// Gets a value indicating whether this strategy supports parallel execution.
    /// No switch statements needed - direct property access!
    /// </summary>
    public bool SupportsParallel { get; }
}
