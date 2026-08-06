using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Analysis.Commands;
using Fdw.Roslyn.Commands.Analysis.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fdw.Roslyn.Commands.Analysis.Translators;

/// <summary>
/// Translator for analyzing cyclomatic complexity.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "AnalyzeComplexity")]
public sealed class AnalyzeComplexityTranslator
    : RoslynCommandTranslatorBase<AnalyzeComplexityCommand, QueryResult<ComplexityAnalysisData>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AnalyzeComplexityTranslator"/> class.
    /// </summary>
    public AnalyzeComplexityTranslator()
        : base("AnalyzeComplexityTranslator", "Translates complexity analysis commands")
    {
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult<QueryResult<ComplexityAnalysisData>>> Translate(
        AnalyzeComplexityCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        var documentId = solution.GetDocumentIdsWithFilePath(command.FilePath).FirstOrDefault();
        if (documentId is null)
            return GenericResult<QueryResult<ComplexityAnalysisData>>.Failure(
                RoslynResultCodes.ByName("DocumentNotFound"),
                ResultDetails.Create().With("FilePath", command.FilePath));

        var document = solution.GetDocument(documentId);
        if (document is null)
            return GenericResult<QueryResult<ComplexityAnalysisData>>.Failure(
                RoslynResultCodes.ByName("FailedToLoadDocument"));

        var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (syntaxRoot is null)
            return GenericResult<QueryResult<ComplexityAnalysisData>>.Failure(
                RoslynResultCodes.ByName("FailedToGetSyntaxRoot"));

        var methods = new List<MethodComplexity>();

        foreach (var method in syntaxRoot.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            var complexity = CalculateCyclomaticComplexity(method);
            var lineSpan = method.GetLocation().GetLineSpan();

            methods.Add(new MethodComplexity
            {
                MethodName = method.Identifier.Text,
                Complexity = complexity,
                Line = lineSpan.StartLinePosition.Line + 1,
                ExceedsThreshold = complexity > command.Threshold,
                ContainingType = method.Parent is TypeDeclarationSyntax td ? td.Identifier.Text : string.Empty
            });
        }

        var highComplexity = methods.Where(m => m.ExceedsThreshold).ToList();

        var data = new ComplexityAnalysisData
        {
            Methods = methods,
            HighComplexityMethods = highComplexity,
            Threshold = command.Threshold,
            Count = methods.Count,
            HighCount = highComplexity.Count
        };

        var result = new QueryResult<ComplexityAnalysisData>(
            $"Analyzed {methods.Count} methods, {highComplexity.Count} exceed threshold",
            data);

        return GenericResult<QueryResult<ComplexityAnalysisData>>.Success(result);
    }

    private static int CalculateCyclomaticComplexity(MethodDeclarationSyntax method)
    {
        var complexity = 1;

        foreach (var node in method.DescendantNodes())
        {
#pragma warning disable FDW018 // External Roslyn SyntaxKind enum — cannot convert to TypeCollection
            complexity += node.Kind() switch
            {
                SyntaxKind.IfStatement => 1,
                SyntaxKind.SwitchSection => 1,
                SyntaxKind.ForStatement => 1,
                SyntaxKind.ForEachStatement => 1,
                SyntaxKind.WhileStatement => 1,
                SyntaxKind.DoStatement => 1,
                SyntaxKind.CatchClause => 1,
                SyntaxKind.ConditionalExpression => 1,
                SyntaxKind.CoalesceExpression => 1,
                SyntaxKind.LogicalAndExpression => 1,
                SyntaxKind.LogicalOrExpression => 1,
                SyntaxKind.SwitchExpressionArm => 1,
                _ => 0
            };
#pragma warning restore FDW018
        }

        return complexity;
    }
}
