using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Http.Protocols.GraphQLOperators;

/// <summary>The operator In is written as <c>in</c>.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(GraphQLOperatorWords), "In")]
public sealed class InGraphQLOperatorWord : GraphQLOperatorWordBase
{
    /// <summary>Initializes a new instance of the <see cref="InGraphQLOperatorWord"/> class.</summary>
    public InGraphQLOperatorWord()
        : base("In", "in")
    {
    }
}
