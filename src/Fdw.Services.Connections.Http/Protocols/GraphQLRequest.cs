using System.Collections.Generic;

namespace Fdw.Services.Connections.Http.Protocols;

/// <summary>
/// GraphQL request body structure.
/// </summary>
public class GraphQLRequest
{
    /// <summary>Gets or sets the GraphQL query or mutation string.</summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>Gets or sets the operation name (optional).</summary>
    public string? OperationName { get; set; }

    /// <summary>Gets or sets the variables dictionary (optional).</summary>
    public IDictionary<string, object?>? Variables { get; set; }
}