using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Commands.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Results;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// Interface for data command translators.
/// Translators convert universal IDataCommand to domain-specific command types.
/// </summary>
/// <typeparam name="TCommand">The command type this translator produces (SqlCommand, HttpRequestMessage, etc.).</typeparam>
/// <remarks>
/// <para>
/// Translators bridge the gap between universal data commands and connection-specific commands:
/// <list type="bullet">
/// <item>SQL Translator: IDataCommand → SqlCommand</item>
/// <item>REST Translator: IDataCommand → HttpRequestMessage</item>
/// <item>File Translator: IDataCommand → FileStream operations</item>
/// <item>GraphQL Translator: IDataCommand → GraphQL query object</item>
/// </list>
/// </para>
/// <para>
/// Translators are registered:
/// <list type="bullet">
/// <item>Compile-time: Via [TypeOption] attribute (discovered by source generator)</item>
/// <item>Runtime: Via DataCommandTranslators.Register() (for connection-provided translators)</item>
/// </list>
/// </para>
/// <para>
/// Inherits Id, Name, and Category from ITypeOption for TypeCollection support.
/// </para>
/// </remarks>
public interface IDataCommandTranslator<TCommand> : ITypeOption<int>
{
    /// <summary>
    /// Gets the domain name this translator targets (Sql, Rest, File, GraphQL, etc.).
    /// </summary>
    string DomainName { get; }

    /// <summary>
    /// Translates a data command to a connection-specific command.
    /// Uses container schema for intelligent query building (field roles, types, converters).
    /// </summary>
    /// <param name="command">The data command to translate.</param>
    /// <param name="container">The container with schema metadata (fields, roles, converters).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the translated command of type TCommand.</returns>
    Task<IGenericResult<TCommand>> Translate(
        IDataCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken = default);
}
