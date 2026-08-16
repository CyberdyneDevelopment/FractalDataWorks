using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Analysis.Helpers;
using Fdw.Roslyn.Commands.Logging;
using Fdw.Roslyn.Commands.Refactoring.Commands;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace Fdw.Roslyn.Commands.Refactoring.Translators;

/// <summary>
/// Translator for RenameCommand.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "Rename")]
public sealed class RenameTranslator : RoslynCommandTranslatorBase<RenameCommand, MutationResult>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RenameTranslator"/> class.
    /// </summary>
    public RenameTranslator()
        : base("Rename", "Renames a symbol across the solution")
    {
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Linear Roslyn flow: resolve symbol, perform rename, calculate changes
    public override async Task<IGenericResult<MutationResult>> Translate(
        RenameCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        RenameTranslatorLog.Renaming(Logger, command.FilePath, command.Line, command.Column, command.NewName);

        var documentId = solution.GetDocumentIdsWithFilePath(command.FilePath).FirstOrDefault();
        if (documentId is null)
        {
            RenameTranslatorLog.DocumentNotFound(Logger, command.FilePath);
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("DocumentNotFound"),
                ResultDetails.Create().With("FilePath", command.FilePath));
        }

        var document = solution.GetDocument(documentId);
        if (document is null)
        {
            RenameTranslatorLog.FailedToLoadDocument(Logger, command.FilePath);
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("FailedToLoadDocument"));
        }

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

        if (semanticModel is null || syntaxRoot is null)
        {
            RenameTranslatorLog.FailedToAnalyzeDocument(Logger, command.FilePath);
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("FailedToAnalyzeDocument"));
        }

        var position = text.Lines.GetPosition(new LinePosition(command.Line - 1, command.Column - 1));
        var token = syntaxRoot.FindToken(position);
        var symbol = semanticModel.GetSymbolInfo(token.Parent!, cancellationToken).Symbol
                  ?? semanticModel.GetDeclaredSymbol(token.Parent!, cancellationToken);

        if (symbol is null)
        {
            RenameTranslatorLog.NoSymbolFoundAtPosition(Logger, command.FilePath, command.Line, command.Column);
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("NoSymbolFoundAtPosition"));
        }

        var oldName = symbol.Name;
        var oldFqn = SymbolFqn.Of(symbol);
        var kind = symbol.Kind.ToString();
        var newFqn = SymbolFqn.OfRenamed(symbol, command.NewName);

        // Perform the rename
        var newSolution = await Renamer.RenameSymbolAsync(
            solution,
            symbol,
            new SymbolRenameOptions(),
            command.NewName,
            cancellationToken).ConfigureAwait(false);

        // Calculate changes
        var changedFiles = new List<FileChange>();
        var changeCount = 0;

        foreach (var projectId in newSolution.ProjectIds)
        {
            var newProject = newSolution.GetProject(projectId);
            var oldProject = solution.GetProject(projectId);

            if (newProject is null || oldProject is null)
                continue;

            foreach (var newDoc in newProject.Documents)
            {
                var oldDoc = oldProject.GetDocument(newDoc.Id);
                if (oldDoc is null)
                    continue;

                var newText = await newDoc.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var oldText = await oldDoc.GetTextAsync(cancellationToken).ConfigureAwait(false);

                if (!newText.ContentEquals(oldText))
                {
                    var changes = newText.GetTextChanges(oldText);
                    changedFiles.Add(new FileChange(
                        newDoc.FilePath ?? string.Empty,
                        FileChangeTypes.Modified,
                        newProject.Name)
                    {
                        TextChangeCount = changes.Count
                    });
                    changeCount += changes.Count;
                }
            }
        }

        var symbolChanges = new List<SymbolChange>
        {
            new SymbolChange(
                oldFqn, newFqn, SymbolChangeTypes.Renamed.Name, kind,
                document.FilePath, document.FilePath,
                document.Project.AssemblyName, document.Project.AssemblyName,
                NamespaceLayout.RelativePosition(document.Project, document.FilePath))
        };

        RenameTranslatorLog.Renamed(Logger, oldName, command.NewName, changeCount, changedFiles.Count);

        return GenericResult<MutationResult>.Success(
            new MutationResult(
                $"Renamed '{oldName}' to '{command.NewName}' with {changeCount} changes across {changedFiles.Count} files",
                newSolution,
                changedFiles,
                symbolChanges,
                Array.Empty<PathChange>()));
    }
#pragma warning restore MA0051
}
