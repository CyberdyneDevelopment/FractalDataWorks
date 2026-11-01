#pragma warning disable CA1305 // Specify IFormatProvider - project commands use invariant strings

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Logging;
using Fdw.Roslyn.Commands.Projects.Commands;
using Fdw.Roslyn.Commands.Projects.Results;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Projects.Translators;

/// <summary>
/// Translator for RemoveDocumentCommand.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "RemoveDocumentTranslator")]
public sealed class RemoveDocumentTranslator : RoslynCommandTranslatorBase<RemoveDocumentCommand, MutationResult<RemoveDocumentResult>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemoveDocumentTranslator"/> class.
    /// </summary>
    public RemoveDocumentTranslator()
        : base("RemoveDocumentTranslator", "Translates RemoveDocumentCommand to remove a document from a project")
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<MutationResult<RemoveDocumentResult>>> Translate(
        RemoveDocumentCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        RemoveDocumentTranslatorLog.Removing(Logger, command.ProjectName, command.DocumentPath);

        var project = solution.Projects.FirstOrDefault(p =>
            string.Equals(p.Name, command.ProjectName, StringComparison.OrdinalIgnoreCase));

        if (project is null)
        {
            RemoveDocumentTranslatorLog.ProjectNotFound(Logger, command.ProjectName);
            return Task.FromResult<IGenericResult<MutationResult<RemoveDocumentResult>>>(
                GenericResult<MutationResult<RemoveDocumentResult>>.Failure(
                RoslynResultCodes.ByName("ProjectNotFound"),
                ResultDetails.Create().With("ProjectName", command.ProjectName)));
        }

        // Find the document
        var document = project.Documents.FirstOrDefault(d =>
            string.Equals(d.FilePath, command.DocumentPath, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(d.Name, command.DocumentPath, StringComparison.OrdinalIgnoreCase));

        if (document is null)
        {
            RemoveDocumentTranslatorLog.NotFound(Logger, command.ProjectName, command.DocumentPath);

            var notFoundResult = new RemoveDocumentResult(
                projectName: command.ProjectName,
                documentName: command.DocumentPath,
                documentPath: command.DocumentPath,
                removed: false,
                reason: "Document not found");

            var notFoundMutationResult = new MutationResult<RemoveDocumentResult>(
                $"Document {command.DocumentPath} not found",
                solution,
                notFoundResult);

            return Task.FromResult(GenericResult<MutationResult<RemoveDocumentResult>>.Success(notFoundMutationResult));
        }

        // Remove the document
        var newSolution = solution.RemoveDocument(document.Id);

        var result = new RemoveDocumentResult(
            projectName: command.ProjectName,
            documentName: document.Name,
            documentPath: document.FilePath ?? string.Empty,
            removed: true);

        var mutationResult = new MutationResult<RemoveDocumentResult>(
            $"Removed document {document.Name} from {command.ProjectName}",
            newSolution,
            result);

        RemoveDocumentTranslatorLog.Removed(Logger, command.ProjectName, document.Name);

        return Task.FromResult(GenericResult<MutationResult<RemoveDocumentResult>>.Success(mutationResult));
    }
}
