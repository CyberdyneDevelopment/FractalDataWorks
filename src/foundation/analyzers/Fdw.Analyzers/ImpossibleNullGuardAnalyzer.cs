using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fdw.Analyzers;

/// <summary>
/// FDW063: an ArgumentNullException guard on a parameter that cannot be null. FDW does not throw ArgumentNullException, and
/// nullable reference types are enabled everywhere, so a parameter declared with a non-nullable reference type already states
/// that null is not a value it takes; the guard repeats it at run time (Mike, 2026-11-11; FDW-5184, FDW-5198).
/// </summary>
/// <remarks>
/// <para>
/// Reported, when the guarded value is such a parameter: <c>if (p == null) throw new ArgumentNullException(...);</c> (also
/// <c>null == p</c> and <c>p is null</c>, a single throw with or without braces and no else), <c>p ?? throw new
/// ArgumentNullException(...)</c>, and the statement <c>ArgumentNullException.ThrowIfNull(p);</c>. A guarding statement is
/// reported only where it sits directly in a block, so the code fix can remove it.
/// </para>
/// <para>
/// Not reported: a parameter that may be null (<c>T?</c>, <c>Nullable&lt;T&gt;</c>), an unconstrained or <c>notnull</c> type
/// parameter, a parameter declared where nullable is disabled (oblivious), a field, property, local or other expression, a
/// compound condition (<c>p == null || q == null</c>), and <c>ArgumentException.ThrowIfNullOrEmpty</c>/<c>ThrowIfNullOrWhiteSpace</c>,
/// which also refuse an empty string.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ImpossibleNullGuardAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for an ArgumentNullException guard on a parameter that cannot be null.
    /// </summary>
    public const string DiagnosticId = "FDW063";

    private const string ArgumentNullExceptionName = "System.ArgumentNullException";
    private const string ThrowIfNullName = "ThrowIfNull";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "An ArgumentNullException guard on a parameter that cannot be null",
        "'{0}' is a non-nullable parameter, so it cannot be null here: remove the ArgumentNullException guard",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "FDW does not throw ArgumentNullException. With nullable reference types enabled, a parameter of a non-nullable reference type already states it takes no null, so a guard that throws ArgumentNullException for it is removed.");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeIf, SyntaxKind.IfStatement);
        context.RegisterSyntaxNodeAction(AnalyzeCoalesce, SyntaxKind.CoalesceExpression);
        context.RegisterSyntaxNodeAction(AnalyzeThrowIfNull, SyntaxKind.ExpressionStatement);
    }

    private static void AnalyzeIf(SyntaxNodeAnalysisContext context)
    {
        var statement = (IfStatementSyntax)context.Node;
        if (statement.Else != null || statement.Parent is not BlockSyntax)
            return;

        var thrown = statement.Statement switch
        {
            ThrowStatementSyntax throwStatement => throwStatement.Expression,
            BlockSyntax { Statements.Count: 1 } block when block.Statements[0] is ThrowStatementSyntax only => only.Expression,
            _ => null,
        };

        if (thrown == null || !CreatesArgumentNullException(thrown, context.SemanticModel)
            || NullCheckedOperand(statement.Condition) is not { } operand)
            return;

        Report(context, statement, operand);
    }

    private static void AnalyzeCoalesce(SyntaxNodeAnalysisContext context)
    {
        var coalesce = (BinaryExpressionSyntax)context.Node;
        if (coalesce.Right is not ThrowExpressionSyntax throwExpression
            || !CreatesArgumentNullException(throwExpression.Expression, context.SemanticModel))
            return;

        Report(context, coalesce, coalesce.Left);
    }

    private static void AnalyzeThrowIfNull(SyntaxNodeAnalysisContext context)
    {
        var statement = (ExpressionStatementSyntax)context.Node;
        if (statement.Parent is not BlockSyntax
            || statement.Expression is not InvocationExpressionSyntax { ArgumentList.Arguments.Count: > 0 } invocation
            || context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol method
            || !string.Equals(method.Name, ThrowIfNullName, StringComparison.Ordinal)
            || !string.Equals(method.ContainingType?.ToDisplayString(), ArgumentNullExceptionName, StringComparison.Ordinal))
            return;

        Report(context, statement, invocation.ArgumentList.Arguments[0].Expression);
    }

    private static void Report(SyntaxNodeAnalysisContext context, SyntaxNode guard, ExpressionSyntax operand)
    {
        if (context.SemanticModel.GetSymbolInfo(Unparenthesized(operand), context.CancellationToken).Symbol is IParameterSymbol parameter
            && CannotBeNull(parameter))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, guard.GetLocation(), parameter.Name));
        }
    }

    // A non-nullable reference type declared in an enabled nullable context. Oblivious code is None, T? is Annotated, and a value
    // type (Nullable<T> included) or a type parameter that may be one is not a reference type.
    private static bool CannotBeNull(IParameterSymbol parameter) =>
        parameter.NullableAnnotation == NullableAnnotation.NotAnnotated && parameter.Type.IsReferenceType;

    private static bool CreatesArgumentNullException(ExpressionSyntax? thrown, SemanticModel model) =>
        thrown is BaseObjectCreationExpressionSyntax creation
        && string.Equals(model.GetTypeInfo(creation).Type?.ToDisplayString(), ArgumentNullExceptionName, StringComparison.Ordinal);

    // The value a condition compares with null: p == null, null == p, or p is null. Anything else (a compound condition) is not a guard
    // the fix can remove.
    private static ExpressionSyntax? NullCheckedOperand(ExpressionSyntax condition) => Unparenthesized(condition) switch
    {
        BinaryExpressionSyntax { RawKind: (int)SyntaxKind.EqualsExpression } equals when IsNull(equals.Right) => equals.Left,
        BinaryExpressionSyntax { RawKind: (int)SyntaxKind.EqualsExpression } equals when IsNull(equals.Left) => equals.Right,
        IsPatternExpressionSyntax { Pattern: ConstantPatternSyntax constant } isPattern when IsNull(constant.Expression) => isPattern.Expression,
        _ => null,
    };

    private static bool IsNull(ExpressionSyntax expression) => Unparenthesized(expression).IsKind(SyntaxKind.NullLiteralExpression);

    private static ExpressionSyntax Unparenthesized(ExpressionSyntax expression)
    {
        while (expression is ParenthesizedExpressionSyntax parenthesized)
            expression = parenthesized.Expression;

        return expression;
    }
}
