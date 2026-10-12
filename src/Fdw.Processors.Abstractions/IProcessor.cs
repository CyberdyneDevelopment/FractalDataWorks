using System.Collections.Generic;
using Fdw.Results;

namespace Fdw.Processors;

/// <summary>
/// Core interface for synchronous command processors.
/// Processors transform a command using the provided context.
/// </summary>
/// <typeparam name="TCommand">The type being processed (e.g., StringBuilder, HttpRequestMessage).</typeparam>
/// <typeparam name="TContext">The processing context (typically a readonly record struct with config, secrets, etc.).</typeparam>
/// <remarks>
/// <para>
/// Processors are stateless TypeOptions discovered at compile time via source generation.
/// All processing state must come from the TContext parameter.
/// </para>
/// <para>
/// The Process method transforms TCommand in-place or returns a new instance.
/// Validation should be called before processing to ensure context is valid.
/// </para>
/// </remarks>
public interface IProcessor<TCommand, TContext>
{
    /// <summary>
    /// Gets a value indicating whether this is the Empty/NotFound sentinel.
    /// </summary>
    /// <remarks>
    /// The source generator creates an Empty processor for each TypeCollection.
    /// Check this property after lookups to detect invalid processor names.
    /// </remarks>
    bool IsEmpty { get; }

    /// <summary>
    /// Gets the list of required context properties for this processor.
    /// Used for validation before processing.
    /// </summary>
    /// <remarks>
    /// Property names should match configuration property names (e.g., "Username", "SecretKeyName").
    /// The base class Validate method checks these properties are present and non-empty.
    /// </remarks>
    IReadOnlyList<string> RequiredProperties { get; }

    /// <summary>
    /// Validates that the context has all required properties for this processor.
    /// </summary>
    /// <param name="context">The processing context to validate.</param>
    /// <returns>Success if valid, Failure with error messages if not.</returns>
    /// <remarks>
    /// Call this before Process to get clear error messages about missing configuration.
    /// The default implementation checks RequiredProperties against the context.
    /// </remarks>
    IGenericResult Validate(TContext context);

    /// <summary>
    /// Processes the command using the provided context.
    /// </summary>
    /// <param name="command">The command to process (may be modified in-place).</param>
    /// <param name="context">The processing context (config, secrets, credentials, etc.).</param>
    /// <returns>The processed command wrapped in a result, or failure information.</returns>
    /// <remarks>
    /// <para>
    /// Processors should call Validate internally before processing.
    /// If validation fails, return the validation failure result.
    /// </para>
    /// <para>
    /// The returned command may be the same instance (modified) or a new instance.
    /// Callers should always use the returned value, not the original.
    /// </para>
    /// </remarks>
    IGenericResult<TCommand> Process(TCommand command, TContext context);
}
