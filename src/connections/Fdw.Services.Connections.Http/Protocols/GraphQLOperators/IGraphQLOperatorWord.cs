using Fdw.Collections;

namespace Fdw.Services.Connections.Http.Protocols.GraphQLOperators;

/// <summary>The word GraphQL convention uses for one filter operator of FDW; the option is named as the operator is (<c>IFilterOperator.Name</c>).</summary>
public interface IGraphQLOperatorWord : ITypeOption<int, IGraphQLOperatorWord>
{
    /// <summary>Gets the GraphQL word (<c>eq</c>, <c>gte</c>, <c>startsWith</c>...).</summary>
    string Word { get; }
}
