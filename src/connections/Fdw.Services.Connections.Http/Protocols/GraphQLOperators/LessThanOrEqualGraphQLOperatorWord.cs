using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Http.Protocols.GraphQLOperators;

/// <summary>The operator LessThanOrEqual is written as <c>lte</c>.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(GraphQLOperatorWords), "LessThanOrEqual")]
public sealed class LessThanOrEqualGraphQLOperatorWord : GraphQLOperatorWordBase
{
    /// <summary>Initializes a new instance of the <see cref="LessThanOrEqualGraphQLOperatorWord"/> class.</summary>
    public LessThanOrEqualGraphQLOperatorWord()
        : base("LessThanOrEqual", "lte")
    {
    }
}
