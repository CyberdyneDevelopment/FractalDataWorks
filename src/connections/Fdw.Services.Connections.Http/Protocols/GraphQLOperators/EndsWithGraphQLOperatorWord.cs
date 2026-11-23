using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Http.Protocols.GraphQLOperators;

/// <summary>The operator EndsWith is written as <c>endsWith</c>.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(GraphQLOperatorWords), "EndsWith")]
public sealed class EndsWithGraphQLOperatorWord : GraphQLOperatorWordBase
{
    /// <summary>Initializes a new instance of the <see cref="EndsWithGraphQLOperatorWord"/> class.</summary>
    public EndsWithGraphQLOperatorWord()
        : base("EndsWith", "endsWith")
    {
    }
}
