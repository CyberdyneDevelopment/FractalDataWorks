namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// A data command a database connection can run, and the one answer every such command gives about its route: whether it
/// runs serially, as ordinary SQL, or is rerouted to the connection's bulk mode (FDW-1152).
/// </summary>
/// <remarks>
/// <para>
/// Why the answer lives on the command and not on the translator: whether an insert is worth a bulk copy depends on the command's
/// own rows against a limit the connection's configuration states, and a translator sees neither the limit nor the connection.
/// The database connection (and its transaction class) asks the command before it translates, and takes its bulk route when the
/// answer is yes. Only database connections ask; an HTTP, file or any other connection never references this interface.
/// </para>
/// <para>
/// Why every data command answers, with a default of no: for a command where bulk does nothing (a query, an update, a delete) the
/// answer is always no and the command runs serially exactly as it did. A command that can use bulk (a bulk insert) overrides it.
/// </para>
/// </remarks>
public interface IDatabaseConnectionCommand : IDataCommand
{
    /// <summary>Says whether this command runs in the connection's bulk mode rather than serially.</summary>
    /// <param name="maxSerialInsert">The most rows the connection's configuration lets an insert run serially, as ordinary SQL.</param>
    /// <returns><see langword="true"/> when the command has more rows than <paramref name="maxSerialInsert"/> and can use bulk; otherwise <see langword="false"/>.</returns>
    bool ReroutesToBulk(int maxSerialInsert);
}
