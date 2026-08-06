using System;
using System.Collections.Generic;
using Fdw.Abstractions;
using Fdw.Collections;

namespace Fdw.Commands.Abstractions;

/// <summary>
/// Base class for command type definitions.
/// </summary>
/// <remarks>
/// <para>
/// Command types provide metadata about command implementations and their capabilities.
/// Inherit from this class to define specific command types that will be discovered
/// by the TypeCollection source generator.
/// </para>
/// <para>
/// Command types are static singletons (e.g., SqlQueryCommandType.Instance) that define:
/// <list type="bullet">
/// <item>Command category and execution characteristics</item>
/// <item>Supported translators for this command type</item>
/// <item>Batching and pipelining capabilities</item>
/// <item>The command interface type this represents</item>
/// </list>
/// </para>
/// <para>
/// This pattern follows the same approach as ServiceTypeBase → ConnectionTypeBase.
/// </para>
/// </remarks>
public abstract class CommandTypeBase : TypeOptionBase<int, CommandTypeBase>, IGenericCommandType
{
    /// <inheritdoc/>
    public IGenericCommandCategory CommandCategory { get; }

    /// <inheritdoc/>
    public IReadOnlyCollection<ITranslatorType> SupportedTranslators { get; }

    /// <inheritdoc/>
    public bool SupportsBatching { get; }

    /// <inheritdoc/>
    public bool SupportsPipelining { get; }

    /// <inheritdoc/>
    public int MaxBatchSize { get; }

    /// <summary>
    /// Gets the command interface type that this command type represents.
    /// </summary>
    /// <value>
    /// The interface type (e.g., typeof(IQueryCommand), typeof(IMutationCommand))
    /// that defines the contract for this command type.
    /// </value>
    /// <remarks>
    /// This enables runtime type checking and proper routing of command executions
    /// to the appropriate handlers and translators.
    /// </remarks>
    public virtual Type CommandInterfaceType => typeof(IGenericCommand);

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandTypeBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this command type.</param>
    /// <param name="name">The name of this command type.</param>
    /// <param name="description">The description of this command type.</param>
    /// <param name="category">The command category.</param>
    /// <param name="supportedTranslators">The supported translator types.</param>
    /// <param name="supportsBatching">Whether batching is supported (default false).</param>
    /// <param name="supportsPipelining">Whether pipelining is supported (default false).</param>
    /// <param name="maxBatchSize">Maximum batch size (default 1).</param>
    protected CommandTypeBase(
        int id,
        string name,
        string description,
        IGenericCommandCategory category,
        IReadOnlyCollection<ITranslatorType> supportedTranslators,
        bool supportsBatching = false,
        bool supportsPipelining = false,
        int maxBatchSize = 1)
        : base(id, name, description)
    {
        CommandCategory = category;
        SupportedTranslators = supportedTranslators;
        SupportsBatching = supportsBatching;
        SupportsPipelining = supportsPipelining;
        MaxBatchSize = maxBatchSize;
    }
}