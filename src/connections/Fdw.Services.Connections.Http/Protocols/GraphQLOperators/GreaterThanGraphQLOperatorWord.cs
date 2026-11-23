using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Http.Protocols.GraphQLOperators;

/// <summary>The operator GreaterThan is written as <c>gt</c>.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(GraphQLOperatorWords), "GreaterThan")]
public sealed class GreaterThanGraphQLOperatorWord : GraphQLOperatorWordBase
{
    /// <summary>Initializes a new instance of the <see cref="GreaterThanGraphQLOperatorWord"/> class.</summary>
    public GreaterThanGraphQLOperatorWord()
        : base("GreaterThan", "gt")
    {
    }
}
