using Fdw.Collections;

namespace Fdw.Commands.Abstractions;

/// <summary>
/// Base class for command categories.
/// </summary>
/// <remarks>
/// Inherit from this class to define specific command categories with
/// their execution characteristics and requirements.
/// </remarks>
public abstract class CommandCategoryBase : TypeOptionBase<int, CommandCategoryBase>, IGenericCommandCategory
{
    /// <inheritdoc/>
    public bool RequiresTransaction { get; }

    /// <inheritdoc/>
    public bool SupportsStreaming { get; }

    /// <inheritdoc/>
    public bool IsCacheable { get; }

    /// <inheritdoc/>
    public bool IsMutation { get; }

    /// <inheritdoc/>
    public int ExecutionPriority { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandCategoryBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this category.</param>
    /// <param name="name">The name of this category.</param>
    /// <param name="requiresTransaction">Whether commands in this category require transactions.</param>
    /// <param name="supportsStreaming">Whether commands in this category support streaming.</param>
    /// <param name="isCacheable">Whether command results can be cached.</param>
    /// <param name="isMutation">Whether commands modify data.</param>
    /// <param name="executionPriority">The execution priority (default 50).</param>
    protected CommandCategoryBase(
        int id,
        string name,
        bool requiresTransaction,
        bool supportsStreaming,
        bool isCacheable,
        bool isMutation,
        int executionPriority = 50) : base(id, name)
    {
        RequiresTransaction = requiresTransaction;
        SupportsStreaming = supportsStreaming;
        IsCacheable = isCacheable;
        IsMutation = isMutation;
        ExecutionPriority = executionPriority;
    }
}