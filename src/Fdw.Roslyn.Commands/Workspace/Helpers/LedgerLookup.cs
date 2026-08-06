using System.Diagnostics.CodeAnalysis;
using Fdw.Roslyn.Commands.Abstractions.Results;

namespace Fdw.Roslyn.Commands.Workspace.Helpers;

/// <summary>
/// The result of asking the ledger where a missing type went.
/// </summary>
// Why: pure data holder with two factories, no logic
[ExcludeFromCodeCoverage]
public sealed class LedgerLookup
{
    private LedgerLookup(SymbolChange? change, string? reason)
    {
        Change = change;
        Reason = reason;
    }

    /// <summary>Gets the matched symbol change, or <see langword="null"/> when unresolved.</summary>
    public SymbolChange? Change { get; }

    /// <summary>Gets why the lookup failed, or <see langword="null"/> when it succeeded.</summary>
    public string? Reason { get; }

    /// <summary>Gets a value indicating whether the ledger explained the error.</summary>
    public bool IsResolved => Change is not null;

    /// <summary>Creates a resolved lookup.</summary>
    /// <param name="change">The matched symbol change.</param>
    /// <returns>The lookup.</returns>
    public static LedgerLookup Found(SymbolChange change) => new(change, null);

    /// <summary>Creates an unresolved lookup.</summary>
    /// <param name="reason">Why the lookup failed.</param>
    /// <returns>The lookup.</returns>
    public static LedgerLookup NotFound(string reason) => new(null, reason);
}
