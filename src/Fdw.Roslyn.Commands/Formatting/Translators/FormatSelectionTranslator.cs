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
using Microsoft.CodeAnalysis.Text;

namespace Fdw.Roslyn.Commands.Formatting.Translators;

/// <summary>
/// Translator for FormatSelectionCommand.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "FormatSelection")]
public sealed class FormatSelectionTranslator : RoslynCommandTranslatorBase<FormatSelectionCommand, MutationResult>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FormatSelectionTranslator"/> class.
    /// </summary>
    public FormatSelectionTranslator()
        : base("FormatSelection", "Formats a selection within a document")
    {
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult<MutationResult>> Translate(
        FormatSelectionCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        FormatSelectionTranslatorLog.Formatting(Logger, command.FilePath, command.StartLine, command.EndLine);

        var documentId = solution.GetDocumentIdsWithFilePath(command.FilePath).FirstOrDefault();
        if (documentId is null)
        {
            FormatSelectionTranslatorLog.DocumentNotFound(Logger, command.FilePath);
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("DocumentNotFound"),
                ResultDetails.Create().With("FilePath", command.FilePath));
        }

        var document = solution.GetDocument(documentId);
        if (document is null)
        {
            FormatSelectionTranslatorLog.FailedToLoadDocument(Logger, command.FilePath);
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("FailedToLoadDocument"));
        }

        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

        var startPosition = text.Lines.GetPosition(new LinePosition(command.StartLine - 1, command.StartColumn - 1));
        var endPosition = text.Lines.GetPosition(new LinePosition(command.EndLine - 1, command.EndColumn - 1));
        var span = TextSpan.FromBounds(startPosition, endPosition);

        // Format the selection
        var formattedDocument = await Formatter.FormatAsync(document, span, cancellationToken: cancellationToken).ConfigureAwait(false);
        var formattedText = await formattedDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);

        var changes = formattedText.GetTextChanges(text);
        var newSolution = formattedDocument.Project.Solution;

        var fileChanges = new List<FileChange>();
        if (changes.Count > 0)
        {
            fileChanges.Add(new FileChange(command.FilePath, FileChangeTypes.Modified, document.Project.Name)
            {
                TextChangeCount = changes.Count
            });
        }

        FormatSelectionTranslatorLog.Formatted(Logger, command.FilePath, command.StartLine, command.EndLine, changes.Count);

        return GenericResult<MutationResult>.Success(
            new MutationResult(
                $"Formatted selection from line {command.StartLine} to {command.EndLine} with {changes.Count} changes",
                newSolution,
                fileChanges));
    }
}
