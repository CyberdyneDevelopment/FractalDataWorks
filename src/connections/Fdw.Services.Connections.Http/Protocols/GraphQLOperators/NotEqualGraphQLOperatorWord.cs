using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Http.Protocols.GraphQLOperators;

/// <summary>The operator NotEqual is written as <c>ne</c>.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(GraphQLOperatorWords), "NotEqual")]
public sealed class NotEqualGraphQLOperatorWord : GraphQLOperatorWordBase
{
    /// <summary>Initializes a new instance of the <see cref="NotEqualGraphQLOperatorWord"/> class.</summary>
    public NotEqualGraphQLOperatorWord()
        : base("NotEqual", "ne")
    {
    }
}
