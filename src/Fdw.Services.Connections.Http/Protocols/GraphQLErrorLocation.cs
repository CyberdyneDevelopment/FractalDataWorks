namespace Fdw.Services.Connections.Http.Protocols;

/// <summary>
/// GraphQL error location indicating where in the query the error occurred.
/// </summary>
public class GraphQLErrorLocation
{
    /// <summary>Gets or sets the line number (1-based).</summary>
    public int Line { get; set; }

    /// <summary>Gets or sets the column number (1-based).</summary>
    public int Column { get; set; }
}