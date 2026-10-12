using System;
using System.Collections.Generic;
using FluentValidation.Results;
using Fdw.Results;

namespace Fdw.Services.Abstractions.Commands;


/// <summary>
/// Interface for command builders in the Fdw framework.
/// Provides a fluent interface for constructing data commands with proper validation and configuration.
/// </summary>
/// <typeparam name="TCommand">The type of command this builder creates.</typeparam>
/// <remarks>
/// Command builders abstract the complexity of command construction and provide a consistent
/// interface for creating properly configured data commands. They enable fluent configuration
/// and validation of command parameters, metadata, and execution options.
/// </remarks>
public interface IGenericCommandBuilder<TCommand>
{
    /// <summary>
    /// Sets the target resource (table, collection, endpoint) for the command.
    /// </summary>
    /// <param name="target">The target resource name.</param>
    /// <returns>The command builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="target"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="target"/> is empty or whitespace.</exception>
    /// <remarks>
    /// The target specifies which data structure the command should operate on.
    /// This may be a table name, collection name, API endpoint, or other resource identifier
    /// depending on the data provider and command type.
    /// </remarks>
    IGenericCommandBuilder<TCommand> WithTarget(string target);

    /// <summary>
    /// Sets the timeout for command execution.
    /// </summary>
    /// <param name="timeout">The maximum time to wait for command execution.</param>
    /// <returns>The command builder instance for method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="timeout"/> is negative.</exception>
    /// <remarks>
    /// Command timeouts override provider default timeouts and enable fine-grained
    /// control over execution time limits for specific operations.
    /// </remarks>
    IGenericCommandBuilder<TCommand> WithTimeout(TimeSpan timeout);

    /// <summary>
    /// Adds a parameter to the command.
    /// </summary>
    /// <param name="name">The parameter name.</param>
    /// <param name="value">The parameter value.</param>
    /// <returns>The command builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty or whitespace.</exception>
    /// <remarks>
    /// Parameters provide input data for command execution. Parameter names should follow
    /// consistent naming conventions and be unique within the command scope.
    /// </remarks>
    IGenericCommandBuilder<TCommand> WithParameter(string name, object? value);

    /// <summary>
    /// Adds multiple parameters to the command.
    /// </summary>
    /// <param name="parameters">A dictionary of parameter names and values.</param>
    /// <returns>The command builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="parameters"/> is null.</exception>
    /// <remarks>
    /// This method provides a convenient way to add multiple parameters at once.
    /// Existing parameters with the same names will be overwritten with new values.
    /// </remarks>
    IGenericCommandBuilder<TCommand> WithParameters(IReadOnlyDictionary<string, object?> parameters);

    /// <summary>
    /// Adds metadata to the command.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <returns>The command builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is empty or whitespace.</exception>
    /// <remarks>
    /// Metadata provides additional context and hints for command execution.
    /// Common metadata includes caching directives, optimization hints, and execution preferences.
    /// </remarks>
    IGenericCommandBuilder<TCommand> WithMetadata(string key, object value);

    /// <summary>
    /// Adds multiple metadata entries to the command.
    /// </summary>
    /// <param name="metadata">A dictionary of metadata keys and values.</param>
    /// <returns>The command builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="metadata"/> is null.</exception>
    /// <remarks>
    /// This method provides a convenient way to add multiple metadata entries at once.
    /// Existing metadata with the same keys will be overwritten with new values.
    /// </remarks>
    IGenericCommandBuilder<TCommand> WithMetadata(IReadOnlyDictionary<string, object> metadata);

    /// <summary>
    /// Specifies the expected result type for the command.
    /// </summary>
    /// <param name="resultType">The expected result type.</param>
    /// <returns>The command builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="resultType"/> is null.</exception>
    /// <remarks>
    /// Setting the expected result type helps data providers prepare appropriate
    /// result handling and type conversion logic before executing the command.
    /// </remarks>
    IGenericCommandBuilder<TCommand> WithExpectedResultType(Type resultType);

    /// <summary>
    /// Specifies the expected result type for the command using generic type parameter.
    /// </summary>
    /// <typeparam name="TResult">The expected result type.</typeparam>
    /// <returns>The command builder instance for method chaining.</returns>
    /// <remarks>
    /// This method provides type-safe specification of expected result types
    /// without the need for runtime Type objects.
    /// </remarks>
    IGenericCommandBuilder<TCommand> WithExpectedResultType<TResult>();

    /// <summary>
    /// Marks the command as data-modifying or read-only.
    /// </summary>
    /// <param name="isDataModifying">True if the command modifies data; false if it's read-only.</param>
    /// <returns>The command builder instance for method chaining.</returns>
    /// <remarks>
    /// This flag helps data providers determine appropriate connection handling,
    /// transaction requirements, and caching behavior for the command.
    /// </remarks>
    IGenericCommandBuilder<TCommand> WithDataModifying(bool isDataModifying);

    /// <summary>
    /// Validates the current builder state without building the command.
    /// </summary>
    /// <returns>A result indicating whether the builder state is valid for command creation.</returns>
    /// <remarks>
    /// This method enables early validation of builder configuration before
    /// attempting to build the actual command. Useful for providing user feedback
    /// during interactive command construction.
    /// </remarks>
    IGenericResult ValidateBuilder();

    /// <summary>
    /// Builds the command using the current builder configuration.
    /// </summary>
    /// <returns>
    /// A result containing the constructed command if successful, or error information if building failed.
    /// </returns>
    /// <remarks>
    /// This method creates the final command instance using all configured parameters,
    /// metadata, and options. The builder validates the configuration and returns
    /// appropriate error information if the command cannot be constructed.
    /// </remarks>
    IGenericResult<TCommand> Build();

    /// <summary>
    /// Resets the builder to its initial state, clearing all configured values.
    /// </summary>
    /// <returns>The command builder instance for method chaining.</returns>
    /// <remarks>
    /// This method enables builder reuse for creating multiple similar commands
    /// without needing to create new builder instances.
    /// </remarks>
    IGenericCommandBuilder<TCommand> Reset();
}

/// <summary>
/// Interface for generic command builders that create typed commands.
/// Extends the base command builder with strongly-typed result handling.
/// </summary>
/// <typeparam name="TCommand">The type of command this builder creates.</typeparam>
/// <typeparam name="TResult">The expected result type for commands created by this builder.</typeparam>
/// <remarks>
/// Typed command builders provide compile-time type safety for both command construction
/// and result type specification, eliminating runtime type checking and casting.
/// </remarks>
public interface IGenericCommandBuilder<TCommand, TResult> : IGenericCommandBuilder<TCommand>
{
    /// <summary>
    /// Builds a typed command using the current builder configuration.
    /// </summary>
    /// <returns>
    /// A result containing the constructed typed command if successful, or error information if building failed.
    /// </returns>
    /// <remarks>
    /// This method creates a strongly-typed command instance that eliminates the need
    /// for runtime type checking when processing command results.
    /// </remarks>
    new IGenericResult<TCommand> Build();
}

