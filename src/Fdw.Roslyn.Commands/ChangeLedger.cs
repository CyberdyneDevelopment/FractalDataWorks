using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Roslyn.Commands;

/// <summary>
/// Default in-memory implementation of <see cref="IChangeLedger"/>.
/// Thread-safe: the stdio host may dispatch tool calls concurrently.
/// </summary>
public sealed class ChangeLedger : IChangeLedger
{
    private readonly Lock _lock = new();
    private readonly List<ChangeLedgerEntry> _entries = new();
    private readonly ILogger<ChangeLedger> _logger;
    private int _sequence;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChangeLedger"/> class.
    /// </summary>
    /// <param name="logger">Optional logger.</param>
    public ChangeLedger(ILogger<ChangeLedger>? logger = null)
    {
        _logger = logger ?? NullLogger<ChangeLedger>.Instance;
    }

    /// <inheritdoc/>
    public ChangeLedgerEntry Record(
        string commandName,
        string summary,
        IReadOnlyList<FileChange> changedFiles,
        IReadOnlyList<SymbolChange> symbolChanges,
        IReadOnlyList<PathChange> pathChanges)
    {
        var ledgerFileChanges = new List<LedgerFileChange>(changedFiles.Count);
        foreach (var fileChange in changedFiles)
        {
            ledgerFileChanges.Add(new LedgerFileChange(
                fileChange.FilePath,
                fileChange.ChangeType.Name,
                fileChange.ProjectName,
                fileChange.TextChangeCount));
        }

        lock (_lock)
        {
            _sequence++;
            var entry = new ChangeLedgerEntry(_sequence, commandName, summary, ledgerFileChanges, symbolChanges, pathChanges);
            _entries.Add(entry);
            ChangeLedgerLog.LedgerEntryRecorded(_logger, entry.Sequence, commandName);
            return entry;
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<ChangeLedgerEntry> Entries
    {
        get
        {
            lock (_lock)
            {
                return _entries.ToArray();
            }
        }
    }

    /// <inheritdoc/>
    public void Clear()
    {
        lock (_lock)
        {
            _entries.Clear();
            _sequence = 0;
        }

        ChangeLedgerLog.LedgerCleared(_logger);
    }

    /// <inheritdoc/>
    public Task<IGenericResult<MigrationGuideResult>> WriteMarkdown(
        string outputPath,
        string? solutionName,
        CancellationToken cancellationToken = default)
    {
        var entries = Entries;
        return WriteText(
            outputPath,
            MigrationGuideMarkdownFormatter.Build(solutionName, entries),
            append: false,
            entries.Count,
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task<IGenericResult<MigrationGuideResult>> WriteMarkdown(
        string outputPath,
        string? solutionName,
        bool overwrite,
        string? sectionTitle,
        CancellationToken cancellationToken = default)
    {
        if (overwrite) return WriteMarkdown(outputPath, solutionName, cancellationToken);

        var entries = Entries;
        var section = MigrationGuideMarkdownFormatter.BuildSection(
            string.IsNullOrWhiteSpace(sectionTitle) ? "session" : sectionTitle!,
            entries,
            DateTimeOffset.Now);

        return WriteText(
            outputPath,
            File.Exists(outputPath)
                ? section
                : MigrationGuideMarkdownFormatter.BuildHeader(solutionName) + section,
            append: true,
            entries.Count,
            cancellationToken);
    }

    private async Task<IGenericResult<MigrationGuideResult>> WriteText(
        string outputPath,
        string markdown,
        bool append,
        int entryCount,
        CancellationToken cancellationToken)
    {
        try
        {
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (append)
                await File.AppendAllTextAsync(outputPath, markdown, cancellationToken).ConfigureAwait(false);
            else
                await File.WriteAllTextAsync(outputPath, markdown, cancellationToken).ConfigureAwait(false);

            ChangeLedgerLog.MigrationGuideWritten(_logger, outputPath, entryCount);
            return GenericResult<MigrationGuideResult>.Success(new MigrationGuideResult(outputPath, entryCount));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return GenericResult<MigrationGuideResult>.Failure(
                ChangeLedgerLog.MigrationGuideWriteFailed(_logger, ex, outputPath));
        }
    }
}
