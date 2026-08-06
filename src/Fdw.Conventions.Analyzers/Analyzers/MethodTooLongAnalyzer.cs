using System;
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
                SyntaxKind.DestructorDeclaration);
        });
    }

    private static void AnalyzeMethod(SyntaxNodeAnalysisContext context, int globalMaxLines)
    {
        var node = context.Node;

        // Get method body
        BlockSyntax? body = null;
        ArrowExpressionClauseSyntax? expressionBody = null;
        string methodName;

        switch (node)
        {
            case MethodDeclarationSyntax method:
                body = method.Body;
                expressionBody = method.ExpressionBody;
                methodName = method.Identifier.Text;
                break;
            case ConstructorDeclarationSyntax ctor:
                body = ctor.Body;
                expressionBody = ctor.ExpressionBody;
                methodName = ctor.Identifier.Text;
                break;
            case DestructorDeclarationSyntax dtor:
                body = dtor.Body;
                expressionBody = dtor.ExpressionBody;
                methodName = "~" + dtor.Identifier.Text;
                break;
            default:
                return;
        }

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

        var identifier = node switch
        {
            MethodDeclarationSyntax m => m.Identifier,
            ConstructorDeclarationSyntax c => c.Identifier,
            DestructorDeclarationSyntax d => d.Identifier,
            _ => default
        };

        if (identifier == default)
            return;

        var diagnostic = Diagnostic.Create(
            Rule,
            identifier.GetLocation(),
            methodName,
            lineCount,
            threshold);

        context.ReportDiagnostic(diagnostic);
    }

    private static int CountExecutableLines(BlockSyntax body, SyntaxTree syntaxTree)
    {
        var text = syntaxTree.GetText();

        // Get the open and close brace positions to exclude them
        var openBraceEnd = body.OpenBraceToken.Span.End;
        var closeBraceStart = body.CloseBraceToken.SpanStart;

        var count = 0;
        var inBlockComment = false;

        foreach (var textLine in text.Lines)
        {
            // Skip lines outside the body span (between braces)
            if (textLine.End <= openBraceEnd || textLine.Start >= closeBraceStart)
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

            // Skip lines that are only braces
            if (string.Equals(trimmed, "{", StringComparison.Ordinal) || string.Equals(trimmed, "}", StringComparison.Ordinal))
                continue;

            count++;
        }

        return count;
    }
}
