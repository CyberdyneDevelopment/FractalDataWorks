using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Results;

namespace Fdw.Processors;

/// <summary>
/// Base class for asynchronous processors using the CRTP pattern.
/// </summary>
/// <typeparam name="TCommand">The type being processed.</typeparam>
/// <typeparam name="TContext">The processing context.</typeparam>
/// <typeparam name="TBase">The concrete base type (CRTP self-reference).</typeparam>
/// <remarks>
/// <para>
/// Use this base class when processing requires async operations such as:
/// token acquisition, secret resolution, or external API calls.
/// </para>
/// <para>
/// Async processors are stateless TypeOptions, same as sync processors.
/// All state must come from TContext.
/// </para>
/// </remarks>
public abstract class AsyncProcessorBase<TCommand, TContext, TBase>
    : TypeOptionBase<int, TBase>, IAsyncProcessor<TCommand, TContext>
    where TBase : AsyncProcessorBase<TCommand, TContext, TBase>
{
    /// <summary>
    /// Initializes a new instance for the Empty/NotFound sentinel.
    /// </summary>
    protected AsyncProcessorBase()
        : base(0, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty)
    {
        RequiredProperties = Array.Empty<string>();
    }

    /// <summary>
    /// Initializes a new instance with the specified metadata.
    /// </summary>
    /// <param name="name">The processor identifier.</param>
    /// <param name="displayName">Human-readable name for UI display.</param>
    /// <param name="description">Description of what this processor does.</param>
    /// <param name="requiredProperties">Required context property names for validation.</param>
    /// <param name="category">Category for grouping processors (default: "Processor").</param>
    protected AsyncProcessorBase(
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
    public virtual IGenericResult Validate(TContext context) => GenericResult.Success();

    /// <inheritdoc />
    public abstract Task<IGenericResult<TCommand>> Process(
        TCommand command,
        TContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a stable ID from the processor name using FNV-1a hash.
    /// </summary>
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
