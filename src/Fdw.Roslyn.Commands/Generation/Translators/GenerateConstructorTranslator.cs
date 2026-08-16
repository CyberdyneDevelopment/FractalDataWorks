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
using Fdw.Roslyn.Commands.Logging;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Fdw.Roslyn.Commands.Generation.Translators;

/// <summary>
/// Translator for GenerateConstructorCommand.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "GenerateConstructor")]
public sealed class GenerateConstructorTranslator : RoslynCommandTranslatorBase<GenerateConstructorCommand, MutationResult>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GenerateConstructorTranslator"/> class.
    /// </summary>
    public GenerateConstructorTranslator()
        : base("GenerateConstructor", "Generates a constructor for a class")
    {
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Linear code generation: find fields, build constructor via SyntaxFactory
    public override async Task<IGenericResult<MutationResult>> Translate(
        GenerateConstructorCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        GenerateConstructorTranslatorLog.Generating(Logger, command.FilePath, command.Line, command.Column);

        var documentId = solution.GetDocumentIdsWithFilePath(command.FilePath).FirstOrDefault();
        if (documentId is null)
        {
            GenerateConstructorTranslatorLog.DocumentNotFound(Logger, command.FilePath);
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("DocumentNotFound"),
                ResultDetails.Create().With("FilePath", command.FilePath));
        }

        var document = solution.GetDocument(documentId);
        if (document is null)
        {
            GenerateConstructorTranslatorLog.FailedToLoadDocument(Logger, command.FilePath);
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("FailedToLoadDocument"));
        }

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

        if (semanticModel is null || syntaxRoot is null)
        {
            GenerateConstructorTranslatorLog.FailedToAnalyzeDocument(Logger, command.FilePath);
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
            GenerateConstructorTranslatorLog.NoTypeDeclarationFoundAtPosition(Logger, command.FilePath, command.Line, command.Column);
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("NoTypeDeclarationFoundAtPosition"));
        }

        var symbol = semanticModel.GetDeclaredSymbol(typeDecl, cancellationToken);
        if (symbol is not INamedTypeSymbol typeSymbol)
        {
            GenerateConstructorTranslatorLog.FailedToGetTypeSymbol(Logger, command.FilePath, command.Line, command.Column);
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("FailedToGetTypeSymbol"));
        }

        var typeName = typeSymbol.Name;

        // Get fields for constructor parameters
        var fields = typeSymbol.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(f => !f.IsStatic && !f.IsConst)
            .Where(f => command.IncludeReadonlyFields || !f.IsReadOnly)
            .ToList();

        if (fields.Count == 0)
        {
            GenerateConstructorTranslatorLog.NoFieldsFoundToGenerateConstructorParameters(Logger, typeName);
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("NoFieldsFoundToGenerateConstructorParameters"));
        }

        // Build constructor parameters
        var parameters = new List<ParameterSyntax>();
        var assignments = new List<StatementSyntax>();

        foreach (var field in fields)
        {
            var paramName = ToCamelCase(field.Name);
            var paramType = SyntaxFactory.ParseTypeName(field.Type.ToDisplayString());

            parameters.Add(
                SyntaxFactory.Parameter(SyntaxFactory.Identifier(paramName))
                    .WithType(paramType));

            var assignment = SyntaxFactory.ExpressionStatement(
                SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.IdentifierName(field.Name),
                    SyntaxFactory.IdentifierName(paramName)));

            assignments.Add(assignment);
        }

        // Build XML documentation
        var xmlTrivia = new List<SyntaxTrivia>
        {
            SyntaxFactory.Trivia(
                SyntaxFactory.DocumentationCommentTrivia(
                    SyntaxKind.SingleLineDocumentationCommentTrivia,
                    SyntaxFactory.List(new XmlNodeSyntax[]
                    {
                        SyntaxFactory.XmlText("/// "),
                        SyntaxFactory.XmlSummaryElement(
                            SyntaxFactory.XmlText($"Initializes a new instance of the <see cref=\"{typeName}\"/> class.")),
                        SyntaxFactory.XmlText("\n/// ")
                    })))
        };

        // Add parameter documentation
        foreach (var field in fields)
        {
            var paramName = ToCamelCase(field.Name);
            xmlTrivia.Add(
                SyntaxFactory.Trivia(
                    SyntaxFactory.DocumentationCommentTrivia(
                        SyntaxKind.SingleLineDocumentationCommentTrivia,
                        SyntaxFactory.List(new XmlNodeSyntax[]
                        {
                            SyntaxFactory.XmlParamElement(
                                paramName,
                                SyntaxFactory.XmlText($"The {field.Name.TrimStart('_')}."))
                        }))));
        }

        // Build constructor
        var constructor = SyntaxFactory.ConstructorDeclaration(typeName)
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters)))
            .WithBody(SyntaxFactory.Block(assignments));

        // Add constructor to the type
        var newTypeDecl = typeDecl.AddMembers(constructor);
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

        GenerateConstructorTranslatorLog.Generated(Logger, typeName, fields.Count);

        return GenericResult<MutationResult>.Success(
            new MutationResult(
                $"Generated constructor for '{typeName}' with {fields.Count} parameters",
                newSolution,
                fileChanges));
    }
#pragma warning restore MA0051

    private static string ToCamelCase(string fieldName)
    {
        // Remove leading underscore and convert to camelCase
        if (fieldName.Length > 1 && fieldName[0] == '_')
            return char.ToLowerInvariant(fieldName[1]) + fieldName.Substring(2);

        return char.ToLowerInvariant(fieldName[0]) + fieldName.Substring(1);
    }
}
