using Fdw.Collections;

namespace Fdw.Services.Connections.Abstractions.DocumentRecords;

/// <summary>
/// The DocumentRecord extension of one kind of connection (FDW-3673): given a connection of that kind, it answers the same connection as
/// one that keeps document records, the way the script extensions do for scripts (FDW-1447).
/// </summary>
public interface IDocumentRecordConnectionType : ITypeOption<int, IDocumentRecordConnectionType>
{
    /// <summary>Wraps a connection so it keeps document records.</summary>
    /// <param name="connection">A connection.</param>
    /// <returns>The DocumentRecord connection when this extension is for that kind of connection; null when it is not.</returns>
    IDocumentRecordConnection? Adapt(IGenericConnection connection);
}
