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
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Fdw.Roslyn.Commands.Refactoring.Translators;

/// <summary>
/// Translator for ExtractMethodCommand.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "ExtractMethod")]
public sealed class ExtractMethodTranslator : RoslynCommandTranslatorBase<ExtractMethodCommand, MutationResult>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExtractMethodTranslator"/> class.
    /// </summary>
    public ExtractMethodTranslator()
        : base("ExtractMethod", "Extracts selected code into a new method")
    {
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Linear Roslyn flow: analyze data flow, build extracted method, replace with call
    public override async Task<IGenericResult<MutationResult>> Translate(
        ExtractMethodCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        ExtractMethodTranslatorLog.Extracting(Logger, command.FilePath, command.MethodName, command.StartLine, command.StartColumn, command.EndLine, command.EndColumn);

        var documentId = solution.GetDocumentIdsWithFilePath(command.FilePath).FirstOrDefault();
        if (documentId is null)
        {
            ExtractMethodTranslatorLog.DocumentNotFound(Logger, command.FilePath);
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("DocumentNotFound"),
                ResultDetails.Create().With("FilePath", command.FilePath));
        }

        var document = solution.GetDocument(documentId);
        if (document is null)
        {
            ExtractMethodTranslatorLog.FailedToLoadDocument(Logger, command.FilePath);
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("FailedToLoadDocument"));
        }

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

        if (semanticModel is null || syntaxRoot is null)
        {
            ExtractMethodTranslatorLog.FailedToAnalyzeDocument(Logger, command.FilePath);
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("FailedToAnalyzeDocument"));
        }

        var startPosition = text.Lines.GetPosition(new LinePosition(command.StartLine - 1, command.StartColumn - 1));
        var endPosition = text.Lines.GetPosition(new LinePosition(command.EndLine - 1, command.EndColumn - 1));
        var span = TextSpan.FromBounds(startPosition, endPosition);

        // Find statements in the selection
        var selectedNodes = syntaxRoot.DescendantNodes()
            .Where(n => span.Contains(n.Span) && n is StatementSyntax)
            .Cast<StatementSyntax>()
            .ToList();

        if (selectedNodes.Count == 0)
        {
            ExtractMethodTranslatorLog.NoStatementsFoundInSelectedRange(Logger, command.FilePath);
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("NoStatementsFoundInSelectedRange"));
        }

        // Find containing method
        var containingMethod = selectedNodes[0].Ancestors()
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault();

        if (containingMethod is null)
        {
            ExtractMethodTranslatorLog.SelectedCodeNotWithinMethod(Logger, command.FilePath);
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("SelectedCodeNotWithinMethod"));
        }

        // The containing method declaration is already bound against this semanticModel, so
        // GetDeclaredSymbol is guaranteed to resolve here.
        var containingMethodSymbol = semanticModel.GetDeclaredSymbol(containingMethod, cancellationToken)!;
        var oldFqn = SymbolFqn.Of(containingMethodSymbol);
        var newFqn = SymbolFqn.OfRenamed(containingMethodSymbol, command.MethodName);

        // Analyze data flow
        var dataFlow = semanticModel.AnalyzeDataFlow(selectedNodes.First(), selectedNodes.Last());
        if (dataFlow is null || !dataFlow.Succeeded)
        {
            ExtractMethodTranslatorLog.FailedToAnalyzeDataFlow(Logger, command.FilePath);
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("FailedToAnalyzeDataFlow"));
        }

        var parameters = dataFlow.DataFlowsIn
            .Where(s => s is ILocalSymbol or IParameterSymbol)
            .Select(s => new { Symbol = s, Name = s.Name, Type = GetSymbolType(s) })
            .Distinct()
            .ToList();

        var returnVariable = dataFlow.DataFlowsOut.FirstOrDefault();
        var returnType = returnVariable is not null ? GetSymbolType(returnVariable) : "void";

        // Build the extracted method
        var parameterList = SyntaxFactory.ParameterList(
            SyntaxFactory.SeparatedList(
                parameters.Select(p =>
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier(p.Name))
                        .WithType(SyntaxFactory.ParseTypeName(p.Type)))));

        var methodBody = SyntaxFactory.Block(selectedNodes);

        var newMethod = SyntaxFactory.MethodDeclaration(
                SyntaxFactory.ParseTypeName(returnType),
                command.MethodName)
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)))
            .WithParameterList(parameterList)
            .WithBody(methodBody);

        // Replace selected statements with method call
        var arguments = SyntaxFactory.ArgumentList(
            SyntaxFactory.SeparatedList(
                parameters.Select(p =>
                    SyntaxFactory.Argument(SyntaxFactory.IdentifierName(p.Name)))));

        var methodCall = SyntaxFactory.InvocationExpression(
            SyntaxFactory.IdentifierName(command.MethodName),
            arguments);

        StatementSyntax callStatement = returnVariable is not null
            ? SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(
                    SyntaxFactory.ParseTypeName(returnType),
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(returnVariable.Name)
                            .WithInitializer(SyntaxFactory.EqualsValueClause(methodCall)))))
            : SyntaxFactory.ExpressionStatement(methodCall);

        // Replace the first selected statement with the call, remove the rest
        var newRoot = syntaxRoot.ReplaceNode(selectedNodes.First(), callStatement);
        if (selectedNodes.Count > 1)
        {
            var nodesToRemove = selectedNodes.Skip(1).Select(n => newRoot.DescendantNodes().FirstOrDefault(d => d.IsEquivalentTo(n))).Where(n => n is not null);
            newRoot = newRoot.RemoveNodes(nodesToRemove!, SyntaxRemoveOptions.KeepNoTrivia);
        }

        // Add the new method after the containing method
        var containingType = containingMethod.Parent as TypeDeclarationSyntax;
        if (containingType is null)
        {
            ExtractMethodTranslatorLog.CouldNotFindContainingType(Logger, command.FilePath, containingMethod.Identifier.Text);
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("CouldNotFindContainingType"));
        }

        var newContainingType = containingType.InsertNodesAfter(
            containingType.Members.First(m => m == containingMethod),
            new[] { newMethod });

        newRoot = syntaxRoot.ReplaceNode(containingType, newContainingType);

        var newDocument = document.WithSyntaxRoot(newRoot);
        var newSolution = newDocument.Project.Solution;

        var fileChanges = new List<FileChange>
        {
            new FileChange(command.FilePath, FileChangeTypes.Modified, document.Project.Name)
            {
                TextChangeCount = selectedNodes.Count + 1
            }
        };

        var symbolChanges = new List<SymbolChange>
        {
            new SymbolChange(
                oldFqn, newFqn, SymbolChangeTypes.Added.Name, "Method",
                document.FilePath, document.FilePath,
                document.Project.AssemblyName, document.Project.AssemblyName,
                NamespaceLayout.RelativePosition(document.Project, document.FilePath))
        };

        ExtractMethodTranslatorLog.Extracted(Logger, command.MethodName, containingMethod.Identifier.Text, selectedNodes.Count);

        return GenericResult<MutationResult>.Success(
            new MutationResult(
                $"Extracted method '{command.MethodName}' from '{containingMethod.Identifier.Text}' with {selectedNodes.Count} statements",
                newSolution,
                fileChanges,
                symbolChanges,
                Array.Empty<PathChange>()));
    }
#pragma warning restore MA0051

    private static string GetSymbolType(ISymbol symbol)
    {
        return symbol switch
        {
            ILocalSymbol local => local.Type.ToDisplayString(),
            IParameterSymbol param => param.Type.ToDisplayString(),
            _ => "object"
        };
    }
}
