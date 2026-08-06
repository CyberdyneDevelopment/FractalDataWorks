namespace Fdw.Roslyn.Commands.Abstractions;

/// <summary>
/// Marks a command that carries the caller's reason for making the change.
/// </summary>
/// <remarks>
/// The ledger records WHAT changed; this is the only thing that records WHY, and a migration guide that
/// cannot say which slice or issue caused a move is not auditable.
/// </remarks>
public interface IReasonedCommand
{
    /// <summary>Gets the caller's stated reason for the change.</summary>
    string? Reason { get; }
}
