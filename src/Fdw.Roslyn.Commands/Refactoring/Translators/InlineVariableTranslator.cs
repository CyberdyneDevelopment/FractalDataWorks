using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Logging;
using Fdw.Roslyn.Commands.Refactoring.Commands;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Text;

namespace Fdw.Roslyn.Commands.Refactoring.Translators;

/// <summary>
/// Translator for InlineVariableCommand.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "InlineVariable")]
public sealed class InlineVariableTranslator : RoslynCommandTranslatorBase<InlineVariableCommand, MutationResult>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InlineVariableTranslator"/> class.
    /// </summary>
    public InlineVariableTranslator()
        : base("InlineVariable", "Inlines a local variable")
    {
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Linear Roslyn flow: resolve variable, find references, replace with initializer
    public override async Task<IGenericResult<MutationResult>> Translate(
        InlineVariableCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        InlineVariableTranslatorLog.Inlining(Logger, command.FilePath, command.Line, command.Column);

        var documentId = solution.GetDocumentIdsWithFilePath(command.FilePath).FirstOrDefault();
        if (documentId is null)
        {
            InlineVariableTranslatorLog.DocumentNotFound(Logger, command.FilePath);
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("DocumentNotFound"),
                ResultDetails.Create().With("FilePath", command.FilePath));
        }

        var document = solution.GetDocument(documentId);
        if (document is null)
        {
            InlineVariableTranslatorLog.FailedToLoadDocument(Logger, command.FilePath);
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("FailedToLoadDocument"));
        }

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

        if (semanticModel is null || syntaxRoot is null)
        {
            InlineVariableTranslatorLog.FailedToAnalyzeDocument(Logger, command.FilePath);
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("FailedToAnalyzeDocument"));
        }

        var position = text.Lines.GetPosition(new LinePosition(command.Line - 1, command.Column - 1));
        var token = syntaxRoot.FindToken(position);
        var symbol = semanticModel.GetSymbolInfo(token.Parent!, cancellationToken).Symbol
                  ?? semanticModel.GetDeclaredSymbol(token.Parent!, cancellationToken);

        if (symbol is not ILocalSymbol localSymbol)
        {
            InlineVariableTranslatorLog.SymbolNotLocalVariable(Logger, command.FilePath, command.Line, command.Column);
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("SymbolNotLocalVariable"));
        }

        var variableName = localSymbol.Name;

        // Find the variable declarator
        var declarator = token.Parent?.AncestorsAndSelf()
            .OfType<VariableDeclaratorSyntax>()
            .FirstOrDefault();

        if (declarator?.Initializer is null)
        {
            InlineVariableTranslatorLog.VariableMustHaveInitializerToBeInlined(Logger, command.FilePath, variableName);
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("VariableMustHaveInitializerToBeInlined"));
        }

        var initializerExpression = declarator.Initializer.Value;

        // Find all references to this variable
        var references = await SymbolFinder.FindReferencesAsync(localSymbol, solution, cancellationToken).ConfigureAwait(false);
        var referenceLocations = references
            .SelectMany(r => r.Locations)
            .Where(l => l.Location.IsInSource && l.Document.Id == documentId)
            .ToList();

        // Replace all references with the initializer expression
        var newRoot = syntaxRoot;
        foreach (var refLoc in referenceLocations)
        {
            var refNode = newRoot.FindNode(refLoc.Location.SourceSpan);
            if (refNode is IdentifierNameSyntax identifier)
            {
                newRoot = newRoot.ReplaceNode(identifier, initializerExpression);
            }
        }

        // Remove the variable declaration
        var variableDeclaration = declarator.Parent as VariableDeclarationSyntax;
        if (variableDeclaration is not null)
        {
            var localDeclarationStatement = variableDeclaration.Parent as LocalDeclarationStatementSyntax;
            if (localDeclarationStatement is not null)
            {
                // If this is the only variable in the declaration, remove the entire statement
                if (variableDeclaration.Variables.Count == 1)
                {
                    newRoot = newRoot.RemoveNode(localDeclarationStatement, SyntaxRemoveOptions.KeepNoTrivia)!;
                }
                else
                {
                    // Otherwise, just remove this declarator
                    var newVariableDeclaration = variableDeclaration.RemoveNode(declarator, SyntaxRemoveOptions.KeepNoTrivia)!;
                    newRoot = newRoot.ReplaceNode(variableDeclaration, newVariableDeclaration);
                }
            }
        }

        var newDocument = document.WithSyntaxRoot(newRoot);
        var newSolution = newDocument.Project.Solution;

        var fileChanges = new List<FileChange>
        {
            new FileChange(command.FilePath, FileChangeTypes.Modified, document.Project.Name)
            {
                TextChangeCount = referenceLocations.Count + 1
            }
        };

        InlineVariableTranslatorLog.Inlined(Logger, variableName, referenceLocations.Count);

        return GenericResult<MutationResult>.Success(
            new MutationResult(
                $"Inlined variable '{variableName}' at {referenceLocations.Count} locations",
                newSolution,
                fileChanges));
    }
#pragma warning restore MA0051
}
