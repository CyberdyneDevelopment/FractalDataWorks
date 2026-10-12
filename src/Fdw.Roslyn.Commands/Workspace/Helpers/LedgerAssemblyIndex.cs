using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Roslyn.Commands.Abstractions.Results;

namespace Fdw.Roslyn.Commands.Workspace.Helpers;

/// <summary>
/// Indexes the change ledger so a missing type name can be mapped to the assembly that now carries it.
/// </summary>
/// <remarks>
/// Only MOVED symbols are indexed. A rename changes the fully-qualified name, so the compiler error names
/// a type that genuinely no longer exists — that is a real consumer break for a human to decide on, not
/// something to auto-repair by adding a reference.
/// </remarks>
public sealed class LedgerAssemblyIndex
{
    private readonly Dictionary<string, List<SymbolChange>> _byFullName = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<SymbolChange>> _bySimpleName = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="LedgerAssemblyIndex"/> class from ledger entries.
    /// </summary>
    /// <param name="entries">The ledger entries.</param>
    public LedgerAssemblyIndex(IReadOnlyList<ChangeLedgerEntry> entries)
        : this(RequireEntries(entries).SelectMany(e => e.SymbolChanges).ToList())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LedgerAssemblyIndex"/> class from symbol changes.
    /// </summary>
    /// <param name="changes">The symbol changes, from a session ledger or a published guide.</param>
    /// <remarks>
    /// A consumer has no session ledger — only the producer's committed guide. Indexing the changes
    /// directly is what lets both sources drive one repair path.
    /// </remarks>
    public LedgerAssemblyIndex(IReadOnlyList<SymbolChange> changes)
    {
        if (changes is null) throw new ArgumentNullException(nameof(changes));

        foreach (var change in changes)
        {
            if (!string.Equals(change.ChangeType, SymbolChangeTypes.Moved.Name, StringComparison.Ordinal))
                continue;
            if (!change.CrossesAssembly) continue;

            Add(_byFullName, change.NewFullyQualifiedName, change);
            Add(_bySimpleName, SimpleName(change.NewFullyQualifiedName), change);
        }
    }

    private static IReadOnlyList<ChangeLedgerEntry> RequireEntries(IReadOnlyList<ChangeLedgerEntry> entries) =>
        entries ?? throw new ArgumentNullException(nameof(entries));

    /// <summary>Gets the number of distinct moved types indexed.</summary>
    public int Count => _byFullName.Count;

    /// <summary>
    /// Resolves a missing type or namespace name to the assembly that now carries it.
    /// </summary>
    /// <param name="missingName">The name from the compiler diagnostic.</param>
    /// <returns>The resolution outcome.</returns>
    /// <remarks>
    /// Ambiguity is reported, never broken by picking one. Two moved types sharing a simple name and
    /// landing in different assemblies cannot be told apart from the diagnostic alone, and guessing would
    /// add a reference the caller never chose.
    /// </remarks>
    public LedgerLookup Resolve(string missingName)
    {
        if (string.IsNullOrWhiteSpace(missingName))
            return LedgerLookup.NotFound("empty name");

        if (_byFullName.TryGetValue(missingName, out var exact))
            return Single(exact, missingName);

        if (_bySimpleName.TryGetValue(SimpleName(missingName), out var bySimple))
            return Single(bySimple, missingName);

        return LedgerLookup.NotFound(
            $"'{missingName}' does not appear in the change ledger as a type moved between assemblies");
    }

    private static LedgerLookup Single(List<SymbolChange> candidates, string missingName)
    {
        var assemblies = candidates
            .Select(c => c.NewAssembly!)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (assemblies.Count > 1)
            return LedgerLookup.NotFound(
                $"'{missingName}' is ambiguous in the ledger — moved to {string.Join(", ", assemblies)}; resolve by hand");

        return LedgerLookup.Found(candidates[0]);
    }

    private static void Add(Dictionary<string, List<SymbolChange>> index, string key, SymbolChange change)
    {
        if (string.IsNullOrEmpty(key)) return;
        if (!index.TryGetValue(key, out var list))
        {
            list = new List<SymbolChange>();
            index[key] = list;
        }

        list.Add(change);
    }

    private static string SimpleName(string fullName)
    {
        var index = fullName.LastIndexOf('.');
        return index < 0 ? fullName : fullName.Substring(index + 1);
    }
}
