using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Formatting.Commands;
using Fdw.Roslyn.Commands.Formatting.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Fdw.Roslyn.Commands.Formatting.Translators;

/// <summary>
/// Translator for RemoveTrailingWhitespaceCommand.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "RemoveTrailingWhitespace")]
public sealed class RemoveTrailingWhitespaceTranslator : RoslynCommandTranslatorBase<RemoveTrailingWhitespaceCommand, MutationResult<TrailingWhitespaceData>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemoveTrailingWhitespaceTranslator"/> class.
    /// </summary>
    public RemoveTrailingWhitespaceTranslator()
        : base("RemoveTrailingWhitespace", "Removes trailing whitespace from lines")
    {
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult<MutationResult<TrailingWhitespaceData>>> Translate(
        RemoveTrailingWhitespaceCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        var documentId = solution.GetDocumentIdsWithFilePath(command.FilePath).FirstOrDefault();
        if (documentId is null)
            return GenericResult<MutationResult<TrailingWhitespaceData>>.Failure(
                RoslynResultCodes.ByName("DocumentNotFound"),
                ResultDetails.Create().With("FilePath", command.FilePath));

        var document = solution.GetDocument(documentId);
        if (document is null)
            return GenericResult<MutationResult<TrailingWhitespaceData>>.Failure(
                RoslynResultCodes.ByName("FailedToLoadDocument"));

        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

        var linesWithTrailingWhitespace = new List<int>();
        var changes = new List<TextChange>();

        for (var i = 0; i < text.Lines.Count; i++)
        {
            var line = text.Lines[i];
            var lineText = line.ToString();
            var trimmedLength = lineText.TrimEnd().Length;

            if (trimmedLength < lineText.Length)
            {
                linesWithTrailingWhitespace.Add(i + 1); // 1-based line number
                var start = line.Start + trimmedLength;
                var end = line.End;
                changes.Add(new TextChange(TextSpan.FromBounds(start, end), string.Empty));
            }
        }

        var newSolution = solution;
        if (changes.Count > 0)
        {
            var newText = text.WithChanges(changes);
            newSolution = solution.WithDocumentText(documentId, newText);
        }

        var fileChanges = new List<FileChange>();
        if (linesWithTrailingWhitespace.Count > 0)
        {
            fileChanges.Add(new FileChange(command.FilePath, FileChangeTypes.Modified, document.Project.Name)
            {
                TextChangeCount = linesWithTrailingWhitespace.Count
            });
        }

        var data = new TrailingWhitespaceData
        {
            LineCount = linesWithTrailingWhitespace.Count,
            AffectedLines = linesWithTrailingWhitespace
        };

        return GenericResult<MutationResult<TrailingWhitespaceData>>.Success(
            new MutationResult<TrailingWhitespaceData>(
                $"Removed trailing whitespace from {linesWithTrailingWhitespace.Count} lines",
                newSolution,
                fileChanges,
                data));
    }
}
