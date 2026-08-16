using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Formatting.Commands;
using Fdw.Roslyn.Commands.Formatting.Results;
using Fdw.Roslyn.Commands.Logging;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Fdw.Roslyn.Commands.Formatting.Translators;

/// <summary>
/// Translator for NormalizeLineEndingsCommand.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "NormalizeLineEndings")]
public sealed class NormalizeLineEndingsTranslator : RoslynCommandTranslatorBase<NormalizeLineEndingsCommand, MutationResult<LineEndingData>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NormalizeLineEndingsTranslator"/> class.
    /// </summary>
    public NormalizeLineEndingsTranslator()
        : base("NormalizeLineEndings", "Normalizes line endings in a document")
    {
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Linear text processing: count line endings, normalize to target, build result
    public override async Task<IGenericResult<MutationResult<LineEndingData>>> Translate(
        NormalizeLineEndingsCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        var documentId = solution.GetDocumentIdsWithFilePath(command.FilePath).FirstOrDefault();
        if (documentId is null)
        {
            NormalizeLineEndingsTranslatorLog.DocumentNotFound(Logger, command.FilePath);
            return GenericResult<MutationResult<LineEndingData>>.Failure(
                RoslynResultCodes.ByName("DocumentNotFound"),
                ResultDetails.Create().With("FilePath", command.FilePath));
        }

        var document = solution.GetDocument(documentId);
        if (document is null)
        {
            NormalizeLineEndingsTranslatorLog.FailedToLoadDocument(Logger, command.FilePath);
            return GenericResult<MutationResult<LineEndingData>>.Failure(
                RoslynResultCodes.ByName("FailedToLoadDocument"));
        }

        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var content = text.ToString();

        var lineEndingParam = command.LineEnding.ToLowerInvariant();

        var targetLineEnding = lineEndingParam switch
        {
            "crlf" => "\r\n",
            "cr" => "\r",
            _ => "\n"
        };

        var lineEndingName = lineEndingParam switch
        {
            "crlf" => "CRLF",
            "cr" => "CR",
            _ => "LF"
        };

        NormalizeLineEndingsTranslatorLog.Normalizing(Logger, command.FilePath, lineEndingName);

        // Count different line ending types
        var crlfCount = 0;
        var lfCount = 0;
        var crCount = 0;

        for (var i = 0; i < content.Length; i++)
        {
            if (content[i] == '\r')
            {
                if (i + 1 < content.Length && content[i + 1] == '\n')
                {
                    crlfCount++;
                    i++; // Skip the \n
                }
                else
                {
                    crCount++;
                }
            }
            else if (content[i] == '\n')
            {
                lfCount++;
            }
        }

        var normalizedCount = 0;
        var newSolution = solution;

        // Only normalize if there are mixed line endings or different from target
        if ((crlfCount > 0 && (lfCount > 0 || crCount > 0)) ||
            (lfCount > 0 && crCount > 0) ||
            (string.Equals(lineEndingParam, "lf", StringComparison.Ordinal) && crlfCount > 0) ||
            (string.Equals(lineEndingParam, "crlf", StringComparison.Ordinal) && lfCount > 0) ||
            (string.Equals(lineEndingParam, "cr", StringComparison.Ordinal) && (crlfCount > 0 || lfCount > 0)))
        {
            // Normalize line endings
            var normalized = new StringBuilder(content.Length);

            for (var i = 0; i < content.Length; i++)
            {
                if (content[i] == '\r')
                {
                    if (i + 1 < content.Length && content[i + 1] == '\n')
                    {
                        normalized.Append(targetLineEnding);
                        normalizedCount++;
                        i++; // Skip the \n
                    }
                    else
                    {
                        normalized.Append(targetLineEnding);
                        normalizedCount++;
                    }
                }
                else if (content[i] == '\n')
                {
                    normalized.Append(targetLineEnding);
                    normalizedCount++;
                }
                else
                {
                    normalized.Append(content[i]);
                }
            }

            var newText = SourceText.From(normalized.ToString(), text.Encoding);
            newSolution = solution.WithDocumentText(documentId, newText);
        }

        var fileChanges = new List<FileChange>();
        if (normalizedCount > 0)
        {
            fileChanges.Add(new FileChange(command.FilePath, FileChangeTypes.Modified, document.Project.Name)
            {
                TextChangeCount = normalizedCount
            });
        }

        var data = new LineEndingData
        {
            TargetLineEnding = lineEndingName,
            NormalizedCount = normalizedCount,
            OriginalCrlfCount = crlfCount,
            OriginalLfCount = lfCount,
            OriginalCrCount = crCount
        };

        NormalizeLineEndingsTranslatorLog.Normalized(Logger, command.FilePath, normalizedCount, lineEndingName);

        return GenericResult<MutationResult<LineEndingData>>.Success(
            new MutationResult<LineEndingData>(
                $"Normalized {normalizedCount} line endings to {lineEndingName}",
                newSolution,
                fileChanges,
                data));
    }
#pragma warning restore MA0051
}
