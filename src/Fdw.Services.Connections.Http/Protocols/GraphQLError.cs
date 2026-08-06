using System.Collections.Generic;

namespace Fdw.Services.Connections.Http.Protocols;

/// <summary>
/// GraphQL error structure per the GraphQL specification.
/// </summary>
public class GraphQLError
{
    /// <summary>Gets or sets the error message.</summary>
    public string? Message { get; set; }

    /// <summary>Gets or sets the locations in the query where the error occurred.</summary>
    public IList<GraphQLErrorLocation>? Locations { get; set; }

    /// <summary>Gets or sets the path to the field that caused the error.</summary>
    public IList<object>? Path { get; set; }

    /// <summary>Gets or sets extensions data (optional, server-specific).</summary>
    public IDictionary<string, object?>? Extensions { get; set; }
}