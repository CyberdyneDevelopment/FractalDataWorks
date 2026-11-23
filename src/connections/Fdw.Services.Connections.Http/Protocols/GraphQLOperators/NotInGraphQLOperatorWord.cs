using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Http.Protocols.GraphQLOperators;

/// <summary>The operator NotIn is written as <c>notIn</c>.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(GraphQLOperatorWords), "NotIn")]
public sealed class NotInGraphQLOperatorWord : GraphQLOperatorWordBase
{
    /// <summary>Initializes a new instance of the <see cref="NotInGraphQLOperatorWord"/> class.</summary>
    public NotInGraphQLOperatorWord()
        : base("NotIn", "notIn")
    {
    }
}
