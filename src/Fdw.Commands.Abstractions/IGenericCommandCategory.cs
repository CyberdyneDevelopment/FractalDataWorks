using Fdw.Collections;

namespace Fdw.Commands.Abstractions;

/// <summary>
/// Interface for command categories that define command behavior and requirements.
/// </summary>
/// <remarks>
/// Command categories determine execution characteristics such as transaction requirements,
/// streaming support, and validation rules. Categories are implemented as type options
/// for extensibility and type safety.
/// </remarks>
public interface IGenericCommandCategory : ITypeOption<int, CommandCategoryBase>
{
    /// <summary>
    /// Gets whether commands in this category require transaction support.
    /// </summary>
    /// <value>True if commands must execute within a transaction context.</value>
    bool RequiresTransaction { get; }

    /// <summary>
    /// Gets whether commands in this category support streaming execution.
    /// </summary>
    /// <value>True if commands can stream results rather than loading all at once.</value>
    bool SupportsStreaming { get; }

    /// <summary>
    /// Gets whether commands in this category can be cached.
    /// </summary>
    /// <value>True if command results can be cached for performance.</value>
    bool IsCacheable { get; }

    /// <summary>
    /// Gets whether commands in this category modify data.
    /// </summary>
    /// <value>True if commands perform mutations (INSERT, UPDATE, DELETE).</value>
    bool IsMutation { get; }

    /// <summary>
    /// Gets the execution priority for this category.
    /// </summary>
    /// <value>Higher values indicate higher priority for execution ordering.</value>
    int ExecutionPriority { get; }
}