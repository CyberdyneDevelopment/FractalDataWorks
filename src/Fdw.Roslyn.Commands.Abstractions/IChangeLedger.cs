using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions.Results;

namespace Fdw.Roslyn.Commands.Abstractions;

/// <summary>
/// Records mutating command effects for the current session and can render them as a
/// migration-guide markdown document.
/// </summary>
public interface IChangeLedger
{
    /// <summary>
    /// Records one mutating command's effects as a new ledger entry.
    /// </summary>
    /// <param name="commandName">The name of the command that produced the mutation.</param>
    /// <param name="summary">The mutation's summary text.</param>
    /// <param name="changedFiles">The file changes produced by the mutation.</param>
    /// <param name="symbolChanges">The symbol-level changes produced by the mutation.</param>
    /// <param name="pathChanges">The path changes produced by the mutation.</param>
    /// <returns>The recorded entry.</returns>
    ChangeLedgerEntry Record(
        string commandName,
        string summary,
        IReadOnlyList<FileChange> changedFiles,
        IReadOnlyList<SymbolChange> symbolChanges,
        IReadOnlyList<PathChange> pathChanges);

    /// <summary>
    /// Gets a snapshot of every entry recorded so far, in sequence order.
    /// </summary>
    IReadOnlyList<ChangeLedgerEntry> Entries { get; }

    /// <summary>
    /// Clears all recorded entries.
    /// </summary>
    void Clear();

    /// <summary>
    /// Writes the recorded entries to a migration-guide markdown file at <paramref name="outputPath"/>.
    /// </summary>
    /// <param name="outputPath">The absolute path to write the markdown file to.</param>
    /// <param name="solutionName">The solution name to display in the document header, or null when the solution has no file path.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    Task<IGenericResult<MigrationGuideResult>> WriteMarkdown(
        string outputPath,
        string? solutionName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes the ledger as markdown, APPENDING a titled section when the guide already exists.
    /// </summary>
    /// <param name="outputPath">The file to write.</param>
    /// <param name="solutionName">The solution name for the document header.</param>
    /// <param name="overwrite">When true, replace the file entirely instead of appending to it.</param>
    /// <param name="sectionTitle">The section label for the appended section.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The write result.</returns>
    /// <remarks>
    /// Appending is the DEFAULT because the guide is meant to live in the repo and accumulate across
    /// commits — a diff on it shows exactly what a given change moved. Clobbering a committed ledger is
    /// destructive and irreversible, so it has to be asked for explicitly.
    /// </remarks>
    Task<IGenericResult<MigrationGuideResult>> WriteMarkdown(
        string outputPath,
        string? solutionName,
        bool overwrite,
        string? sectionTitle,
        CancellationToken cancellationToken = default);
}
