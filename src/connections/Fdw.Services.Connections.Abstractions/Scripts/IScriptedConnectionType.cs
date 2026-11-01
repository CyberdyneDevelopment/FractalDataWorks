using Fdw.Collections;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// The script extension of one kind of database connection (FDW-1447): given a connection of that kind, it answers the same
/// connection as one that takes scripts.
/// </summary>
public interface IScriptedConnectionType : ITypeOption<int, IScriptedConnectionType>
{
    /// <summary>Wraps a connection so it takes scripts.</summary>
    /// <param name="connection">A connection a data store reaches its data by.</param>
    /// <returns>The scripted connection when this extension is for that kind of connection; null when it is not.</returns>
    IScriptedDataConnection? Adapt(IDataConnection connection);
}
