using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Http.Protocols.GraphQLOperators;

/// <summary>The operator LessThan is written as <c>lt</c>.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(GraphQLOperatorWords), "LessThan")]
public sealed class LessThanGraphQLOperatorWord : GraphQLOperatorWordBase
{
    /// <summary>Initializes a new instance of the <see cref="LessThanGraphQLOperatorWord"/> class.</summary>
    public LessThanGraphQLOperatorWord()
        : base("LessThan", "lt")
    {
    }
}
