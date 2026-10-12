using System;
using System.Collections.Generic;
using Fdw.Results;

namespace Fdw.Processors;

/// <summary>
/// Composes multiple processors into a chainable pipeline.
/// Supports Railway-Oriented error handling - stops on first failure.
/// </summary>
/// <typeparam name="TCommand">The command type being processed through the chain.</typeparam>
/// <remarks>
/// <para>
/// ProcessorChain enables composition of multiple processors that operate on the same
/// command type but may have different context types. Each processor is invoked in
/// sequence, passing the output of one to the input of the next.
/// </para>
/// <para>
/// The chain follows Railway-Oriented Programming: if any processor fails,
/// the chain stops immediately and returns that failure. Successful results
/// flow through to the next processor.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var chain = new ProcessorChain&lt;HttpRequestMessage&gt;()
///     .Add(signingProcessor, signingContext)
///     .AddIf(config.Encryption.Enabled, encryptionProcessor, encryptionContext)
///     .Add(authProcessor, authContext);
/// 
/// var result = chain.Execute(request);
/// if (!result.IsSuccess)
/// {
///     // Handle failure - chain stopped at first error
/// }
/// </code>
/// </example>
public sealed class ProcessorChain<TCommand>
{
    private readonly List<Func<TCommand, IGenericResult<TCommand>>> _processors = [];

    /// <summary>
    /// Adds a processor to the chain.
    /// </summary>
    /// <typeparam name="TContext">The processor's context type.</typeparam>
    /// <param name="processor">The processor to add.</param>
    /// <param name="context">The context to use when this processor executes.</param>
    /// <returns>This chain for fluent method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when processor is null.</exception>
    public ProcessorChain<TCommand> Add<TContext>(
        IProcessor<TCommand, TContext> processor,
        TContext context)
    {
        if (processor == null)
            throw new ArgumentNullException(nameof(processor));

        _processors.Add(cmd => processor.Process(cmd, context));
        return this;
    }

    /// <summary>
    /// Conditionally adds a processor to the chain.
    /// </summary>
    /// <typeparam name="TContext">The processor's context type.</typeparam>
    /// <param name="condition">If true, the processor is added; if false, it is skipped.</param>
    /// <param name="processor">The processor to conditionally add.</param>
    /// <param name="context">The context to use when this processor executes.</param>
    /// <returns>This chain for fluent method chaining.</returns>
    /// <remarks>
    /// Use this method when a processor should only run under certain conditions,
    /// such as when encryption is enabled or when running in a specific environment.
    /// </remarks>
    public ProcessorChain<TCommand> AddIf<TContext>(
        bool condition,
        IProcessor<TCommand, TContext> processor,
        TContext context)
    {
        if (condition)
        {
            if (processor == null)
                throw new ArgumentNullException(nameof(processor));

            _processors.Add(cmd => processor.Process(cmd, context));
        }
        return this;
    }

    /// <summary>
    /// Gets the number of processors in the chain.
    /// </summary>
    public int Count => _processors.Count;

    /// <summary>
    /// Executes all processors in sequence on the provided command.
    /// Stops and returns failure on the first error.
    /// </summary>
    /// <param name="command">The initial command to process.</param>
    /// <returns>
    /// The fully processed command if all processors succeed,
    /// or the first failure encountered.
    /// </returns>
    /// <remarks>
    /// <para>
    /// If the chain is empty, returns success with the original command unchanged.
    /// </para>
    /// <para>
    /// Each processor receives the output from the previous processor.
    /// The final result contains either the fully processed command or
    /// the failure message from the first processor that failed.
    /// </para>
    /// </remarks>
    public IGenericResult<TCommand> Execute(TCommand command)
    {
        var result = GenericResult<TCommand>.Success(command);

        foreach (var process in _processors)
        {
            if (!result.IsSuccess || result.Value == null)
            {
                return result;
            }

            result = process(result.Value);
        }

        return result;
    }
}
