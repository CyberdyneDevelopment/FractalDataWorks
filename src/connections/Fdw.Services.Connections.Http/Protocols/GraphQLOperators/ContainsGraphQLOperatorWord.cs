using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Http.Protocols.GraphQLOperators;

/// <summary>The operator Contains is written as <c>contains</c>.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(GraphQLOperatorWords), "Contains")]
public sealed class ContainsGraphQLOperatorWord : GraphQLOperatorWordBase
{
    /// <summary>Initializes a new instance of the <see cref="ContainsGraphQLOperatorWord"/> class.</summary>
    public ContainsGraphQLOperatorWord()
        : base("Contains", "contains")
    {
    }
}
