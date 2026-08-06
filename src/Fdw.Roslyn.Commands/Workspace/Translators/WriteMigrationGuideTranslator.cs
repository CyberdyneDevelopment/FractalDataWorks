#pragma warning disable CA1305 // Specify IFormatProvider - workspace commands use invariant strings

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Workspace.Commands;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Workspace.Translators;

/// <summary>
/// Translator for writing the session's change ledger to a migration-guide markdown file.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "WriteMigrationGuide")]
public sealed class WriteMigrationGuideTranslator
    : RoslynCommandTranslatorBase<WriteMigrationGuideCommand, QueryResult<MigrationGuideResult>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WriteMigrationGuideTranslator"/> class.
    /// </summary>
    public WriteMigrationGuideTranslator()
        : base("WriteMigrationGuideTranslator", "Translates write migration guide commands")
    {
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult<QueryResult<MigrationGuideResult>>> Translate(
        WriteMigrationGuideCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        if (command.Ledger is null)
        {
            return GenericResult<QueryResult<MigrationGuideResult>>.Failure(
                RoslynResultCodes.ByName("LedgerNotAvailable"));
        }

        if (string.IsNullOrWhiteSpace(command.OutputPath))
        {
            return GenericResult<QueryResult<MigrationGuideResult>>.Failure(
                RoslynResultCodes.ByName("OutputPathRequired"));
        }

        // Why: header-only use — an in-memory/no-path solution renders a nameless guide header;
        // the absence is passed through explicitly, never replaced with an invented name.
        var solutionName = solution.FilePath is null
            ? null
            : Path.GetFileNameWithoutExtension(solution.FilePath);

        // Why: a relative path would otherwise resolve against the MCP server's process working
        // directory — wherever the client happened to spawn it — so the guide would land somewhere
        // arbitrary. Resolving against the solution directory is what makes an in-repo path like
        // "PACKAGE-MIGRATION.md" deterministic, committable, and therefore trackable across commits.
        var outputPath = command.OutputPath;
        if (!Path.IsPathRooted(outputPath))
        {
            var solutionDirectory = solution.FilePath is null ? null : Path.GetDirectoryName(solution.FilePath);
            if (string.IsNullOrEmpty(solutionDirectory))
            {
                return GenericResult<QueryResult<MigrationGuideResult>>.Failure(
                    RoslynResultCodes.ByName("RelativeOutputPathNeedsSolutionPath"),
                    ResultDetails.Create().With("OutputPath", command.OutputPath));
            }

            outputPath = Path.GetFullPath(Path.Combine(solutionDirectory!, outputPath));
        }

        var writeResult = await command.Ledger
            .WriteMarkdown(outputPath, solutionName, command.Overwrite, command.SectionTitle, cancellationToken)
            .ConfigureAwait(false);

        if (!writeResult.IsSuccess || writeResult.Value is null)
        {
            return writeResult.ToNewResult<QueryResult<MigrationGuideResult>>();
        }

        var result = new QueryResult<MigrationGuideResult>(
            $"Migration guide written to '{command.OutputPath}' with {writeResult.Value.EntryCount} entries",
            writeResult.Value);

        return GenericResult<QueryResult<MigrationGuideResult>>.Success(result);
    }
}
