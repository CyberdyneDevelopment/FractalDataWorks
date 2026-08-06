namespace Fdw.Roslyn.Commands.Abstractions;

/// <summary>
/// Marks a command as needing the session's <see cref="IChangeLedger"/> injected by the handler
/// before translation, mirroring the BaselineSolution/SnapshotSolution reflection-based injection
/// pattern but through a typed interface instead of reflection.
/// </summary>
public interface ILedgerAwareCommand
{
    /// <summary>
    /// Gets or sets the change ledger. Set by the handler before translation.
    /// </summary>
    IChangeLedger? Ledger { get; set; }
}
