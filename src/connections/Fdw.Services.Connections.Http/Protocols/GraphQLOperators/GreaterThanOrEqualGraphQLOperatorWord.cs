using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Http.Protocols.GraphQLOperators;

/// <summary>The operator GreaterThanOrEqual is written as <c>gte</c>.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(GraphQLOperatorWords), "GreaterThanOrEqual")]
public sealed class GreaterThanOrEqualGraphQLOperatorWord : GraphQLOperatorWordBase
{
    /// <summary>Initializes a new instance of the <see cref="GreaterThanOrEqualGraphQLOperatorWord"/> class.</summary>
    public GreaterThanOrEqualGraphQLOperatorWord()
        : base("GreaterThanOrEqual", "gte")
    {
    }
}
