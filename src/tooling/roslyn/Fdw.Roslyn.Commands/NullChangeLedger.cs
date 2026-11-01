using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;

namespace Fdw.Roslyn.Commands;

/// <summary>
/// No-op <see cref="IChangeLedger"/> used when no ledger has been registered via DI.
/// </summary>
public sealed class NullChangeLedger : IChangeLedger
{
    /// <summary>
    /// Gets the shared singleton instance.
    /// </summary>
    public static NullChangeLedger Instance { get; } = new();

    private NullChangeLedger() { }

    /// <inheritdoc/>
    public ChangeLedgerEntry Record(
        string commandName,
        string summary,
        IReadOnlyList<FileChange> changedFiles,
        IReadOnlyList<SymbolChange> symbolChanges,
        IReadOnlyList<PathChange> pathChanges) =>
        new(
            0,
            commandName,
            summary,
            Array.Empty<LedgerFileChange>(),
            Array.Empty<SymbolChange>(),
            Array.Empty<PathChange>());

    /// <inheritdoc/>
    public IReadOnlyList<ChangeLedgerEntry> Entries => Array.Empty<ChangeLedgerEntry>();

    /// <inheritdoc/>
    public void Clear() { }

    /// <inheritdoc/>
    public Task<IGenericResult<MigrationGuideResult>> WriteMarkdown(
        string outputPath,
        string? solutionName,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(GenericResult<MigrationGuideResult>.Success(new MigrationGuideResult(outputPath, 0)));

    /// <inheritdoc/>
    public Task<IGenericResult<MigrationGuideResult>> WriteMarkdown(
        string outputPath,
        string? solutionName,
        bool overwrite,
        string? sectionTitle,
        CancellationToken cancellationToken = default) =>
        WriteMarkdown(outputPath, solutionName, cancellationToken);
}
