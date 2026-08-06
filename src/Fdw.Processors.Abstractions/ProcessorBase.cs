using System;
using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Results;

namespace Fdw.Processors;

/// <summary>
/// Base class for synchronous processors using the CRTP pattern.
/// </summary>
/// <typeparam name="TCommand">The type being processed.</typeparam>
/// <typeparam name="TContext">The processing context.</typeparam>
/// <typeparam name="TBase">The concrete base type (CRTP self-reference).</typeparam>
/// <remarks>
/// <para>
/// Processors are stateless TypeOptions. All processing state must come from TContext.
/// Do not add mutable instance fields to processor implementations.
/// </para>
/// <para>
/// The CRTP (Curiously Recurring Template Pattern) enables the TypeCollection
/// source generator to create strongly-typed lookups.
/// </para>
/// </remarks>
public abstract class ProcessorBase<TCommand, TContext, TBase>
    : TypeOptionBase<int, TBase>, IProcessor<TCommand, TContext>
    where TBase : ProcessorBase<TCommand, TContext, TBase>
{
    /// <summary>
    /// Initializes a new instance for the Empty/NotFound sentinel.
    /// Used by the source generator to create the NotFound() processor.
    /// </summary>
    protected ProcessorBase()
        : base(0, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty)
    {
        RequiredProperties = Array.Empty<string>();
    }

    /// <summary>
    /// Initializes a new instance with the specified metadata.
    /// </summary>
    /// <param name="name">The processor identifier (e.g., "SqlAuth", "Bearer").</param>
    /// <param name="displayName">Human-readable name for UI display.</param>
    /// <param name="description">Description of what this processor does.</param>
    /// <param name="requiredProperties">Required context property names for validation.</param>
    /// <param name="category">Category for grouping processors (default: "Processor").</param>
    protected ProcessorBase(
        string name,
        string displayName,
        string description,
        IReadOnlyList<string> requiredProperties,
        string category = "Processor")
        : base(
            GenerateIdFromName(name),
            name,
            $"Processor:{category}:{name}",
            displayName,
            description,
            category)
    {
        RequiredProperties = requiredProperties;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> RequiredProperties { get; }

    /// <inheritdoc />
    public bool IsEmpty => string.IsNullOrEmpty(Name);

    /// <inheritdoc />
    /// <remarks>
    /// The default implementation returns success. Override in derived classes
    /// to validate context properties against <see cref="RequiredProperties"/>.
    /// </remarks>
    public virtual IGenericResult Validate(TContext context) => GenericResult.Success();

    /// <inheritdoc />
    public abstract IGenericResult<TCommand> Process(TCommand command, TContext context);

    /// <summary>
    /// Generates a stable ID from the processor name using FNV-1a hash.
    /// Matches the pattern used by TypeOptionExtensionGenerator for consistency.
    /// </summary>
    /// <param name="name">The processor name to hash.</param>
    /// <returns>A positive integer ID derived from the name.</returns>
    protected static int GenerateIdFromName(string name)
    {
        unchecked
        {
            const int FnvPrime = 0x01000193;
            const int FnvOffsetBasis = unchecked((int)0x811C9DC5);

            int hash = FnvOffsetBasis;
            foreach (char c in name)
            {
                hash ^= c;
                hash *= FnvPrime;
            }
            return hash & 0x7FFFFFFF;
        }
    }
}
