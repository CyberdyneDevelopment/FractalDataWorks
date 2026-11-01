using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Commands.Abstractions;

/// <summary>
/// Interface for command type definitions with metadata and capabilities.
/// </summary>
/// <remarks>
/// Command types provide metadata about specific command implementations,
/// including their supported translators and execution characteristics.
/// Types are registered via TypeCollections for discovery and routing.
/// </remarks>
public interface IGenericCommandType : ITypeOption<int, CommandTypeBase>
{
    /// <summary>
    /// Gets the command category for this type.
    /// </summary>
    /// <value>The category that defines behavior and requirements.</value>
    IGenericCommandCategory CommandCategory { get; }

    /// <summary>
    /// Gets the supported translator types for this command.
    /// </summary>
    /// <value>Collection of translator types that can process this command.</value>
    IReadOnlyCollection<ITranslatorType> SupportedTranslators { get; }

    /// <summary>
    /// Gets whether this command type supports batching.
    /// </summary>
    /// <value>True if multiple commands can be batched together.</value>
    bool SupportsBatching { get; }

    /// <summary>
    /// Gets whether this command type supports pipelining.
    /// </summary>
    /// <value>True if commands can be pipelined for performance.</value>
    bool SupportsPipelining { get; }

    /// <summary>
    /// Gets the maximum batch size for this command type.
    /// </summary>
    /// <value>Maximum number of commands that can be batched together.</value>
    int MaxBatchSize { get; }
}