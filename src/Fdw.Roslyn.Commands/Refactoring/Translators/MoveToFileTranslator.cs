using System;
using System.Collections.Generic;
using System.IO;
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
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Fdw.Roslyn.Commands.Refactoring.Translators;

/// <summary>
/// Translator for MoveToFileCommand.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "MoveToFile")]
public sealed class MoveToFileTranslator : RoslynCommandTranslatorBase<MoveToFileCommand, MutationResult>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MoveToFileTranslator"/> class.
    /// </summary>
    public MoveToFileTranslator()
        : base("MoveToFile", "Moves a type to its own file")
    {
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Linear Roslyn flow: resolve type, build new file, remove from original
    public override async Task<IGenericResult<MutationResult>> Translate(
        MoveToFileCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        MoveToFileTranslatorLog.Moving(Logger, command.FilePath, command.Line, command.Column);

        var documentId = solution.GetDocumentIdsWithFilePath(command.FilePath).FirstOrDefault();
        if (documentId is null)
        {
            MoveToFileTranslatorLog.DocumentNotFound(Logger, command.FilePath);
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("DocumentNotFound"),
                ResultDetails.Create().With("FilePath", command.FilePath));
        }

        var document = solution.GetDocument(documentId);
        if (document is null)
        {
            MoveToFileTranslatorLog.FailedToLoadDocument(Logger, command.FilePath);
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("FailedToLoadDocument"));
        }

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

        if (semanticModel is null || syntaxRoot is null)
        {
            MoveToFileTranslatorLog.FailedToAnalyzeDocument(Logger, command.FilePath);
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("FailedToAnalyzeDocument"));
        }

        var position = text.Lines.GetPosition(new LinePosition(command.Line - 1, command.Column - 1));
        var token = syntaxRoot.FindToken(position);

        // Find the type declaration
        var typeDecl = token.Parent?.AncestorsAndSelf()
            .OfType<TypeDeclarationSyntax>()
            .FirstOrDefault();

        if (typeDecl is null)
        {
            MoveToFileTranslatorLog.NoTypeDeclarationFoundAtPosition(Logger, command.FilePath, command.Line, command.Column);
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("NoTypeDeclarationFoundAtPosition"));
        }

        var typeSymbol = semanticModel.GetDeclaredSymbol(typeDecl, cancellationToken);
        if (typeSymbol is null)
        {
            MoveToFileTranslatorLog.FailedToGetTypeSymbol(Logger, command.FilePath, command.Line, command.Column);
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("FailedToGetTypeSymbol"));
        }

        var typeName = typeSymbol.Name;
        var fqn = SymbolFqn.Of(typeSymbol);
        var directory = Path.GetDirectoryName(command.FilePath) ?? string.Empty;
        var newFileName = command.TargetFileName ?? $"{typeName}.cs";
        var newFilePath = Path.Combine(directory, newFileName);

        // Check if this is the only type in the file
        var allTypes = syntaxRoot.DescendantNodes()
            .OfType<TypeDeclarationSyntax>()
            .ToList();

        if (allTypes.Count == 1)
        {
            MoveToFileTranslatorLog.TypeAlreadyOnlyTypeInFile(Logger, command.FilePath, typeName);
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("TypeAlreadyOnlyTypeInFile"));
        }

        // Get namespace and using directives
        var namespaceDecl = typeDecl.Ancestors()
            .OfType<BaseNamespaceDeclarationSyntax>()
            .FirstOrDefault();

        var usings = syntaxRoot.DescendantNodes()
            .OfType<UsingDirectiveSyntax>()
            .ToList();

        // Build the new file content
        var compilationUnit = SyntaxFactory.CompilationUnit()
            .WithUsings(SyntaxFactory.List(usings));

        if (namespaceDecl is not null)
        {
            if (namespaceDecl is FileScopedNamespaceDeclarationSyntax fileScopedNs)
            {
                var newFileScopedNs = SyntaxFactory.FileScopedNamespaceDeclaration(fileScopedNs.Name)
                    .WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(typeDecl));
                compilationUnit = compilationUnit.AddMembers(newFileScopedNs);
            }
            else if (namespaceDecl is NamespaceDeclarationSyntax ns)
            {
                var newNs = SyntaxFactory.NamespaceDeclaration(ns.Name)
                    .WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(typeDecl));
                compilationUnit = compilationUnit.AddMembers(newNs);
            }
        }
        else
        {
            compilationUnit = compilationUnit.AddMembers(typeDecl);
        }

        var newFileContent = compilationUnit.NormalizeWhitespace().ToFullString();

        // Create new document
        var project = document.Project;
        var newDocumentId = DocumentId.CreateNewId(project.Id);
        var newDocument = project.AddDocument(newFileName, newFileContent, document.Folders, newFilePath);

        // Remove the type from the original document
        var newOriginalRoot = syntaxRoot.RemoveNode(typeDecl, SyntaxRemoveOptions.KeepNoTrivia)!;
        var updatedOriginalDocument = document.WithSyntaxRoot(newOriginalRoot);

        var newSolution = newDocument.Project.Solution;
        newSolution = newSolution.WithDocumentSyntaxRoot(updatedOriginalDocument.Id, newOriginalRoot);

        var fileChanges = new List<FileChange>
        {
            new FileChange(command.FilePath, FileChangeTypes.Modified, document.Project.Name)
            {
                TextChangeCount = 1
            },
            new FileChange(newFilePath, FileChangeTypes.Added, document.Project.Name)
            {
                TextChangeCount = 1
            }
        };

        var symbolChanges = new List<SymbolChange>
        {
            new SymbolChange(
                fqn, fqn, SymbolChangeTypes.Moved.Name, "NamedType",
                command.FilePath, newFilePath,
                document.Project.AssemblyName, document.Project.AssemblyName,
                NamespaceLayout.RelativePosition(document.Project, newFilePath))
        };

        MoveToFileTranslatorLog.Moved(Logger, typeName, newFileName);

        return GenericResult<MutationResult>.Success(
            new MutationResult(
                $"Moved type '{typeName}' to '{newFileName}'",
                newSolution,
                fileChanges,
                symbolChanges,
                Array.Empty<PathChange>()));
    }
#pragma warning restore MA0051
}
