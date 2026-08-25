using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fdw.Analyzers;

/// <summary>
/// Analyzer that warns when IGenericResult&lt;T&gt;.Value is accessed without first checking IsSuccess or IsFailure.
/// Fdw convention: always check IsSuccess before reading .Value to avoid silent null propagation.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UncheckedResultValueAccessAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for unchecked result value access.
    /// </summary>
    public const string DiagnosticId = "FDW016";

    private const string Title = "IGenericResult<T>.Value accessed without success check";
    private const string MessageFormat = "'{0}.Value' is accessed without checking IsSuccess first";
    private const string Description = "Fdw convention: IGenericResult<T>.Value must only be accessed after verifying IsSuccess is true. Accessing Value on a failed result returns default/null and silently drops the error.";
    private const string Category = "Usage";

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
        context.ConfigureGeneratedCodeAnalysis(
            GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
    }

    /// <summary>
    /// Checks every member access expression for unguarded .Value on IGenericResult&lt;T&gt;.
    /// </summary>
    private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
    {
        if (IsTestProject(context))
            return;

        var memberAccess = (MemberAccessExpressionSyntax)context.Node;

        // Fast path: only interested in .Value
        if (!string.Equals(memberAccess.Name.Identifier.Text, "Value", StringComparison.Ordinal))
            return;

        // Skip code inside Fdw.Results namespace (the type's own implementation)
        if (IsResultsNamespace(context))
            return;

        // Check that the expression type implements IGenericResult<T> (which has the Value property)
        var typeInfo = context.SemanticModel.GetTypeInfo(memberAccess.Expression, context.CancellationToken);
        if (!IsGenericResultWithValue(typeInfo.Type))
            return;

        // Extract the expression text for guard matching (e.g., "result" from "result.Value")
        var expressionText = GetExpressionKey(memberAccess.Expression);

        // Check if this .Value access is properly guarded
        if (IsValueAccessGuarded(memberAccess, expressionText))
            return;

        var diagnostic = Diagnostic.Create(
            Rule,
            memberAccess.GetLocation(),
            expressionText);

        context.ReportDiagnostic(diagnostic);
    }

    /// <summary>
    /// Determines whether the .Value access is inside a guard that verifies IsSuccess/IsFailure.
    /// </summary>
    private static bool IsValueAccessGuarded(
        MemberAccessExpressionSyntax valueAccess,
        string expressionText)
    {
        // Check 1: Is the .Value access inside the correct branch of an if-statement that checks success?
        if (IsInsideSuccessGuard(valueAccess, expressionText))
            return true;

        // Check 2: Is there an early-return-on-failure pattern before this access?
        if (HasPriorFailureEarlyReturn(valueAccess, expressionText))
            return true;

        // Check 3: Is .Value in the right operand of a short-circuit && where left checks success?
        // Handles: if (result.IsSuccess && result.Value != null)
        if (IsGuardedByShortCircuitAnd(valueAccess, expressionText))
            return true;

        // Check 4: Is .Value in the right operand of a short-circuit || where left checks failure?
        // Handles: if (!result.IsSuccess || result.Value == null) return;
        if (IsGuardedByShortCircuitOr(valueAccess, expressionText))
            return true;

        // Check 5: Is .Value in the true-branch of a ternary where condition checks success?
        // Handles: result.IsSuccess ? result.Value : fallback
        if (IsInsideTernaryGuard(valueAccess, expressionText))
            return true;

        return false;
    }

    /// <summary>
    /// Walks up the syntax tree to find an enclosing if-statement whose condition checks
    /// IsSuccess/IsFailure on the same variable, with .Value in the correct branch.
    /// </summary>
    private static bool IsInsideSuccessGuard(SyntaxNode valueAccess, string expressionText)
    {
        var current = valueAccess.Parent;
        while (current != null)
        {
            if (current is IfStatementSyntax ifStmt)
            {
                // Check if .Value is inside the then-branch and condition is a positive success check
                if (IsDescendantOf(valueAccess, ifStmt.Statement) &&
                    IsPositiveSuccessCondition(ifStmt.Condition, expressionText))
                {
                    return true;
                }

                // Check if .Value is inside the else-branch and condition is a failure check
                if (ifStmt.Else != null &&
                    IsDescendantOf(valueAccess, ifStmt.Else.Statement) &&
                    IsNegativeSuccessCondition(ifStmt.Condition, expressionText))
                {
                    return true;
                }
            }

            current = current.Parent;
        }

        return false;
    }

    /// <summary>
    /// Checks whether .Value is inside the right operand of a short-circuit &amp;&amp; expression
    /// where the left operand checks IsSuccess. C# evaluates left-to-right with short-circuit,
    /// so the right operand is only reached when IsSuccess is true.
    /// Handles: if (result.IsSuccess &amp;&amp; result.Value != null)
    /// </summary>
    private static bool IsGuardedByShortCircuitAnd(SyntaxNode valueAccess, string expressionText)
    {
        var current = valueAccess;
        while (current != null)
        {
            if (current.Parent is BinaryExpressionSyntax binary &&
                binary.IsKind(SyntaxKind.LogicalAndExpression) &&
                IsDescendantOf(valueAccess, binary.Right) &&
                IsPositiveSuccessCondition(binary.Left, expressionText))
            {
                return true;
            }

            current = current.Parent;
        }

        return false;
    }

    /// <summary>
    /// Checks whether .Value is in the right operand of a short-circuit || expression
    /// where the left operand is a failure check. C# short-circuit OR only evaluates the right
    /// when the left is false, meaning the result IS successful.
    /// Handles: if (!result.IsSuccess || result.Value == null) return;
    /// </summary>
    private static bool IsGuardedByShortCircuitOr(SyntaxNode valueAccess, string expressionText)
    {
        var current = valueAccess;
        while (current != null)
        {
            if (current.Parent is BinaryExpressionSyntax binary &&
                binary.IsKind(SyntaxKind.LogicalOrExpression) &&
                IsDescendantOf(valueAccess, binary.Right) &&
                IsNegativeSuccessCondition(binary.Left, expressionText))
            {
                return true;
            }

            current = current.Parent;
        }

        return false;
    }

    /// <summary>
    /// Checks whether .Value is in the true-branch of a ternary (conditional) expression
    /// where the condition checks IsSuccess.
    /// Handles: result.IsSuccess ? result.Value : fallback
    /// </summary>
    private static bool IsInsideTernaryGuard(SyntaxNode valueAccess, string expressionText)
    {
        var current = valueAccess;
        while (current != null)
        {
            if (current.Parent is ConditionalExpressionSyntax ternary)
            {
                // .Value in true-branch with positive success condition
                if (IsDescendantOf(valueAccess, ternary.WhenTrue) &&
                    IsPositiveSuccessCondition(ternary.Condition, expressionText))
                {
                    return true;
                }

                // .Value in false-branch with negative success condition
                if (IsDescendantOf(valueAccess, ternary.WhenFalse) &&
                    IsNegativeSuccessCondition(ternary.Condition, expressionText))
                {
                    return true;
                }
            }

            current = current.Parent;
        }

        return false;
    }

    /// <summary>
    /// Checks whether preceding statements in enclosing blocks contain an early-return pattern
    /// after checking for failure. Handles nested scopes by walking up through all ancestor blocks.
    /// </summary>
    private static bool HasPriorFailureEarlyReturn(SyntaxNode valueAccess, string expressionText)
    {
        var current = valueAccess;
        while (current != null)
        {
            if (current is StatementSyntax statement && current.Parent is BlockSyntax block)
            {
                foreach (var sibling in block.Statements)
                {
                    // Stop when we reach the statement containing .Value
                    if (sibling.SpanStart >= statement.SpanStart)
                        break;

                    if (sibling is IfStatementSyntax ifStmt &&
                        IsNegativeSuccessCondition(ifStmt.Condition, expressionText) &&
                        ContainsUnconditionalEarlyExit(ifStmt.Statement))
                    {
                        return true;
                    }
                }
            }

            current = current.Parent;
        }

        return false;
    }

    /// <summary>
    /// Checks whether a condition is a "positive success" test: the then-branch means the result is successful.
    /// Matches: expr.IsSuccess, !expr.IsFailure, expr is { IsSuccess: true }, expr is { IsFailure: false },
    /// and compound AND forms containing these.
    /// </summary>
    private static bool IsPositiveSuccessCondition(ExpressionSyntax condition, string expressionText)
    {
        // expr.IsSuccess
        if (IsMemberAccessCheck(condition, expressionText, "IsSuccess"))
            return true;

        // !expr.IsFailure
        if (condition is PrefixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.LogicalNotExpression } negated &&
            IsMemberAccessCheck(negated.Operand, expressionText, "IsFailure"))
        {
            return true;
        }

        // expr is { IsSuccess: true }
        if (IsPropertyPatternCheck(condition, expressionText, "IsSuccess", true))
            return true;

        // expr is { IsFailure: false }
        if (IsPropertyPatternCheck(condition, expressionText, "IsFailure", false))
            return true;

        // Compound AND: left && right (both sides are safe if either checks success)
        if (condition is BinaryExpressionSyntax binary &&
            binary.IsKind(SyntaxKind.LogicalAndExpression))
        {
            if (IsPositiveSuccessCondition(binary.Left, expressionText) ||
                IsPositiveSuccessCondition(binary.Right, expressionText))
            {
                return true;
            }
        }

        // Parenthesized
        if (condition is ParenthesizedExpressionSyntax parens)
            return IsPositiveSuccessCondition(parens.Expression, expressionText);

        return false;
    }

    /// <summary>
    /// Checks whether a condition is a "negative success" test: the then-branch means the result failed.
    /// Matches: expr.IsFailure, !expr.IsSuccess, expr is { IsSuccess: false }, expr is { IsFailure: true }.
    /// Used for early-return guards and else-branch detection.
    /// </summary>
    private static bool IsNegativeSuccessCondition(ExpressionSyntax condition, string expressionText)
    {
        // expr.IsFailure or expr.Error
        if (IsMemberAccessCheck(condition, expressionText, "IsFailure") ||
            IsMemberAccessCheck(condition, expressionText, "Error"))
        {
            return true;
        }

        // !expr.IsSuccess
        if (condition is PrefixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.LogicalNotExpression } negated &&
            IsMemberAccessCheck(negated.Operand, expressionText, "IsSuccess"))
        {
            return true;
        }

        // expr is { IsSuccess: false }
        if (IsPropertyPatternCheck(condition, expressionText, "IsSuccess", false))
            return true;

        // expr is { IsFailure: true }
        if (IsPropertyPatternCheck(condition, expressionText, "IsFailure", true))
            return true;

        // Compound OR: !expr.IsSuccess || ... or expr.IsFailure || ... or expr.Error || ...
        // After an early-return on this condition, ALL OR'd conditions are false —
        // so every variable checked anywhere in the compound OR is guaranteed successful.
        if (condition is BinaryExpressionSyntax binary &&
            binary.IsKind(SyntaxKind.LogicalOrExpression))
        {
            if (IsNegativeSuccessCondition(binary.Left, expressionText) ||
                IsNegativeSuccessCondition(binary.Right, expressionText))
            {
                return true;
            }
        }

        // Parenthesized
        if (condition is ParenthesizedExpressionSyntax parens)
            return IsNegativeSuccessCondition(parens.Expression, expressionText);

        return false;
    }

    /// <summary>
    /// Checks if an expression is a member access of the form "expressionText.memberName".
    /// </summary>
    private static bool IsMemberAccessCheck(
        ExpressionSyntax expression,
        string expressionText,
        string memberName)
    {
        if (expression is MemberAccessExpressionSyntax memberAccess &&
            string.Equals(memberAccess.Name.Identifier.Text, memberName, StringComparison.Ordinal) &&
            string.Equals(GetExpressionKey(memberAccess.Expression), expressionText, StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks for a property pattern match like: expr is { PropertyName: true/false }.
    /// </summary>
    private static bool IsPropertyPatternCheck(
        ExpressionSyntax condition,
        string expressionText,
        string propertyName,
        bool expectedValue)
    {
        if (condition is IsPatternExpressionSyntax isPattern &&
            string.Equals(GetExpressionKey(isPattern.Expression), expressionText, StringComparison.Ordinal) &&
            isPattern.Pattern is RecursivePatternSyntax recursivePattern &&
            recursivePattern.PropertyPatternClause is { } propClause)
        {
            foreach (var subPattern in propClause.Subpatterns)
            {
                if (subPattern.NameColon != null &&
                    string.Equals(subPattern.NameColon.Name.Identifier.Text, propertyName, StringComparison.Ordinal) &&
                    subPattern.Pattern is ConstantPatternSyntax constant)
                {
                    var literalKind = expectedValue
                        ? SyntaxKind.TrueLiteralExpression
                        : SyntaxKind.FalseLiteralExpression;

                    if (constant.Expression.IsKind(literalKind))
                        return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Checks whether a statement unconditionally exits the current scope (return, throw).
    /// For blocks, checks that the last statement is an early exit.
    /// </summary>
    private static bool ContainsUnconditionalEarlyExit(StatementSyntax statement)
    {
        if (statement is ReturnStatementSyntax or ThrowStatementSyntax
            or ContinueStatementSyntax or BreakStatementSyntax)
        {
            return true;
        }

        if (statement is BlockSyntax block && block.Statements.Count > 0)
        {
            var lastStatement = block.Statements[block.Statements.Count - 1];
            return lastStatement is ReturnStatementSyntax or ThrowStatementSyntax
                or ContinueStatementSyntax or BreakStatementSyntax;
        }

        return false;
    }

    /// <summary>
    /// Checks whether a node is the same as or a descendant of the specified potential ancestor.
    /// Includes equality: returns true when node and potentialAncestor are the same object.
    /// </summary>
    private static bool IsDescendantOf(SyntaxNode node, SyntaxNode? potentialAncestor)
    {
        if (potentialAncestor == null)
            return false;

        var current = node;
        while (current != null)
        {
            if (ReferenceEquals(current, potentialAncestor))
                return true;

            current = current.Parent;
        }

        return false;
    }

    /// <summary>
    /// Extracts a normalized key for an expression for use in guard matching.
    /// For simple identifiers, returns the identifier text. For other expressions,
    /// returns a whitespace-normalized string representation.
    /// </summary>
    private static string GetExpressionKey(ExpressionSyntax expression)
    {
        if (expression is IdentifierNameSyntax identifier)
            return identifier.Identifier.Text;

        return expression.ToString().Replace(" ", string.Empty);
    }

    /// <summary>
    /// Determines whether a type is IGenericResult&lt;T&gt; or GenericResult&lt;T&gt;
    /// (the generic form that has a Value property).
    /// </summary>
    private static bool IsGenericResultWithValue(ITypeSymbol? type)
    {
        if (type == null)
            return false;

        if (IsGenericResultByNameWithArity(type))
            return true;

        foreach (var iface in type.AllInterfaces)
        {
            if (IsGenericResultByNameWithArity(iface))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Checks whether a type symbol is IGenericResult or GenericResult with generic arity 1
    /// in the Fdw.Results namespace.
    /// </summary>
    private static bool IsGenericResultByNameWithArity(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol namedType || namedType.Arity != 1)
            return false;

        var namespaceName = type.ContainingNamespace?.ToDisplayString();
        if (!string.Equals(namespaceName, "Fdw.Results", StringComparison.Ordinal))
            return false;

        return string.Equals(type.Name, "IGenericResult", StringComparison.Ordinal) ||
               string.Equals(type.Name, "GenericResult", StringComparison.Ordinal);
    }

    /// <summary>
    /// Determines whether the current compilation is a test project by checking the assembly name.
    /// </summary>
    private static bool IsTestProject(SyntaxNodeAnalysisContext context)
    {
        var assemblyName = context.SemanticModel.Compilation.AssemblyName;
        if (assemblyName == null)
            return false;

        return assemblyName.EndsWith(".Tests", StringComparison.OrdinalIgnoreCase) ||
               assemblyName.EndsWith(".Test", StringComparison.OrdinalIgnoreCase) ||
               assemblyName.IndexOf(".Tests.", StringComparison.OrdinalIgnoreCase) >= 0 ||
               assemblyName.IndexOf(".Test.", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>
    /// Determines whether the analyzed code is inside the Fdw.Results namespace.
    /// Skips analysis to avoid flagging the IGenericResult&lt;T&gt; implementation itself.
    /// </summary>
    private static bool IsResultsNamespace(SyntaxNodeAnalysisContext context)
    {
        var containingNamespace = context.ContainingSymbol?.ContainingNamespace?.ToDisplayString();
        return containingNamespace != null &&
               containingNamespace.StartsWith("Fdw.Results", StringComparison.Ordinal);
    }
}
