using System;
using System.Collections.Immutable;
using Fdw.Conventions.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fdw.Conventions.Analyzers;

/// <summary>
/// Analyzer that warns when a method exceeds the configured maximum cyclomatic complexity.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MethodTooComplexAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for method too complex violation.
    /// </summary>
    public const string DiagnosticId = "FDW007";

    private const string Title = "Method is too complex";
    private const string MessageFormat = "Method '{0}' has cyclomatic complexity {1} (threshold: {2})";
    private const string Description = "Methods with high cyclomatic complexity are hard to test and maintain. Configure threshold via FDW_MaxCyclomaticComplexity MSBuild property or [ConventionOverride] attribute.";
    private const string Category = "Maintainability";
    private const int DefaultMaxComplexity = 10;

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
            var maxComplexity = ConventionOverrideHelper.GetBuildPropertyInt(
                compilationContext.Options.AnalyzerConfigOptionsProvider,
                "FDW_MaxCyclomaticComplexity",
                DefaultMaxComplexity);

            compilationContext.RegisterSyntaxNodeAction(
                nodeContext => AnalyzeMethod(nodeContext, maxComplexity),
                SyntaxKind.MethodDeclaration,
                SyntaxKind.ConstructorDeclaration,
                SyntaxKind.DestructorDeclaration);
        });
    }

    private static void AnalyzeMethod(SyntaxNodeAnalysisContext context, int globalMaxComplexity)
    {
        var node = context.Node;

        // Get method body
        SyntaxNode? bodyNode = null;
        string methodName;

        switch (node)
        {
            case MethodDeclarationSyntax method:
                bodyNode = (SyntaxNode?)method.Body ?? method.ExpressionBody;
                methodName = method.Identifier.Text;
                break;
            case ConstructorDeclarationSyntax ctor:
                bodyNode = (SyntaxNode?)ctor.Body ?? ctor.ExpressionBody;
                methodName = ctor.Identifier.Text;
                break;
            case DestructorDeclarationSyntax dtor:
                bodyNode = (SyntaxNode?)dtor.Body ?? dtor.ExpressionBody;
                methodName = "~" + dtor.Identifier.Text;
                break;
            default:
                return;
        }

        // No body = abstract/extern/partial - skip
        if (bodyNode == null)
            return;

        // Resolve threshold
        var threshold = ConventionOverrideHelper.GetOverrideValue(node, "MaxCyclomaticComplexity") ?? globalMaxComplexity;

        // Calculate complexity
        var walker = new CyclomaticComplexityWalker();
        walker.Visit(bodyNode);
        var complexity = walker.Complexity;

        if (complexity <= threshold)
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
            complexity,
            threshold);

        context.ReportDiagnostic(diagnostic);
    }

    /// <summary>
    /// Walks the syntax tree to calculate cyclomatic complexity.
    /// Skips nested lambdas, anonymous methods, and local functions.
    /// </summary>
    private sealed class CyclomaticComplexityWalker : CSharpSyntaxWalker
    {
        public int Complexity { get; private set; } = 1;

        public override void VisitIfStatement(IfStatementSyntax node)
        {
            Complexity++;
            base.VisitIfStatement(node);
        }

        public override void VisitWhileStatement(WhileStatementSyntax node)
        {
            Complexity++;
            base.VisitWhileStatement(node);
        }

        public override void VisitForStatement(ForStatementSyntax node)
        {
            Complexity++;
            base.VisitForStatement(node);
        }

        public override void VisitForEachStatement(ForEachStatementSyntax node)
        {
            Complexity++;
            base.VisitForEachStatement(node);
        }

        public override void VisitDoStatement(DoStatementSyntax node)
        {
            Complexity++;
            base.VisitDoStatement(node);
        }

        public override void VisitCatchClause(CatchClauseSyntax node)
        {
            Complexity++;
            base.VisitCatchClause(node);
        }

        public override void VisitCaseSwitchLabel(CaseSwitchLabelSyntax node)
        {
            Complexity++;
            base.VisitCaseSwitchLabel(node);
        }

        public override void VisitCasePatternSwitchLabel(CasePatternSwitchLabelSyntax node)
        {
            Complexity++;
            base.VisitCasePatternSwitchLabel(node);
        }

        public override void VisitSwitchExpressionArm(SwitchExpressionArmSyntax node)
        {
            // Don't count the discard pattern arm (default case)
            if (node.Pattern is not DiscardPatternSyntax)
            {
                Complexity++;
            }
            base.VisitSwitchExpressionArm(node);
        }

        public override void VisitConditionalExpression(ConditionalExpressionSyntax node)
        {
            Complexity++;
            base.VisitConditionalExpression(node);
        }

        public override void VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            if (node.IsKind(SyntaxKind.CoalesceExpression) ||
                node.IsKind(SyntaxKind.LogicalAndExpression) ||
                node.IsKind(SyntaxKind.LogicalOrExpression))
            {
                Complexity++;
            }
            base.VisitBinaryExpression(node);
        }

        public override void VisitConditionalAccessExpression(ConditionalAccessExpressionSyntax node)
        {
            Complexity++;
            base.VisitConditionalAccessExpression(node);
        }

        // Skip nested constructs - they get their own complexity count

        public override void VisitLocalFunctionStatement(LocalFunctionStatementSyntax node)
        {
            // Don't recurse into local functions
        }

        public override void VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
        {
            // Don't recurse into lambda expressions
        }

        public override void VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
        {
            // Don't recurse into lambda expressions
        }

        public override void VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node)
        {
            // Don't recurse into anonymous methods
        }
    }
}
