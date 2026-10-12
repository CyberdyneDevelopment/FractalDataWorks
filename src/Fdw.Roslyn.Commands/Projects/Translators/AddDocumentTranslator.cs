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
using Microsoft.CodeAnalysis.Text;

namespace Fdw.Roslyn.Commands.Projects.Translators;

/// <summary>
/// Translator for AddDocumentCommand.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "AddDocumentTranslator")]
public sealed class AddDocumentTranslator : RoslynCommandTranslatorBase<AddDocumentCommand, MutationResult<AddDocumentResult>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddDocumentTranslator"/> class.
    /// </summary>
    public AddDocumentTranslator()
        : base("AddDocumentTranslator", "Translates AddDocumentCommand to add a document to a project")
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<MutationResult<AddDocumentResult>>> Translate(
        AddDocumentCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        AddDocumentTranslatorLog.Adding(Logger, command.ProjectName, command.DocumentName);

        var project = solution.Projects.FirstOrDefault(p =>
            string.Equals(p.Name, command.ProjectName, StringComparison.OrdinalIgnoreCase));

        if (project is null)
        {
            AddDocumentTranslatorLog.ProjectNotFound(Logger, command.ProjectName);
            return Task.FromResult<IGenericResult<MutationResult<AddDocumentResult>>>(
                GenericResult<MutationResult<AddDocumentResult>>.Failure(
                RoslynResultCodes.ByName("ProjectNotFound"),
                ResultDetails.Create().With("ProjectName", command.ProjectName)));
        }

        // Check if document already exists
        var existingDoc = project.Documents.FirstOrDefault(d =>
            string.Equals(d.Name, command.DocumentName, StringComparison.OrdinalIgnoreCase) &&
            d.Folders.SequenceEqual(command.Folders, StringComparer.OrdinalIgnoreCase));

        if (existingDoc is not null)
        {
            AddDocumentTranslatorLog.AlreadyExists(Logger, command.ProjectName, command.DocumentName);

            var existingResult = new AddDocumentResult(
                projectName: command.ProjectName,
                documentName: command.DocumentName,
                folders: command.Folders,
                added: false,
                reason: "Document already exists");

            var existingMutationResult = new MutationResult<AddDocumentResult>(
                $"Document {command.DocumentName} already exists",
                solution,
                existingResult);

            return Task.FromResult(GenericResult<MutationResult<AddDocumentResult>>.Success(existingMutationResult));
        }

        // Add the document
        var sourceText = SourceText.From(command.Content);
        var newSolution = solution.AddDocument(
            DocumentId.CreateNewId(project.Id, command.DocumentName),
            command.DocumentName,
            sourceText,
            folders: command.Folders);

        var result = new AddDocumentResult(
            projectName: command.ProjectName,
            documentName: command.DocumentName,
            folders: command.Folders,
            added: true);

        var mutationResult = new MutationResult<AddDocumentResult>(
            $"Added document {command.DocumentName} to {command.ProjectName}",
            newSolution,
            result);

        AddDocumentTranslatorLog.Added(Logger, command.ProjectName, command.DocumentName);

        return Task.FromResult(GenericResult<MutationResult<AddDocumentResult>>.Success(mutationResult));
    }
}
