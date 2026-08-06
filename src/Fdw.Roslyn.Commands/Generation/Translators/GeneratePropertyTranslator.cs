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
using Microsoft.CodeAnalysis.Text;

namespace Fdw.Roslyn.Commands.Generation.Translators;

/// <summary>
/// Translator for GeneratePropertyCommand.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "GenerateProperty")]
public sealed class GeneratePropertyTranslator : RoslynCommandTranslatorBase<GeneratePropertyCommand, MutationResult>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GeneratePropertyTranslator"/> class.
    /// </summary>
    public GeneratePropertyTranslator()
        : base("GenerateProperty", "Generates a property")
    {
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Linear code generation: validate property, build via SyntaxFactory
    public override async Task<IGenericResult<MutationResult>> Translate(
        GeneratePropertyCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(command.PropertyName))
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("PropertyNameRequired"));

        if (string.IsNullOrEmpty(command.PropertyType))
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("PropertyTypeRequired"));

        if (!command.HasGetter && !command.HasSetter)
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("PropertyMustHaveGetterOrSetter"));

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

        // Find the type declaration
        var typeDecl = token.Parent?.AncestorsAndSelf()
            .OfType<TypeDeclarationSyntax>()
            .FirstOrDefault();

        if (typeDecl is null)
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("NoTypeDeclarationFoundAtPosition"));

        PropertyDeclarationSyntax property;

        if (command.IsAutoProperty)
        {
            // Auto-property
            var accessors = new List<AccessorDeclarationSyntax>();
            if (command.HasGetter)
                accessors.Add(SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));
            if (command.HasSetter)
                accessors.Add(SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));

            property = SyntaxFactory.PropertyDeclaration(
                    SyntaxFactory.ParseTypeName(command.PropertyType),
                    command.PropertyName)
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.List(accessors)));
        }
        else
        {
            // Full property with backing field
            var fieldName = command.BackingFieldName ??
                $"_{char.ToLowerInvariant(command.PropertyName[0])}{command.PropertyName.Substring(1)}";

            var accessors = new List<AccessorDeclarationSyntax>();

            if (command.HasGetter)
            {
                var getterBody = SyntaxFactory.ArrowExpressionClause(
                    SyntaxFactory.IdentifierName(fieldName));
                accessors.Add(SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                    .WithExpressionBody(getterBody)
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));
            }

            if (command.HasSetter)
            {
                var setterBody = SyntaxFactory.ArrowExpressionClause(
                    SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        SyntaxFactory.IdentifierName(fieldName),
                        SyntaxFactory.IdentifierName("value")));
                accessors.Add(SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                    .WithExpressionBody(setterBody)
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));
            }

            property = SyntaxFactory.PropertyDeclaration(
                    SyntaxFactory.ParseTypeName(command.PropertyType),
                    command.PropertyName)
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.List(accessors)));
        }

        // Add property to the type
        var newTypeDecl = typeDecl.AddMembers(property);
        var newRoot = syntaxRoot.ReplaceNode(typeDecl, newTypeDecl);

        var newDocument = document.WithSyntaxRoot(newRoot);
        var newSolution = newDocument.Project.Solution;

        var fileChanges = new List<FileChange>
        {
            new FileChange(command.FilePath, FileChangeTypes.Modified, document.Project.Name)
            {
                TextChangeCount = 1
            }
        };

        return GenericResult<MutationResult>.Success(
            new MutationResult(
                $"Generated property '{command.PropertyName}' of type '{command.PropertyType}'",
                newSolution,
                fileChanges));
    }
#pragma warning restore MA0051
}
