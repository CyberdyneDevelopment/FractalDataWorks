using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fdw.Conventions.Analyzers;

/// <summary>
/// Analyzer that identifies enum declarations and enum-based dispatch patterns
/// that should be replaced with TypeCollections.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TypeCollectionOpportunityAnalyzer : DiagnosticAnalyzer
{
    /// <summary>Diagnostic ID for enum declaration opportunity.</summary>
    public const string DiagnosticId017 = "FDW017";

    /// <summary>Diagnostic ID for switch-on-enum opportunity.</summary>
    public const string DiagnosticId018 = "FDW018";

    /// <summary>Diagnostic ID for if/else chain opportunity.</summary>
    public const string DiagnosticId019 = "FDW019";

    private const string Category = "Design";

    private const string Title017 = "Enum declaration should be replaced with TypeCollection";
    private const string MessageFormat017 = "Enum '{0}' should be replaced with a TypeCollection. TypeCollections provide extensibility, ByName/ById lookup, and eliminate switch/if dispatch.";
    private const string Description017 = "TypeCollections are the preferred extensible type pattern in FDW. Enums lack extensibility and force switch/if dispatch patterns that TypeCollections eliminate.";

    private const string Title018 = "Switch on enum type suggests TypeCollection ByName lookup";
    private const string MessageFormat018 = "Switch on enum type '{0}' suggests a TypeCollection ByName() lookup. Use '{0}Collection.ByName(value)' for type-safe dispatch.";
    private const string Description018 = "Switch statements on enum types can be replaced with TypeCollection ByName() lookups for type-safe, extensible dispatch.";

    private const string Title019 = "If/else chain comparing enum values suggests TypeCollection ByName dispatch";
    private const string MessageFormat019 = "If/else chain with {0} branches comparing '{1}' suggests a TypeCollection ByName() dispatch";
    private const string Description019 = "If/else chains that compare a variable against multiple enum member values or string constants can be replaced with TypeCollection ByName() dispatch.";

    private static readonly DiagnosticDescriptor Rule017 = new(
        DiagnosticId017,
        Title017,
        MessageFormat017,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description017);

    private static readonly DiagnosticDescriptor Rule018 = new(
        DiagnosticId018,
        Title018,
        MessageFormat018,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description018);

    private static readonly DiagnosticDescriptor Rule019 = new(
        DiagnosticId019,
        Title019,
        MessageFormat019,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description019);

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule017, Rule018, Rule019];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeEnumDeclaration, SyntaxKind.EnumDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeSwitchStatement, SyntaxKind.SwitchStatement);
        context.RegisterSyntaxNodeAction(AnalyzeSwitchExpression, SyntaxKind.SwitchExpression);
        context.RegisterSyntaxNodeAction(AnalyzeIfStatement, SyntaxKind.IfStatement);
    }

    private static void AnalyzeEnumDeclaration(SyntaxNodeAnalysisContext context)
    {
        var enumDecl = (EnumDeclarationSyntax)context.Node;

        // Skip [Flags] enums — bitfield semantics don't map to TypeCollection
        foreach (var attributeList in enumDecl.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var name = attribute.Name.ToString();
                if (string.Equals(name, "Flags", StringComparison.Ordinal)
                    || string.Equals(name, "FlagsAttribute", StringComparison.Ordinal)
                    || string.Equals(name, "System.Flags", StringComparison.Ordinal)
                    || string.Equals(name, "System.FlagsAttribute", StringComparison.Ordinal))
                {
                    return;
                }
            }
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Rule017,
            enumDecl.Identifier.GetLocation(),
            enumDecl.Identifier.Text));
    }

    private static void AnalyzeSwitchStatement(SyntaxNodeAnalysisContext context)
    {
        var switchStmt = (SwitchStatementSyntax)context.Node;

        var typeInfo = context.SemanticModel.GetTypeInfo(switchStmt.Expression, context.CancellationToken);
        if (typeInfo.Type?.TypeKind != TypeKind.Enum)
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            Rule018,
            switchStmt.SwitchKeyword.GetLocation(),
            typeInfo.Type.Name));
    }

    private static void AnalyzeSwitchExpression(SyntaxNodeAnalysisContext context)
    {
        var switchExpr = (SwitchExpressionSyntax)context.Node;

        var typeInfo = context.SemanticModel.GetTypeInfo(switchExpr.GoverningExpression, context.CancellationToken);
        if (typeInfo.Type?.TypeKind != TypeKind.Enum)
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            Rule018,
            switchExpr.SwitchKeyword.GetLocation(),
            typeInfo.Type.Name));
    }

    private static void AnalyzeIfStatement(SyntaxNodeAnalysisContext context)
    {
        var ifStatement = (IfStatementSyntax)context.Node;

        // Skip else-if branches to avoid double-reporting
        if (ifStatement.Parent is ElseClauseSyntax)
            return;

        var branchCount = 0;
        string? comparedVariable = null;
        StatementSyntax? current = ifStatement;

        while (current is IfStatementSyntax currentIf)
        {
            var (variable, isEnumOrString) = ExtractComparisonVariable(currentIf.Condition, context);

            if (variable == null || !isEnumOrString)
                return;

            if (comparedVariable == null)
            {
                comparedVariable = variable;
            }
            else if (!string.Equals(comparedVariable, variable, StringComparison.Ordinal))
            {
                return;
            }

            branchCount++;
            current = currentIf.Else?.Statement;
        }

        if (branchCount < 3 || comparedVariable == null)
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            Rule019,
            ifStatement.IfKeyword.GetLocation(),
            branchCount,
            comparedVariable));
    }

    private static (string? variable, bool isEnumOrString) ExtractComparisonVariable(
        ExpressionSyntax condition, SyntaxNodeAnalysisContext context)
    {
        if (condition is BinaryExpressionSyntax binaryExpr
            && binaryExpr.IsKind(SyntaxKind.EqualsExpression))
        {
            return TryExtractFromBinary(binaryExpr, context);
        }

        if (condition is ParenthesizedExpressionSyntax paren)
            return ExtractComparisonVariable(paren.Expression, context);

        return (null, false);
    }

    private static (string? variable, bool isEnumOrString) TryExtractFromBinary(
        BinaryExpressionSyntax binaryExpr, SyntaxNodeAnalysisContext context)
    {
        // variable == EnumType.Member  or  variable == "string"
        if (IsEnumMemberOrStringLiteral(binaryExpr.Right, context))
        {
            var varName = GetSimpleVariableName(binaryExpr.Left);
            if (varName != null)
                return (varName, true);
        }

        // EnumType.Member == variable  or  "string" == variable
        if (IsEnumMemberOrStringLiteral(binaryExpr.Left, context))
        {
            var varName = GetSimpleVariableName(binaryExpr.Right);
            if (varName != null)
                return (varName, true);
        }

        return (null, false);
    }

    private static bool IsEnumMemberOrStringLiteral(ExpressionSyntax expr, SyntaxNodeAnalysisContext context)
    {
        if (expr is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression))
            return true;

        var typeInfo = context.SemanticModel.GetTypeInfo(expr, context.CancellationToken);
        return typeInfo.Type?.TypeKind == TypeKind.Enum;
    }

    private static string? GetSimpleVariableName(ExpressionSyntax expr)
    {
        if (expr is IdentifierNameSyntax identifier)
            return identifier.Identifier.Text;

        if (expr is MemberAccessExpressionSyntax memberAccess)
            return memberAccess.ToString();

        return null;
    }
}
