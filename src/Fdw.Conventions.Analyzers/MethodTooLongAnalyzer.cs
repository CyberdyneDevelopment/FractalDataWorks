using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Fdw.Conventions.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Fdw.Conventions.Analyzers;

/// <summary>
/// Analyzer that warns when a method exceeds the configured maximum number of executable lines.
/// Replaces MA0051 with configurable thresholds via MSBuild properties and [ConventionOverride].
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MethodTooLongAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for method too long violation.
    /// </summary>
    public const string DiagnosticId = "FDW006";

    private const string Title = "Method is too long";
    private const string MessageFormat = "Method '{0}' has {1} executable lines (threshold: {2})";
    private const string Description = "Methods should be kept short for readability and maintainability. Configure threshold via FDW_MaxMethodLines MSBuild property or [ConventionOverride] attribute.";
    private const string Category = "Maintainability";
    private const int DefaultMaxLines = 60;

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description);

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var maxLines = ConventionOverrideHelper.GetBuildPropertyInt(
                compilationContext.Options.AnalyzerConfigOptionsProvider,
                "FDW_MaxMethodLines",
                DefaultMaxLines);

            compilationContext.RegisterSyntaxNodeAction(
                nodeContext => AnalyzeMethod(nodeContext, maxLines),
                SyntaxKind.MethodDeclaration,
                SyntaxKind.ConstructorDeclaration,
                SyntaxKind.DestructorDeclaration,
                SyntaxKind.LocalFunctionStatement,
                SyntaxKind.SimpleLambdaExpression,
                SyntaxKind.ParenthesizedLambdaExpression,
                SyntaxKind.AnonymousMethodExpression);
        });
    }

    private static void AnalyzeMethod(SyntaxNodeAnalysisContext context, int globalMaxLines)
    {
        var node = context.Node;

        // Get method body
        BlockSyntax? body = null;
        ArrowExpressionClauseSyntax? expressionBody = null;
        string methodName;
        Location location;

        switch (node)
        {
            case MethodDeclarationSyntax method:
                body = method.Body;
                expressionBody = method.ExpressionBody;
                methodName = method.Identifier.Text;
                location = method.Identifier.GetLocation();
                break;
            case ConstructorDeclarationSyntax ctor:
                body = ctor.Body;
                expressionBody = ctor.ExpressionBody;
                methodName = ctor.Identifier.Text;
                location = ctor.Identifier.GetLocation();
                break;
            case DestructorDeclarationSyntax dtor:
                body = dtor.Body;
                expressionBody = dtor.ExpressionBody;
                methodName = "~" + dtor.Identifier.Text;
                location = dtor.Identifier.GetLocation();
                break;
            case LocalFunctionStatementSyntax local:
                body = local.Body;
                expressionBody = local.ExpressionBody;
                methodName = local.Identifier.Text;
                location = local.Identifier.GetLocation();
                break;
            case AnonymousFunctionExpressionSyntax lambda:
                body = lambda.Block;
                methodName = DescribeLambda(lambda);
                location = lambda.GetFirstToken().GetLocation();
                break;
            default:
                return;
        }

        if ((node is AnonymousFunctionExpressionSyntax || node is LocalFunctionStatementSyntax)
            && IsPhaseAuthoringType(node, context.SemanticModel))
            return;

        // Expression-bodied methods count as 1 line - always pass
        if (expressionBody != null && body == null)
            return;

        // No body = abstract/extern/partial - skip
        if (body == null)
            return;

        // Resolve threshold: attribute > MSBuild > default
        var threshold = ConventionOverrideHelper.GetOverrideValue(node, "MaxMethodLines") ?? globalMaxLines;

        // Count executable lines
        var lineCount = CountExecutableLines(body, context.Node.SyntaxTree);
        if (lineCount <= threshold)
            return;

        var diagnostic = Diagnostic.Create(
            Rule,
            location,
            methodName,
            lineCount,
            threshold);

        context.ReportDiagnostic(diagnostic);
    }

    /// <summary>Names a lambda by the member that declares it, for the diagnostic message.</summary>
    private static string DescribeLambda(SyntaxNode lambda)
    {
        foreach (var ancestor in lambda.Ancestors())
        {
            var name = ancestor switch
            {
                MethodDeclarationSyntax method => method.Identifier.Text,
                ConstructorDeclarationSyntax ctor => ctor.Identifier.Text,
                LocalFunctionStatementSyntax local => local.Identifier.Text,
                PropertyDeclarationSyntax property => property.Identifier.Text,
                _ => null
            };

            if (name is not null)
                return name + " (lambda)";
        }

        return "(lambda)";
    }

    /// <summary>Gets the block a nested function owns, or null when it is expression-bodied.</summary>
    private static BlockSyntax? NestedFunctionBlock(SyntaxNode node) => node switch
    {
        AnonymousFunctionExpressionSyntax lambda => lambda.Block,
        LocalFunctionStatementSyntax local => local.Body,
        _ => null
    };

    private static int CountExecutableLines(BlockSyntax body, SyntaxTree syntaxTree)
    {
        var text = syntaxTree.GetText();

        // Get the open and close brace positions to exclude them
        var openBraceEnd = body.OpenBraceToken.Span.End;
        var closeBraceStart = body.CloseBraceToken.SpanStart;

        var nested = new List<TextSpan>();
        foreach (var descendant in body.DescendantNodes())
        {
            var block = NestedFunctionBlock(descendant);
            if (block is not null)
                nested.Add(TextSpan.FromBounds(block.OpenBraceToken.Span.End, block.CloseBraceToken.SpanStart));
        }

        var count = 0;
        var inBlockComment = false;

        foreach (var textLine in text.Lines)
        {
            // Skip lines outside the body span (between braces)
            if (textLine.End <= openBraceEnd || textLine.Start >= closeBraceStart)
                continue;

            if (IsInsideNestedFunction(textLine, nested))
                continue;

            var lineText = textLine.ToString();
            var trimmed = lineText.Trim();

            // Skip empty lines
            if (trimmed.Length == 0)
                continue;

            // Handle block comments
            if (inBlockComment)
            {
                if (trimmed.Contains("*/"))
                    inBlockComment = false;
                continue;
            }

            if (trimmed.StartsWith("/*", StringComparison.Ordinal))
            {
                inBlockComment = !trimmed.Contains("*/");
                continue;
            }

            // Skip single-line comments
            if (trimmed.StartsWith("//", StringComparison.Ordinal))
                continue;

            // Skip lines that are only punctuation closing a construct. Why more than a bare brace: the
            // line that ends a func handed to a call is "});", and one that ends a collection initializer
            // is "};" - neither states anything, but both were counted, so the lambda-heavy style paid a
            // line for every construct it closed.
            if (IsOnlyClosingPunctuation(trimmed))
                continue;

            count++;
        }

        return count;
    }

    /// <summary>Reports whether a line falls entirely inside one of the nested function bodies.</summary>
    private static bool IsInsideNestedFunction(TextLine line, List<TextSpan> nested)
    {
        foreach (var span in nested)
        {
            if (line.Start >= span.Start && line.End <= span.End)
                return true;
        }

        return false;
    }

    /// <summary>Reports whether the node sits inside a class whose job is declaring phase funcs.</summary>
    private static bool IsPhaseAuthoringType(SyntaxNode node, SemanticModel semanticModel)
    {
        foreach (var ancestor in node.Ancestors())
        {
            if (ancestor is not TypeDeclarationSyntax typeDeclaration)
                continue;

            if (semanticModel.GetDeclaredSymbol(typeDeclaration) is not INamedTypeSymbol type)
                return false;

            foreach (var attribute in type.GetAttributes())
            {
                var name = attribute.AttributeClass?.Name;
                if (string.Equals(name, "ServiceTypeOptionAttribute", StringComparison.Ordinal)
                    || string.Equals(name, "ServiceTypeCollectionAttribute", StringComparison.Ordinal)
                    || string.Equals(name, "TypeOptionAttribute", StringComparison.Ordinal)
                    || string.Equals(name, "TypeCollectionAttribute", StringComparison.Ordinal)
                    || string.Equals(name, "PlatformServiceProviderAttribute", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        return false;
    }

    /// <summary>Reports whether a line carries nothing but the punctuation that closes a construct.</summary>
    private static bool IsOnlyClosingPunctuation(string trimmed)
    {
        foreach (var character in trimmed)
        {
            if (character is not ('{' or '}' or '(' or ')' or '[' or ']' or ';' or ','))
                return false;
        }

        return true;
    }
}
