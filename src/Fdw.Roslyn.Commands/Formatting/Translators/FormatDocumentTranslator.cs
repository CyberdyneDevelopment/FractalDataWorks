using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Formatting.Commands;
using Fdw.Roslyn.Commands.Logging;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting;

namespace Fdw.Roslyn.Commands.Formatting.Translators;

/// <summary>
/// Translator for FormatDocumentCommand.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "FormatDocument")]
public sealed class FormatDocumentTranslator : RoslynCommandTranslatorBase<FormatDocumentCommand, MutationResult>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FormatDocumentTranslator"/> class.
    /// </summary>
    public FormatDocumentTranslator()
        : base("FormatDocument", "Formats an entire document")
    {
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult<MutationResult>> Translate(
        FormatDocumentCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        FormatDocumentTranslatorLog.Formatting(Logger, command.FilePath);

        var documentId = solution.GetDocumentIdsWithFilePath(command.FilePath).FirstOrDefault();
        if (documentId is null)
        {
            FormatDocumentTranslatorLog.DocumentNotFound(Logger, command.FilePath);
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("DocumentNotFound"),
                ResultDetails.Create().With("FilePath", command.FilePath));
        }

        var document = solution.GetDocument(documentId);
        if (document is null)
        {
            FormatDocumentTranslatorLog.FailedToLoadDocument(Logger, command.FilePath);
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("FailedToLoadDocument"));
        }

        var originalText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

        // Format the document
        var formattedDocument = await Formatter.FormatAsync(document, cancellationToken: cancellationToken).ConfigureAwait(false);
        var formattedText = await formattedDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);

        var changes = formattedText.GetTextChanges(originalText);
        var newSolution = formattedDocument.Project.Solution;

        var fileChanges = new List<FileChange>();
        if (changes.Count > 0)
        {
            fileChanges.Add(new FileChange(command.FilePath, FileChangeTypes.Modified, document.Project.Name)
            {
                TextChangeCount = changes.Count
            });
        }

        FormatDocumentTranslatorLog.Formatted(Logger, command.FilePath, changes.Count);

        return GenericResult<MutationResult>.Success(
            new MutationResult(
                $"Formatted document with {changes.Count} changes",
                newSolution,
                fileChanges));
    }
}
