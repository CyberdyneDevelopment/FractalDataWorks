using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Http.Protocols.GraphQLOperators;

/// <summary>The operator StartsWith is written as <c>startsWith</c>.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(GraphQLOperatorWords), "StartsWith")]
public sealed class StartsWithGraphQLOperatorWord : GraphQLOperatorWordBase
{
    /// <summary>Initializes a new instance of the <see cref="StartsWithGraphQLOperatorWord"/> class.</summary>
    public StartsWithGraphQLOperatorWord()
        : base("StartsWith", "startsWith")
    {
    }
}
