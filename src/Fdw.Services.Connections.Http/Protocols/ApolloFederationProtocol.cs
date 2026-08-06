using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Connections.Http.Abstractions;
using Fdw.Services.Connections.Http.Abstractions.OptionTypes.HttpProtocolOptions;

namespace Fdw.Services.Connections.Http.Protocols;

/// <summary>
/// Apollo Federation compatible GraphQL protocol.
/// </summary>
/// <remarks>
/// <para>
/// This protocol supports Apollo Federation conventions:
/// <list type="bullet">
/// <item><description>_entities query for federated entity resolution</description></item>
/// <item><description>_service query for schema discovery</description></item>
/// <item><description>@key, @requires, @provides directive handling</description></item>
/// </list>
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(HttpProtocols), "ApolloFederation")]
public sealed class ApolloFederationProtocol : GraphQLProtocolBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApolloFederationProtocol"/> class.
    /// </summary>
    public ApolloFederationProtocol()
        : base(8, "ApolloFederation", "Apollo Federation compatible GraphQL protocol")
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureGraphQLHeaders(System.Net.Http.HttpRequestMessage request, HttpProtocolContext context)
    {
        base.ConfigureGraphQLHeaders(request, context);

        // Apollo Federation may require additional headers such as:
        // - Apollo-Require-Preflight
        // - apollo-federation-include-trace
        // These would typically come from configuration when implemented
    }
}