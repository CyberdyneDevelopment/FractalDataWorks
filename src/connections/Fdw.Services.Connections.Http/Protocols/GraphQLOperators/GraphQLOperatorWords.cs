using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Http.Protocols.GraphQLOperators;

/// <summary>
/// The filter operators GraphQL can express and the word it writes for each, found by the operator's name. An operator that matches none
/// gets <c>NotFound</c>: the protocol does not support it.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(GraphQLOperatorWordBase), typeof(IGraphQLOperatorWord), typeof(GraphQLOperatorWords))]
public abstract partial class GraphQLOperatorWords : TypeCollectionBase<GraphQLOperatorWordBase, IGraphQLOperatorWord>
{
}
