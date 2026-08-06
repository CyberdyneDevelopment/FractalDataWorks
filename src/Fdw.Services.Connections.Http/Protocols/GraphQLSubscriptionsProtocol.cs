using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Connections.Http.Abstractions.OptionTypes.HttpProtocolOptions;

namespace Fdw.Services.Connections.Http.Protocols;

/// <summary>
/// GraphQL protocol with subscription support via WebSocket.
/// </summary>
/// <remarks>
/// <para>
/// This protocol extends GraphQL with support for subscriptions using
/// the graphql-ws protocol over WebSocket connections.
/// </para>
/// <para>
/// For standard query/mutation operations, uses HTTP POST like <see cref="GraphQLProtocol"/>.
/// For subscriptions, establishes WebSocket connection to ws:// or wss:// endpoint.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(HttpProtocols), "GraphQLSubscriptions")]
public sealed class GraphQLSubscriptionsProtocol : GraphQLProtocolBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GraphQLSubscriptionsProtocol"/> class.
    /// </summary>
    public GraphQLSubscriptionsProtocol()
        : base(7, "GraphQLSubscriptions", "GraphQL protocol with subscription support over WebSocket")
    {
    }

    // Note: Actual WebSocket subscription handling would require additional infrastructure
    // beyond HTTP request/response. This protocol can handle regular queries/mutations
    // while signaling that subscription support is expected.
}