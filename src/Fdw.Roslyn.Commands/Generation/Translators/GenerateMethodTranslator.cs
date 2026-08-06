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
/// Translator for GenerateMethodCommand.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "GenerateMethod")]
public sealed class GenerateMethodTranslator : RoslynCommandTranslatorBase<GenerateMethodCommand, MutationResult>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GenerateMethodTranslator"/> class.
    /// </summary>
    public GenerateMethodTranslator()
        : base("GenerateMethod", "Generates a method signature")
    {
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Linear code generation: validate method, build via verbose SyntaxFactory calls
    public override async Task<IGenericResult<MutationResult>> Translate(
        GenerateMethodCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(command.MethodName))
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("MethodNameRequired"));

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

        // Adjust return type for async
        var actualReturnType = command.ReturnType;
        if (command.IsAsync && !command.ReturnType.StartsWith("Task", StringComparison.Ordinal))
        {
            actualReturnType = string.Equals(command.ReturnType, "void", StringComparison.Ordinal)
                ? "Task"
                : $"Task<{command.ReturnType}>";
        }

        // Parse parameters
        var paramList = string.IsNullOrEmpty(command.Parameters)
            ? Array.Empty<string>()
            : command.Parameters.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // Build parameter list syntax
        var parameters = new List<ParameterSyntax>();
        foreach (var param in paramList)
        {
            var parts = param.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                var paramType = SyntaxFactory.ParseTypeName(parts[0]);
                var paramName = SyntaxFactory.Identifier(parts[1]);
                parameters.Add(SyntaxFactory.Parameter(paramName).WithType(paramType));
            }
        }

        // Build accessibility modifier
        var accessibilityKind = command.Accessibility.ToLowerInvariant() switch
        {
            "private" => SyntaxKind.PrivateKeyword,
            "protected" => SyntaxKind.ProtectedKeyword,
            "internal" => SyntaxKind.InternalKeyword,
            _ => SyntaxKind.PublicKeyword
        };

        var modifiers = new List<SyntaxToken> { SyntaxFactory.Token(accessibilityKind) };
        if (command.IsAsync)
            modifiers.Add(SyntaxFactory.Token(SyntaxKind.AsyncKeyword));

        // Build method body
        StatementSyntax bodyStatement;
        if (string.Equals(actualReturnType, "void", StringComparison.Ordinal))
        {
            bodyStatement = SyntaxFactory.ThrowStatement(
                SyntaxFactory.ObjectCreationExpression(
                    SyntaxFactory.ParseTypeName("NotImplementedException"))
                .WithArgumentList(SyntaxFactory.ArgumentList()));
        }
        else if (string.Equals(actualReturnType, "Task", StringComparison.Ordinal))
        {
            if (command.IsAsync)
            {
                var awaitStatement = SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.AwaitExpression(
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName("Task"),
                                    SyntaxFactory.IdentifierName("CompletedTask")),
                                SyntaxFactory.IdentifierName("ConfigureAwait")))
                        .WithArgumentList(
                            SyntaxFactory.ArgumentList(
                                SyntaxFactory.SingletonSeparatedList(
                                    SyntaxFactory.Argument(
                                        SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression)))))));

                bodyStatement = SyntaxFactory.Block(
                    awaitStatement,
                    SyntaxFactory.ThrowStatement(
                        SyntaxFactory.ObjectCreationExpression(
                            SyntaxFactory.ParseTypeName("NotImplementedException"))
                        .WithArgumentList(SyntaxFactory.ArgumentList())));
            }
            else
            {
                bodyStatement = SyntaxFactory.ThrowStatement(
                    SyntaxFactory.ObjectCreationExpression(
                        SyntaxFactory.ParseTypeName("NotImplementedException"))
                    .WithArgumentList(SyntaxFactory.ArgumentList()));
            }
        }
        else
        {
            bodyStatement = SyntaxFactory.ThrowStatement(
                SyntaxFactory.ObjectCreationExpression(
                    SyntaxFactory.ParseTypeName("NotImplementedException"))
                .WithArgumentList(SyntaxFactory.ArgumentList()));
        }

        // Build the method
        var method = SyntaxFactory.MethodDeclaration(
                SyntaxFactory.ParseTypeName(actualReturnType),
                command.MethodName)
            .WithModifiers(SyntaxFactory.TokenList(modifiers))
            .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters)))
            .WithBody(bodyStatement is BlockSyntax block ? block : SyntaxFactory.Block(bodyStatement));

        // Add method to the type
        var newTypeDecl = typeDecl.AddMembers(method);
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
                $"Generated method '{command.MethodName}' with {paramList.Length} parameters",
                newSolution,
                fileChanges));
    }
#pragma warning restore MA0051
}
