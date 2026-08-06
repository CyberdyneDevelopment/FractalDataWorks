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
/// Translator for detecting code smells.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "DetectCodeSmells")]
public sealed class DetectCodeSmellsTranslator
    : RoslynCommandTranslatorBase<DetectCodeSmellsCommand, QueryResult<CodeSmellsData>>
{
    private const int LongMethodThreshold = 30;
    private const int LongParameterListThreshold = 5;
    private const int HighComplexityThreshold = 10;
    private const int LargeClassThreshold = 20;

    /// <summary>
    /// Initializes a new instance of the <see cref="DetectCodeSmellsTranslator"/> class.
    /// </summary>
    public DetectCodeSmellsTranslator()
        : base("DetectCodeSmellsTranslator", "Translates code smell detection commands")
    {
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Linear Roslyn flow: iterate methods and types, check smell patterns, aggregate results
    public override async Task<IGenericResult<QueryResult<CodeSmellsData>>> Translate(
        DetectCodeSmellsCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        var documentId = solution.GetDocumentIdsWithFilePath(command.FilePath).FirstOrDefault();
        if (documentId is null)
            return GenericResult<QueryResult<CodeSmellsData>>.Failure(
                RoslynResultCodes.ByName("DocumentNotFound"),
                ResultDetails.Create().With("FilePath", command.FilePath));

        var document = solution.GetDocument(documentId);
        if (document is null)
            return GenericResult<QueryResult<CodeSmellsData>>.Failure(
                RoslynResultCodes.ByName("FailedToLoadDocument"));

        var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (syntaxRoot is null)
            return GenericResult<QueryResult<CodeSmellsData>>.Failure(
                RoslynResultCodes.ByName("FailedToGetSyntaxRoot"));

        var smells = new List<CodeSmell>();

        // Detect long methods
        foreach (var method in syntaxRoot.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            var lineCount = CountLines(method);
            if (lineCount > LongMethodThreshold)
            {
                smells.Add(CreateSmell("LongMethod", method.Identifier.Text, method.GetLocation(),
                    $"Method has {lineCount} lines (threshold: {LongMethodThreshold})", "Medium"));
            }

            // Detect long parameter lists
            if (method.ParameterList.Parameters.Count > LongParameterListThreshold)
            {
                smells.Add(CreateSmell("LongParameterList", method.Identifier.Text, method.GetLocation(),
                    $"Method has {method.ParameterList.Parameters.Count} parameters (threshold: {LongParameterListThreshold})", "Low"));
            }

            // Detect high complexity
            var complexity = CalculateCyclomaticComplexity(method);
            if (complexity > HighComplexityThreshold)
            {
                smells.Add(CreateSmell("HighComplexity", method.Identifier.Text, method.GetLocation(),
                    $"Cyclomatic complexity is {complexity} (threshold: {HighComplexityThreshold})", "High"));
            }

            // Detect deeply nested code
            var maxNesting = CalculateMaxNesting(method);
            if (maxNesting > 3)
            {
                smells.Add(CreateSmell("DeeplyNestedCode", method.Identifier.Text, method.GetLocation(),
                    $"Maximum nesting depth is {maxNesting} (threshold: 3)", "Medium"));
            }
        }

        // Detect large classes
        foreach (var typeDecl in syntaxRoot.DescendantNodes().OfType<TypeDeclarationSyntax>())
        {
            var memberCount = typeDecl.Members.Count;
            if (memberCount > LargeClassThreshold)
            {
                smells.Add(CreateSmell("LargeClass", typeDecl.Identifier.Text, typeDecl.GetLocation(),
                    $"Class has {memberCount} members (threshold: {LargeClassThreshold})", "Medium"));
            }

            // Detect god class (too many dependencies)
            var fieldCount = typeDecl.Members.OfType<FieldDeclarationSyntax>().Count();
            if (fieldCount > 10)
            {
                smells.Add(CreateSmell("TooManyFields", typeDecl.Identifier.Text, typeDecl.GetLocation(),
                    $"Class has {fieldCount} fields (threshold: 10)", "Medium"));
            }
        }

        // Detect empty catch blocks
        foreach (var catchClause in syntaxRoot.DescendantNodes().OfType<CatchClauseSyntax>())
        {
            if (catchClause.Block.Statements.Count == 0)
            {
                var containingMethod = catchClause.Ancestors()
                    .OfType<MethodDeclarationSyntax>()
                    .FirstOrDefault()?.Identifier.Text ?? "unknown";

                smells.Add(CreateSmell("EmptyCatchBlock", containingMethod, catchClause.GetLocation(),
                    "Empty catch block swallows exceptions", "High"));
            }
        }

        // Detect magic numbers
        foreach (var literal in syntaxRoot.DescendantNodes().OfType<LiteralExpressionSyntax>())
        {
            if (literal.IsKind(SyntaxKind.NumericLiteralExpression))
            {
                var value = literal.Token.Value;
                if (value is int intVal && intVal != 0 && intVal != 1 && intVal != -1)
                {
                    var isConstant = literal.Ancestors()
                        .OfType<FieldDeclarationSyntax>()
                        .Any(f => f.Modifiers.Any(SyntaxKind.ConstKeyword) ||
                                  f.Modifiers.Any(SyntaxKind.ReadOnlyKeyword));

                    if (!isConstant)
                    {
                        var containingMethod = literal.Ancestors()
                            .OfType<MethodDeclarationSyntax>()
                            .FirstOrDefault()?.Identifier.Text ?? "unknown";

                        smells.Add(CreateSmell("MagicNumber", containingMethod, literal.GetLocation(),
                            $"Magic number {intVal} should be a named constant", "Low"));
                    }
                }
            }
        }

        var highSeverity = smells.Count(s => string.Equals(s.Severity, "High", StringComparison.Ordinal));
        var mediumSeverity = smells.Count(s => string.Equals(s.Severity, "Medium", StringComparison.Ordinal));
        var lowSeverity = smells.Count(s => string.Equals(s.Severity, "Low", StringComparison.Ordinal));

        var data = new CodeSmellsData
        {
            Smells = smells,
            Summary = new CodeSmellsSummary
            {
                Total = smells.Count,
                High = highSeverity,
                Medium = mediumSeverity,
                Low = lowSeverity
            }
        };

        var result = new QueryResult<CodeSmellsData>(
            $"Detected {smells.Count} code smells: {highSeverity} high, {mediumSeverity} medium, {lowSeverity} low",
            data);

        return GenericResult<QueryResult<CodeSmellsData>>.Success(result);
    }
#pragma warning restore MA0051

    private static CodeSmell CreateSmell(string type, string member, Location location, string description, string severity)
    {
        var lineSpan = location.GetLineSpan();
        return new CodeSmell
        {
            Type = type,
            Member = member,
            Description = description,
            Severity = severity,
            Line = lineSpan.StartLinePosition.Line + 1,
            Column = lineSpan.StartLinePosition.Character + 1
        };
    }

    private static int CountLines(SyntaxNode node)
    {
        var span = node.GetLocation().GetLineSpan();
        return span.EndLinePosition.Line - span.StartLinePosition.Line + 1;
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

    private static int CalculateMaxNesting(MethodDeclarationSyntax method)
    {
        var maxNesting = 0;

        foreach (var node in method.DescendantNodes())
        {
            if (node is BlockSyntax or IfStatementSyntax or ForStatementSyntax
                or ForEachStatementSyntax or WhileStatementSyntax or DoStatementSyntax
                or TryStatementSyntax)
            {
                var depth = node.Ancestors().Count(a =>
                    a is BlockSyntax or IfStatementSyntax or ForStatementSyntax
                    or ForEachStatementSyntax or WhileStatementSyntax or DoStatementSyntax
                    or TryStatementSyntax);

                if (depth > maxNesting)
                    maxNesting = depth;
            }
        }

        return maxNesting;
    }
}
