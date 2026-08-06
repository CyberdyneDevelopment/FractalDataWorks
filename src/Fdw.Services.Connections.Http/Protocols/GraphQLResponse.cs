using System.Collections.Generic;
using System.Text.Json;

namespace Fdw.Services.Connections.Http.Protocols;

/// <summary>
/// GraphQL response structure.
/// </summary>
public class GraphQLResponse
{
    /// <summary>Gets or sets the data returned by the query/mutation.</summary>
    public JsonElement? Data { get; set; }

    /// <summary>Gets or sets any errors returned by the GraphQL server.</summary>
    public IList<GraphQLError>? Errors { get; set; }

    /// <summary>Gets or sets extensions data (optional, server-specific).</summary>
    public IDictionary<string, object?>? Extensions { get; set; }
}