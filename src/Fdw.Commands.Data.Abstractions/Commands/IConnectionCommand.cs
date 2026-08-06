using Fdw.Abstractions;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// Marker interface for connection-specific commands.
/// These are the OUTPUT of IDataCommandTranslator (IDataCommand → IConnectionCommand).
/// </summary>
/// <remarks>
/// <para>
/// Connection commands are runtime command instances (not type definitions).
/// They inherit from Fdw.Abstractions.IGenericCommand.
/// </para>
/// <para>
/// Examples:
/// <list type="bullet">
/// <item>SqlConnectionCommand - SQL text with parameters</item>
/// <item>RestConnectionCommand - HTTP request with OData query</item>
/// <item>FileConnectionCommand - File operations</item>
/// <item>GraphQLConnectionCommand - GraphQL query</item>
/// </list>
/// </para>
/// </remarks>
public interface IConnectionCommand : IGenericCommand
{
}
