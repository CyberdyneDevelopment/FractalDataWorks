#pragma warning disable CA1305 // Specify IFormatProvider - code generation uses invariant strings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Generation.Commands;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fdw.Roslyn.Commands.Generation.Translators;

/// <summary>
/// Translator for GenerateDocsCommand.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "GenerateDocs")]
public sealed class GenerateDocsTranslator : RoslynCommandTranslatorBase<GenerateDocsCommand, MutationResult>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GenerateDocsTranslator"/> class.
    /// </summary>
    public GenerateDocsTranslator()
        : base("GenerateDocs", "Generates XML documentation for code members")
    {
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Linear Roslyn flow: find undocumented members, generate XML documentation
    public override async Task<IGenericResult<MutationResult>> Translate(
        GenerateDocsCommand command,
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

        if (semanticModel is null || syntaxRoot is null)
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("FailedToAnalyzeDocument"));

        var nodesToDocument = new List<SyntaxNode>();
        var replacements = new Dictionary<SyntaxNode, SyntaxNode>();
        var documentedCount = 0;

        // Generate docs for types
        foreach (var typeDecl in syntaxRoot.DescendantNodes().OfType<TypeDeclarationSyntax>())
        {
            if (semanticModel.GetDeclaredSymbol(typeDecl, cancellationToken) is not INamedTypeSymbol typeSymbol)
                continue;

            if (!command.IncludePrivate && typeSymbol.DeclaredAccessibility == Accessibility.Private)
                continue;

            if (!HasDocumentation(typeDecl))
            {
                var docComment = GenerateTypeDoc(typeSymbol);
                var newTypeDecl = typeDecl.WithLeadingTrivia(
                    typeDecl.GetLeadingTrivia().Insert(0, docComment));
                replacements[typeDecl] = newTypeDecl;
                documentedCount++;
            }
        }

        // Generate docs for methods
        foreach (var methodDecl in syntaxRoot.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            if (semanticModel.GetDeclaredSymbol(methodDecl, cancellationToken) is not IMethodSymbol methodSymbol)
                continue;

            if (!command.IncludePrivate && methodSymbol.DeclaredAccessibility == Accessibility.Private)
                continue;

            if (!HasDocumentation(methodDecl))
            {
                var docComment = GenerateMethodDoc(methodSymbol);
                var newMethodDecl = methodDecl.WithLeadingTrivia(
                    methodDecl.GetLeadingTrivia().Insert(0, docComment));
                replacements[methodDecl] = newMethodDecl;
                documentedCount++;
            }
        }

        // Generate docs for properties
        foreach (var propDecl in syntaxRoot.DescendantNodes().OfType<PropertyDeclarationSyntax>())
        {
            if (semanticModel.GetDeclaredSymbol(propDecl, cancellationToken) is not IPropertySymbol propSymbol)
                continue;

            if (!command.IncludePrivate && propSymbol.DeclaredAccessibility == Accessibility.Private)
                continue;

            if (!HasDocumentation(propDecl))
            {
                var docComment = GeneratePropertyDoc(propSymbol);
                var newPropDecl = propDecl.WithLeadingTrivia(
                    propDecl.GetLeadingTrivia().Insert(0, docComment));
                replacements[propDecl] = newPropDecl;
                documentedCount++;
            }
        }

        if (documentedCount == 0)
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("NoUndocumentedMembersFound"));

        var newRoot = syntaxRoot.ReplaceNodes(replacements.Keys, (original, _) => replacements[original]);

        var newDocument = document.WithSyntaxRoot(newRoot);
        var newSolution = newDocument.Project.Solution;

        var fileChanges = new List<FileChange>
        {
            new FileChange(command.FilePath, FileChangeTypes.Modified, document.Project.Name)
            {
                TextChangeCount = documentedCount
            }
        };

        return GenericResult<MutationResult>.Success(
            new MutationResult(
                $"Generated documentation for {documentedCount} members",
                newSolution,
                fileChanges));
    }
#pragma warning restore MA0051

    private static bool HasDocumentation(SyntaxNode node)
    {
        var trivia = node.GetLeadingTrivia();
        return trivia.Any(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                               t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia));
    }

    private static SyntaxTrivia GenerateTypeDoc(INamedTypeSymbol typeSymbol)
    {
        var typeKind = typeSymbol.TypeKind.ToString().ToLowerInvariant();
        var comment = $"/// <summary>\n/// Represents the {typeSymbol.Name} {typeKind}.\n/// </summary>\n";
        return SyntaxFactory.ParseLeadingTrivia(comment)[0];
    }

    private static SyntaxTrivia GenerateMethodDoc(IMethodSymbol methodSymbol)
    {
        var lines = new List<string>
        {
            "/// <summary>",
            $"/// {methodSymbol.Name} method.",
            "/// </summary>"
        };

        foreach (var param in methodSymbol.Parameters)
        {
            lines.Add($"/// <param name=\"{param.Name}\">The {param.Name}.</param>");
        }

        if (!methodSymbol.ReturnsVoid)
        {
            lines.Add("/// <returns>The result.</returns>");
        }

        var comment = string.Join("\n", lines) + "\n";
        return SyntaxFactory.ParseLeadingTrivia(comment)[0];
    }

    private static SyntaxTrivia GeneratePropertyDoc(IPropertySymbol propertySymbol)
    {
        var action = propertySymbol.SetMethod is not null ? "Gets or sets" : "Gets";
        var comment = $"/// <summary>\n/// {action} the {propertySymbol.Name}.\n/// </summary>\n";
        return SyntaxFactory.ParseLeadingTrivia(comment)[0];
    }
}
