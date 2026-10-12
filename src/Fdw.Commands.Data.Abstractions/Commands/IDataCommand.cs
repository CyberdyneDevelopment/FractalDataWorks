using System.Collections.Generic;
using Fdw.Abstractions;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// Base interface for all data commands.
/// Data commands extend IGenericCommand and can be submitted anywhere IGenericCommand is accepted.
/// </summary>
/// <remarks>
/// <para>
/// This is the non-generic marker interface used by TypeCollection source generators.
/// For type-safe execution, use <see cref="IDataCommand{TResult}"/> or <see cref="IDataCommand{TResult, TInput}"/>.
/// </para>
/// <para>
/// Data commands represent universal data operations that work across all connection types:
/// SQL, REST, File, GraphQL, etc. Translators convert IDataCommand to domain-specific commands.
/// </para>
/// </remarks>
public interface IDataCommand : IGenericCommand
{
    /// <summary>
    /// Gets metadata for the command (connection hints, caching, etc.).
    /// </summary>
    /// <value>A read-only dictionary of metadata key-value pairs.</value>
    IReadOnlyDictionary<string, object> Metadata { get; }
}

/// <summary>
/// Data command with typed input and typed result.
/// Use this interface for commands that require input data and return a specific result type.
/// </summary>
/// <typeparam name="TResult">The type of result this command returns.</typeparam>
/// <typeparam name="TInput">The type of input data this command requires.</typeparam>
/// <remarks>
/// <para>
/// This interface provides compile-time type safety for both input and result, eliminating all casting.
/// </para>
/// <para>
/// Examples:
/// <list type="bullet">
/// <item>InsertCommand&lt;Customer&gt; accepts Customer, returns int (identity)</item>
/// <item>UpdateCommand&lt;Customer&gt; accepts Customer, returns int (affected rows)</item>
/// <item>BulkInsertCommand&lt;Customer&gt; accepts IEnumerable&lt;Customer&gt;, returns BulkInsertResult</item>
/// </list>
/// </para>
/// </remarks>
public interface IDataCommand<TResult, TInput> : IDataCommand<TResult>, IDataCommandWithInput
{
    /// <summary>
    /// Gets the input data for this command.
    /// </summary>
    /// <value>The typed input data.</value>
    TInput Data { get; }
}
/// <summary>
/// Data command with typed result.
/// Use this interface for commands that return a specific result type without requiring input data.
/// </summary>
/// <typeparam name="TResult">The type of result this command returns.</typeparam>
/// <remarks>
/// <para>
/// This interface provides compile-time type safety for command results, eliminating the need for casting.
/// </para>
/// <para>
/// Examples:
/// <list type="bullet">
/// <item>QueryCommand&lt;Customer&gt; returns IEnumerable&lt;Customer&gt;</item>
/// <item>DeleteCommand returns int (affected rows)</item>
/// </list>
/// </para>
/// </remarks>
public interface IDataCommand<TResult> : IDataCommand
{
    // Marker interface for type-safe result
    // No additional members needed - type parameter provides compile-time safety
}
