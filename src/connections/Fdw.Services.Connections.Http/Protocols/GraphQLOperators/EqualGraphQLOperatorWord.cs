using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Http.Protocols.GraphQLOperators;

/// <summary>The operator Equal is written as <c>eq</c>.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(GraphQLOperatorWords), "Equal")]
public sealed class EqualGraphQLOperatorWord : GraphQLOperatorWordBase
{
    /// <summary>Initializes a new instance of the <see cref="EqualGraphQLOperatorWord"/> class.</summary>
    public EqualGraphQLOperatorWord()
        : base("Equal", "eq")
    {
    }
}
