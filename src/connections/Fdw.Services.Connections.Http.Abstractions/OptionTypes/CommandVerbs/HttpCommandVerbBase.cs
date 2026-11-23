using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Services.Connections.Http.Abstractions.OptionTypes.CommandVerbs;

/// <summary>The type the HTTP command verb collection is built on.</summary>
[ExcludeFromCodeCoverage]
public abstract class HttpCommandVerbBase : TypeOptionBase<HttpCommandVerbBase>, IHttpCommandVerb
{
    /// <summary>Initializes the verb with the command type name it is found by and what it writes.</summary>
    /// <param name="name">The data command type name (<c>Query</c>, <c>Insert</c>...).</param>
    /// <param name="httpMethodName">The HTTP method.</param>
    /// <param name="graphQLMutationAction">The GraphQL mutation action, or null when the command is sent as a query.</param>
    /// <param name="operationPrefix">What the GraphQL operation name starts with, or null when the command names no operation.</param>
    protected HttpCommandVerbBase(string name, string httpMethodName, string? graphQLMutationAction, string? operationPrefix)
        : base(name)
    {
        HttpMethodName = httpMethodName;
        GraphQLMutationAction = graphQLMutationAction;
        OperationPrefix = operationPrefix;
    }

    /// <inheritdoc/>
    public string HttpMethodName { get; }

    /// <inheritdoc/>
    public string? GraphQLMutationAction { get; }

    /// <inheritdoc/>
    public string? OperationPrefix { get; }
}
