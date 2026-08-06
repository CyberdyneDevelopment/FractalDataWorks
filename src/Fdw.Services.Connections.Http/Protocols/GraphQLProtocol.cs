using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Connections.Http.Abstractions.OptionTypes;
using Fdw.Services.Connections.Http.Abstractions.OptionTypes.HttpProtocolOptions;

namespace Fdw.Services.Connections.Http.Protocols;

/// <summary>
/// Standard GraphQL protocol implementation.
/// </summary>
/// <remarks>
/// <para>
/// This protocol implements standard GraphQL conventions:
/// <list type="bullet">
/// <item><description>POST requests to /graphql endpoint</description></item>
/// <item><description>JSON request body with query, operationName, variables</description></item>
/// <item><description>Response with data and errors</description></item>
/// <item><description>Automatic query building from IDataCommand</description></item>
/// </list>
/// </para>
/// <para>
/// Query generation supports:
/// <list type="bullet">
/// <item><description>Explicit queries via Metadata["GraphQLQuery"]</description></item>
/// <item><description>Auto-generated queries from container schema</description></item>
/// <item><description>Variables for filters, input data, and ordering</description></item>
/// </list>
/// </para>
/// <para>
/// For service-specific GraphQL APIs, extend <see cref="GraphQLProtocolBase"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Using explicit query in metadata
/// var command = new QueryCommand("users")
///     .WithMetadata("GraphQLQuery", "query { users { id name email } }");
///
/// // Using auto-generated query with filter
/// var command = new QueryCommand("users")
///     .WithFilter(f => f.Equals("active", true));
/// // Generates: query { users(filter: $filter) { id } } with variables
/// </code>
/// </example>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(HttpProtocols), "GraphQL")]
public sealed class GraphQLProtocol : GraphQLProtocolBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GraphQLProtocol"/> class.
    /// </summary>
    public GraphQLProtocol()
        : base(3, "GraphQL", "GraphQL API protocol with query/mutation support")
    {
    }
}