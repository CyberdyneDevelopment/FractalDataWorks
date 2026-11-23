using Fdw.Collections;

namespace Fdw.Services.Connections.Http.Abstractions.OptionTypes.CommandVerbs;

/// <summary>What an HTTP protocol writes for one data command type: the HTTP method, the GraphQL mutation it sends and the name of its GraphQL operation.</summary>
public interface IHttpCommandVerb : ITypeOption<int, IHttpCommandVerb>
{
    /// <summary>Gets the HTTP method, as <c>HttpMethod</c> spells it (<c>GET</c>, <c>POST</c>, <c>PUT</c>, <c>DELETE</c>).</summary>
    string HttpMethodName { get; }

    /// <summary>Gets the GraphQL mutation action (<c>create</c>, <c>update</c>, <c>delete</c>), or null when the command is sent as a GraphQL query.</summary>
    string? GraphQLMutationAction { get; }

    /// <summary>Gets what a GraphQL operation name for this command starts with (<c>Get</c>, <c>Create</c>...), or null when the command names no operation.</summary>
    string? OperationPrefix { get; }
}
