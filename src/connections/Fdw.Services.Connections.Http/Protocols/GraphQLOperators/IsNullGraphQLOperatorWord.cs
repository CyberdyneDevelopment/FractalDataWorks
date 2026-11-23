using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Http.Protocols.GraphQLOperators;

/// <summary>The operator IsNull is written as <c>eq</c>. It carries no separate value (the value is null by convention); eq against a null value is the protocol's null-comparison vocabulary.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(GraphQLOperatorWords), "IsNull")]
public sealed class IsNullGraphQLOperatorWord : GraphQLOperatorWordBase
{
    /// <summary>Initializes a new instance of the <see cref="IsNullGraphQLOperatorWord"/> class.</summary>
    public IsNullGraphQLOperatorWord()
        : base("IsNull", "eq")
    {
    }
}
