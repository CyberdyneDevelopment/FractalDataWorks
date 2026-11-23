using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Services.Connections.Http.Protocols.GraphQLOperators;

/// <summary>The type the GraphQL operator word collection is built on.</summary>
[ExcludeFromCodeCoverage]
public abstract class GraphQLOperatorWordBase : TypeOptionBase<GraphQLOperatorWordBase>, IGraphQLOperatorWord
{
    /// <summary>Initializes the option with the operator's name and the GraphQL word for it.</summary>
    /// <param name="name">The filter operator's name.</param>
    /// <param name="word">The GraphQL word.</param>
    protected GraphQLOperatorWordBase(string name, string word)
        : base(name)
    {
        Word = word;
    }

    /// <inheritdoc/>
    public string Word { get; }
}
