#pragma warning disable CA1305 // Specify IFormatProvider - migration guide uses invariant strings

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Fdw.Roslyn.Commands.Workspace.Helpers;
using Fdw.Roslyn.Commands.Abstractions.Results;

namespace Fdw.Roslyn.Commands;

/// <summary>
/// Builds the migration-guide markdown document from a session's recorded change-ledger entries.
/// </summary>
public static class MigrationGuideMarkdownFormatter
{
    /// <summary>
    /// Builds the markdown content for the migration guide.
    /// </summary>
    /// <param name="solutionName">The solution name to display in the document header, or null when the solution has no file path.</param>
    /// <param name="entries">The recorded ledger entries, in sequence order.</param>
    /// <returns>The complete markdown document text.</returns>
    public static string Build(string? solutionName, IReadOnlyList<ChangeLedgerEntry> entries)
    {
        if (entries is null) throw new ArgumentNullException(nameof(entries));

        var builder = new StringBuilder();
        // Why: an in-memory solution has no file path — render a nameless header rather than
        // inventing a placeholder name.
        builder.AppendLine(string.IsNullOrWhiteSpace(solutionName)
            ? "# Migration Guide"
            : $"# Migration Guide — {solutionName}");
        builder.AppendLine();
        AppendBody(builder, entries);

        return builder.ToString();
    }

    /// <summary>
    /// Builds an APPENDABLE section rather than a whole document, for a guide that accumulates across
    /// commits instead of being overwritten each session.
    /// </summary>
    /// <param name="sectionTitle">The section label, e.g. "slice-1-vocabulary".</param>
    /// <param name="entries">The ledger entries to render.</param>
    /// <param name="stamp">The timestamp to record.</param>
    /// <returns>The markdown section, led by a rule so successive appends stay separable.</returns>
    /// <remarks>
    /// The timestamp is passed in rather than read from the clock so the output is deterministic and the
    /// formatter stays testable.
    /// </remarks>
    public static string BuildSection(
        string sectionTitle,
        IReadOnlyList<ChangeLedgerEntry> entries,
        DateTimeOffset stamp)
    {
        if (entries is null) throw new ArgumentNullException(nameof(entries));

        var builder = new StringBuilder();
        builder.AppendLine();
        builder.AppendLine("---");
        builder.AppendLine();
        builder.AppendLine(CultureInfo.InvariantCulture,
            $"# {sectionTitle} — {stamp.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture)}");
        builder.AppendLine();
        AppendBody(builder, entries);

        return builder.ToString();
    }

    /// <summary>
    /// Builds only the document header, for the first append to a guide that does not exist yet.
    /// </summary>
    /// <param name="solutionName">The solution the guide describes.</param>
    /// <returns>The title block, with no report body.</returns>
    /// <remarks>
    /// Build() with zero entries produced a full REPORT saying "0 change(s) recorded… No changes were
    /// recorded", which then sat above the real section in every guide the append path created. The
    /// header is a different thing from an empty report, so it gets its own method rather than a flag.
    /// </remarks>
    public static string BuildHeader(string? solutionName)
    {
        var builder = new StringBuilder();
        builder.AppendLine(CultureInfo.InvariantCulture,
            $"# Migration Guide{(string.IsNullOrWhiteSpace(solutionName) ? string.Empty : " — " + solutionName)}");
        builder.AppendLine();

        return builder.ToString();
    }

    private static void AppendBody(StringBuilder builder, IReadOnlyList<ChangeLedgerEntry> entries)
    {
        builder.AppendLine(CultureInfo.InvariantCulture, $"{entries.Count} change(s) recorded in this session.");

        if (entries.Count == 0)
        {
            builder.AppendLine();
            builder.AppendLine("No changes were recorded in this session.");
            return;
        }

        AppendRenames(builder, entries);
        AppendMoves(builder, entries);
        AppendEntrySections(builder, entries);
    }

    private static void AppendRenames(StringBuilder builder, IReadOnlyList<ChangeLedgerEntry> entries)
    {
        var renames = entries
            .SelectMany(e => e.SymbolChanges)
            .Where(s => string.Equals(s.ChangeType, SymbolChangeTypes.Renamed.Name, StringComparison.Ordinal))
            .ToList();

        if (renames.Count == 0) return;

        builder.AppendLine();
        builder.AppendLine("## Renames");
        builder.AppendLine();
        // Why: a rename changes the fully-qualified name, so every downstream consumer that names the old
        // FQN stops compiling. That is the opposite of a Move, which preserves the FQN. The reader must be
        // able to tell the two apart at a glance, so the consumer impact is stated here rather than implied.
        builder.AppendLine("**These are consumer-breaking**: the fully-qualified name changed, so consumers naming the old FQN must be updated. For `[TypeOption]` types the FNV-1a `Id` derived from the FQN changes too.");
        builder.AppendLine();
        builder.AppendLine("| Old | New | Kind |");
        builder.AppendLine("|---|---|---|");
        foreach (var rename in renames)
        {
            builder.AppendLine($"| {rename.OldFullyQualifiedName} | {rename.NewFullyQualifiedName} | {rename.SymbolKind} |");
        }
    }

    private static void AppendMoves(StringBuilder builder, IReadOnlyList<ChangeLedgerEntry> entries)
    {
        var pathMoves = entries.SelectMany(e => e.PathChanges).ToList();
        var symbolMoves = entries
            .SelectMany(e => e.SymbolChanges)
            .Where(s => string.Equals(s.ChangeType, SymbolChangeTypes.Moved.Name, StringComparison.Ordinal))
            .ToList();

        if (pathMoves.Count == 0 && symbolMoves.Count == 0) return;

        builder.AppendLine();
        builder.AppendLine("## Moves");
        builder.AppendLine();
        builder.AppendLine("**These are NOT consumer-breaking**: the fully-qualified name is unchanged, so a consumer hitting CS0246 needs a package reference to the new assembly, not a code edit.");
        builder.AppendLine();
        // Why: a consumer reads this in THEIR repo. Absolute file paths from the author's machine tell
        // them nothing actionable — "Fdw.Data.MsSql.VarCharType is now in Fdw.Data.MsSql" is what maps a
        // CS0246 to a package reference, which is the entire purpose of the table.
        builder.AppendLine("| Type | From package | To package | Kind |");
        builder.AppendLine("|---|---|---|---|");
        foreach (var pathMove in pathMoves)
        {
            builder.AppendLine($"| {pathMove.NewPath} | {pathMove.OldPath} | {pathMove.NewPath} | {pathMove.Kind} |");
        }
        foreach (var symbolMove in symbolMoves)
        {
            builder.AppendLine(
                $"| {symbolMove.NewFullyQualifiedName} " +
                $"| {symbolMove.OldAssembly ?? MigrationGuideReader.NotRecorded} " +
                $"| {symbolMove.NewAssembly ?? MigrationGuideReader.NotRecorded} " +
                $"| {symbolMove.SymbolKind} |");
        }

        AppendAssemblyHops(builder, symbolMoves);
    }

    // Why: this is the table a consumer actually reads when CS0246 fires — it maps the type to the
    // assembly (package) that now carries it. RelativePosition is carried alongside so a later split
    // slice can verify the programme's positional invariant against what an earlier slice recorded.
    private static void AppendAssemblyHops(StringBuilder builder, IReadOnlyList<SymbolChange> symbolMoves)
    {
        var hops = symbolMoves.Where(s => s.CrossesAssembly).ToList();
        if (hops.Count == 0) return;

        builder.AppendLine();
        builder.AppendLine(Workspace.Helpers.MigrationGuideReader.AssemblyMovesHeading);
        builder.AppendLine();
        builder.AppendLine(Workspace.Helpers.MigrationGuideReader.AssemblyMovesColumns);
        builder.AppendLine("|---|---|---|---|");
        foreach (var hop in hops)
        {
            builder.AppendLine($"| {hop.NewFullyQualifiedName} | {hop.OldAssembly} | {hop.NewAssembly} | {hop.RelativePosition ?? Workspace.Helpers.MigrationGuideReader.NotRecorded} |");
        }
    }

    private static void AppendEntrySections(StringBuilder builder, IReadOnlyList<ChangeLedgerEntry> entries)
    {
        foreach (var entry in entries)
        {
            builder.AppendLine();
            builder.AppendLine(CultureInfo.InvariantCulture, $"## {entry.Sequence}. {entry.CommandName}");
            builder.AppendLine();
            builder.AppendLine(entry.Summary);

            if (entry.FileChanges.Count == 0) continue;

            builder.AppendLine();
            foreach (var fileChange in entry.FileChanges)
            {
                builder.AppendLine($"- {fileChange.FilePath} ({fileChange.ChangeType})");
            }
        }
    }
}
