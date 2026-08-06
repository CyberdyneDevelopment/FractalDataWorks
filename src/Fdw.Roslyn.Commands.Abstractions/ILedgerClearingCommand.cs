namespace Fdw.Roslyn.Commands.Abstractions;

/// <summary>
/// Marks the one command permitted to discard the change ledger.
/// </summary>
/// <remarks>
/// Loading a solution, closing a workspace and setting a baseline all used to clear it as a side
/// effect. The ledger is the record a migration guide is built from, so it is discarded only when
/// someone asks for that by name.
/// </remarks>
public interface ILedgerClearingCommand
{
}
