using Fdw.Processors;

namespace Fdw.Services.Connections;

/// <summary>
/// Marker interface for connection-specific processors.
/// </summary>
/// <typeparam name="TCommand">The command type being processed (e.g., StringBuilder for connection strings).</typeparam>
/// <typeparam name="TContext">The processing context (e.g., connection configuration + resolved secrets).</typeparam>
/// <remarks>
/// <para>
/// This interface extends <see cref="IProcessor{TCommand, TContext}"/> to provide
/// a domain-specific marker for connection processors. It enables type constraints
/// and categorization of processors that handle connection-related transformations.
/// </para>
/// <para>
/// Connection processors are used for:
/// <list type="bullet">
/// <item>Authentication - Adding credentials to connection strings or requests</item>
/// <item>Signing - Adding cryptographic signatures to requests</item>
/// <item>Encryption - Encrypting sensitive connection parameters</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public interface IMySqlAuthProcessor 
///     : IConnectionProcessor&lt;StringBuilder, MySqlContext&gt;,
///       ITypeOption&lt;int, MySqlAuthProcessorBase&gt;
/// {
/// }
/// </code>
/// </example>
public interface IConnectionProcessor<TCommand, TContext>
    : IProcessor<TCommand, TContext>
{
}
