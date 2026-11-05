namespace Fdw.Services.Connections.Abstractions.DocumentRecords;

/// <summary>
/// The one place a connection becomes a DocumentRecord connection (FDW-3673): itself when it is one, otherwise the first DocumentRecord
/// extension that takes its kind. The purpose check, the provider and the record store all ask here, so they cannot disagree.
/// </summary>
public static class DocumentRecordConnections
{
    /// <summary>Answers a connection as a DocumentRecord connection.</summary>
    /// <param name="connection">The connection, opened.</param>
    /// <returns>The DocumentRecord connection, or null when the connection is none and no extension takes its kind.</returns>
    public static IDocumentRecordConnection? Of(IGenericConnection connection)
    {
        if (connection is IDocumentRecordConnection direct)
        {
            return direct;
        }

        foreach (var extension in DocumentRecordConnectionTypes.All())
        {
            if (extension.Adapt(connection) is { } adapted)
            {
                return adapted;
            }
        }

        return null;
    }
}
