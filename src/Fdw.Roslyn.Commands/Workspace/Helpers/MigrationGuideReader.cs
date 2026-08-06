using System;
using System.Collections.Generic;
using System.IO;
using Fdw.Roslyn.Commands.Abstractions.Results;

namespace Fdw.Roslyn.Commands.Workspace.Helpers;

/// <summary>
/// Reads a published migration guide back into the assembly moves it records.
/// </summary>
/// <remarks>
/// This is the CONSUMER half of the migration story. A consumer never ran the moves, so has no session
/// ledger — what they have is the producer's committed guide. Parsing its assembly-move table yields the
/// same type-to-package mapping the ledger would have, so the whole repair path works for them unchanged.
///
/// Reading the table makes its layout a CONTRACT rather than human formatting: the emitter and this
/// reader must move together, which is what the round-trip test exists to enforce.
/// </remarks>
public static class MigrationGuideReader
{
    /// <summary>The heading that opens the machine-readable table.</summary>
    public const string AssemblyMovesHeading = "### Assembly moves (type -> new package)";

    /// <summary>The table's column header.</summary>
    public const string AssemblyMovesColumns = "| Type (FQN) | Old assembly | New assembly | Relative position |";

    /// <summary>The placeholder written when a move recorded no relative position.</summary>
    public const string NotRecorded = "(not recorded)";

    /// <summary>
    /// Reads every cross-assembly move recorded in a guide.
    /// </summary>
    /// <param name="path">The guide file.</param>
    /// <returns>The moves, as the same <see cref="SymbolChange"/> shape the ledger produces.</returns>
    /// <remarks>
    /// Returns moves from EVERY section, because an appended guide accumulates one section per commit and
    /// a consumer jumping several versions needs all of them, not just the newest.
    /// </remarks>
    public static IReadOnlyList<SymbolChange> ReadAssemblyMoves(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Path is required", nameof(path));

        var moves = new List<SymbolChange>();
        var inTable = false;

        foreach (var line in File.ReadLines(path))
        {
            var trimmed = line.Trim();

            if (string.Equals(trimmed, AssemblyMovesHeading, StringComparison.Ordinal))
            {
                inTable = true;
                continue;
            }

            if (!inTable) continue;

            // A blank line or any new heading ends this section's table; later sections re-open it.
            if (trimmed.Length == 0) continue;
            if (trimmed.StartsWith('#'))
            {
                inTable = false;
                continue;
            }

            if (!trimmed.StartsWith('|')) { inTable = false; continue; }
            if (string.Equals(trimmed, AssemblyMovesColumns, StringComparison.Ordinal)) continue;
            if (trimmed.Replace("|", string.Empty).Trim().Trim('-').Length == 0) continue;

            var move = ParseRow(trimmed);
            if (move is not null) moves.Add(move);
        }

        return moves;
    }

    private static SymbolChange? ParseRow(string row)
    {
        var cells = row.Trim('|').Split('|');
        if (cells.Length < 3) return null;

        var fullName = cells[0].Trim();
        var oldAssembly = cells[1].Trim();
        var newAssembly = cells[2].Trim();
        var position = cells.Length > 3 ? cells[3].Trim() : string.Empty;

        if (fullName.Length == 0 || oldAssembly.Length == 0 || newAssembly.Length == 0) return null;

        // Why: the guide records a MOVE, so the fully-qualified name is unchanged on both sides — that is
        // precisely why a consumer needs a reference rather than a code edit.
        return new SymbolChange(
            fullName,
            fullName,
            SymbolChangeTypes.Moved.Name,
            "NamedType",
            null,
            null,
            oldAssembly,
            newAssembly,
            string.Equals(position, NotRecorded, StringComparison.Ordinal) || position.Length == 0 ? null : position);
    }
}
