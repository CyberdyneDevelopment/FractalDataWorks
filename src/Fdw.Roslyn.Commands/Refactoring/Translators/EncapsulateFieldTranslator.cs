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
using Fdw.Roslyn.Commands.Refactoring.Commands;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace Fdw.Roslyn.Commands.Refactoring.Translators;

/// <summary>
/// Translator for EncapsulateFieldCommand.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "EncapsulateField")]
public sealed class EncapsulateFieldTranslator : RoslynCommandTranslatorBase<EncapsulateFieldCommand, MutationResult>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EncapsulateFieldTranslator"/> class.
    /// </summary>
    public EncapsulateFieldTranslator()
        : base("EncapsulateField", "Encapsulates a field as a property")
    {
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Linear Roslyn flow: resolve field, rename references, replace declaration with property
    public override async Task<IGenericResult<MutationResult>> Translate(
        EncapsulateFieldCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        var documentId = solution.GetDocumentIdsWithFilePath(command.FilePath).FirstOrDefault();
        if (documentId is null)
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("DocumentNotFound"),
                ResultDetails.Create().With("FilePath", command.FilePath));

        var document = solution.GetDocument(documentId);
        if (document is null)
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("FailedToLoadDocument"));

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

        if (semanticModel is null || syntaxRoot is null)
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("FailedToAnalyzeDocument"));

        var position = text.Lines.GetPosition(new LinePosition(command.Line - 1, command.Column - 1));
        var token = syntaxRoot.FindToken(position);
        var symbol = semanticModel.GetSymbolInfo(token.Parent!, cancellationToken).Symbol
                  ?? semanticModel.GetDeclaredSymbol(token.Parent!, cancellationToken);

        if (symbol is not IFieldSymbol fieldSymbol)
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("SymbolNotField"));

        var fieldName = fieldSymbol.Name;
        var propertyName = command.PropertyName ?? GeneratePropertyName(fieldName);

        // Why: for an Added symbol change, "old" is the source field it was extracted FROM and
        // "new" is the created property, so the guide reads "extracted from X → created Y".
        var oldFqn = SymbolFqn.Of(fieldSymbol);
        var newFqn = SymbolFqn.OfRenamed(fieldSymbol, propertyName);

        // Step 1: Rename the field to the property name across the entire solution.
        // Renamer.RenameSymbolAsync cascades through every reference in every project,
        // so any reader/writer of the field will now address it by the property name.
        // After this step, the declaration site is still a field — we replace it next.
        var renamedSolution = string.Equals(fieldName, propertyName, StringComparison.Ordinal)
            ? solution
            : await Renamer.RenameSymbolAsync(
                solution,
                fieldSymbol,
                new SymbolRenameOptions(),
                propertyName,
                cancellationToken).ConfigureAwait(false);

        // Step 2: In the renamed solution, replace the (now-renamed) field declaration
        // with an auto-property of the same name. Auto-property has no backing field,
        // so external references already point to the right member after the rename.
        var renamedDocument = renamedSolution.GetDocument(documentId);
        if (renamedDocument is null)
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("FailedToLoadDocument"));

        var renamedRoot = await renamedDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var renamedText = await renamedDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
        if (renamedRoot is null)
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("FailedToAnalyzeDocument"));

        // Position may have shifted by rename, so locate the renamed field by the new name.
        var fieldDeclaration = renamedRoot.DescendantNodes()
            .OfType<FieldDeclarationSyntax>()
            .FirstOrDefault(fd => fd.Declaration.Variables
                .Any(v => string.Equals(v.Identifier.Text, propertyName, StringComparison.Ordinal)));

        if (fieldDeclaration is null)
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("CouldNotFindFieldDeclaration"));

        var propertyDecl = BuildAutoPropertyDeclaration(fieldSymbol, propertyName);
        var newRoot = renamedRoot.ReplaceNode(fieldDeclaration, propertyDecl);
        var newDocument = renamedDocument.WithSyntaxRoot(newRoot);
        var newSolution = newDocument.Project.Solution;

        // Aggregate file changes across the entire solution diff.
        var fileChanges = new List<FileChange>();
        var changeCount = 0;
        foreach (var projectId in newSolution.ProjectIds)
        {
            var newProject = newSolution.GetProject(projectId);
            var oldProject = solution.GetProject(projectId);
            if (newProject is null || oldProject is null) continue;

            foreach (var newDoc in newProject.Documents)
            {
                var oldDoc = oldProject.GetDocument(newDoc.Id);
                if (oldDoc is null) continue;

                var newDocText = await newDoc.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var oldDocText = await oldDoc.GetTextAsync(cancellationToken).ConfigureAwait(false);

                if (!newDocText.ContentEquals(oldDocText))
                {
                    var changes = newDocText.GetTextChanges(oldDocText);
                    fileChanges.Add(new FileChange(
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
                oldFqn, newFqn, SymbolChangeTypes.Added.Name, "Property",
                document.FilePath, document.FilePath,
                document.Project.AssemblyName, document.Project.AssemblyName,
                NamespaceLayout.RelativePosition(document.Project, document.FilePath))
        };

        return GenericResult<MutationResult>.Success(
            new MutationResult(
                $"Encapsulated field '{fieldName}' as property '{propertyName}' with {changeCount} changes across {fileChanges.Count} files",
                newSolution,
                fileChanges,
                symbolChanges,
                Array.Empty<PathChange>()));
    }
#pragma warning restore MA0051

    private static string GeneratePropertyName(string fieldName)
    {
        if (fieldName.Length > 1 && fieldName[0] == '_')
            return char.ToUpperInvariant(fieldName[1]) + fieldName.Substring(2);

        if (fieldName.Length > 2 && fieldName.StartsWith("m_", StringComparison.Ordinal))
            return char.ToUpperInvariant(fieldName[2]) + fieldName.Substring(3);

        return char.ToUpperInvariant(fieldName[0]) + fieldName.Substring(1);
    }

    private static PropertyDeclarationSyntax BuildAutoPropertyDeclaration(
        IFieldSymbol fieldSymbol,
        string propertyName)
    {
        var propertyType = SyntaxFactory.ParseTypeName(fieldSymbol.Type.ToDisplayString());
        var accessors = new List<AccessorDeclarationSyntax>
        {
            SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
        };
        if (!fieldSymbol.IsReadOnly)
        {
            accessors.Add(SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));
        }

        var modifiers = SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
        if (fieldSymbol.IsStatic)
        {
            modifiers = modifiers.Add(SyntaxFactory.Token(SyntaxKind.StaticKeyword));
        }

        return SyntaxFactory.PropertyDeclaration(propertyType, propertyName)
            .WithModifiers(modifiers)
            .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.List(accessors)));
    }
}
