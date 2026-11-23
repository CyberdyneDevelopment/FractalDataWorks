using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Http.Protocols.GraphQLOperators;

/// <summary>The operator IsNotNull is written as <c>ne</c>. It carries no separate value (the value is null by convention); ne against a null value is the protocol's null-comparison vocabulary.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(GraphQLOperatorWords), "IsNotNull")]
public sealed class IsNotNullGraphQLOperatorWord : GraphQLOperatorWordBase
{
    /// <summary>Initializes a new instance of the <see cref="IsNotNullGraphQLOperatorWord"/> class.</summary>
    public IsNotNullGraphQLOperatorWord()
        : base("IsNotNull", "ne")
    {
    }
}
