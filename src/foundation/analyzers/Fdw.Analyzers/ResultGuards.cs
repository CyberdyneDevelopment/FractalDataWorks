using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fdw.Analyzers;

/// <summary>
/// Whether a read of a result is guarded by a check that the result succeeded or failed: FDW016 asks it for
/// <c>result.Value</c>, which needs a success, and FDW057 for <c>result.FailureReason()</c>, which needs a failure.
/// </summary>
/// <remarks>
/// <para>
/// Why "implies when true" and "implies when false": a branch is guarded by what its condition proves. The then-branch
/// of <c>if (!r.IsSuccess || r.Value is null)</c> does not prove a failure (r may have succeeded with no value), while
/// the code after <c>if (!r.IsSuccess || r.Value is null) return;</c> does prove a success. An OR proves a state
/// when true only if both sides prove it, and when false if either side does; AND is the reverse; <c>!</c> swaps them.
/// </para>
/// <para>
/// The checks are syntactic and match the receiver by its text, as FDW016 always has (FDW-3580).
/// </para>
/// </remarks>
internal static class ResultGuards
{
    /// <summary>
    /// Whether <paramref name="access"/>, a read of the result named <paramref name="receiver"/>, only runs when the
    /// result's IsSuccess is <paramref name="success"/>.
    /// </summary>
    public static bool IsGuarded(SyntaxNode access, string receiver, bool success) =>
        IsInsideIf(access, receiver, success) ||
        HasPriorEarlyExit(access, receiver, success) ||
        IsInsideShortCircuit(access, receiver, success) ||
        IsInsideTernary(access, receiver, success);

    /// <summary>A normalized key for an expression, for matching a read to its check.</summary>
    public static string Key(ExpressionSyntax expression) =>
        expression is IdentifierNameSyntax identifier
            ? identifier.Identifier.Text
            : expression.ToString().Replace(" ", string.Empty);

    /// <summary>Whether <paramref name="condition"/> being true proves the result's IsSuccess is <paramref name="success"/>.</summary>
    public static bool ProvesWhenTrue(ExpressionSyntax condition, string receiver, bool success) => condition switch
    {
        ParenthesizedExpressionSyntax parens => ProvesWhenTrue(parens.Expression, receiver, success),
        PrefixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.LogicalNotExpression } not => ProvesWhenFalse(not.Operand, receiver, success),
        BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.LogicalAndExpression) =>
            ProvesWhenTrue(binary.Left, receiver, success) || ProvesWhenTrue(binary.Right, receiver, success),
        BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.LogicalOrExpression) =>
            ProvesWhenTrue(binary.Left, receiver, success) && ProvesWhenTrue(binary.Right, receiver, success),
        _ => Atom(condition, receiver) == success,
    };

    /// <summary>Whether <paramref name="condition"/> being false proves the result's IsSuccess is <paramref name="success"/>.</summary>
    public static bool ProvesWhenFalse(ExpressionSyntax condition, string receiver, bool success) => condition switch
    {
        ParenthesizedExpressionSyntax parens => ProvesWhenFalse(parens.Expression, receiver, success),
        PrefixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.LogicalNotExpression } not => ProvesWhenTrue(not.Operand, receiver, success),
        BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.LogicalOrExpression) =>
            ProvesWhenFalse(binary.Left, receiver, success) || ProvesWhenFalse(binary.Right, receiver, success),
        BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.LogicalAndExpression) =>
            ProvesWhenFalse(binary.Left, receiver, success) && ProvesWhenFalse(binary.Right, receiver, success),
        _ => Atom(condition, receiver) == !success,
    };

    /// <summary>
    /// What a single check says when true: <see langword="true"/> for a success check (IsSuccess,
    /// is { IsSuccess: true }, is { IsFailure: false }), <see langword="false"/> for a failure check (IsFailure, Error,
    /// is { IsSuccess: false }, is { IsFailure: true }), <see langword="null"/> when it is not a check of the receiver.
    /// </summary>
    private static bool? Atom(ExpressionSyntax condition, string receiver)
    {
        if (condition is MemberAccessExpressionSyntax member &&
            string.Equals(Key(member.Expression), receiver, StringComparison.Ordinal))
        {
            return member.Name.Identifier.Text switch
            {
                "IsSuccess" => true,
                "IsFailure" or "Error" => false,
                _ => null,
            };
        }

        return condition is IsPatternExpressionSyntax pattern &&
               string.Equals(Key(pattern.Expression), receiver, StringComparison.Ordinal)
            ? PatternAtom(pattern.Pattern)
            : null;
    }

    private static bool? PatternAtom(PatternSyntax pattern)
    {
        if (pattern is not RecursivePatternSyntax { PropertyPatternClause: { } clause })
            return null;

        foreach (var sub in clause.Subpatterns)
        {
            if (sub.NameColon is null || sub.Pattern is not ConstantPatternSyntax constant)
                continue;

            var isTrue = constant.Expression.IsKind(SyntaxKind.TrueLiteralExpression);
            var isFalse = constant.Expression.IsKind(SyntaxKind.FalseLiteralExpression);
            if (!isTrue && !isFalse)
                continue;

            switch (sub.NameColon.Name.Identifier.Text)
            {
                case "IsSuccess":
                    return isTrue;
                case "IsFailure":
                    return isFalse;
            }
        }

        return null;
    }

    private static bool IsInsideIf(SyntaxNode access, string receiver, bool success)
    {
        for (var current = access.Parent; current != null; current = current.Parent)
        {
            if (current is not IfStatementSyntax ifStatement)
                continue;

            if (IsDescendantOf(access, ifStatement.Statement) && ProvesWhenTrue(ifStatement.Condition, receiver, success))
                return true;

            if (ifStatement.Else != null && IsDescendantOf(access, ifStatement.Else.Statement) &&
                ProvesWhenFalse(ifStatement.Condition, receiver, success))
                return true;
        }

        return false;
    }

    /// <summary>An earlier <c>if (...) return;</c> in an enclosing block whose condition, once false, proves the state.</summary>
    private static bool HasPriorEarlyExit(SyntaxNode access, string receiver, bool success)
    {
        for (var current = access; current != null; current = current.Parent)
        {
            if (current is not StatementSyntax statement || current.Parent is not BlockSyntax block)
                continue;

            foreach (var sibling in block.Statements)
            {
                if (sibling.SpanStart >= statement.SpanStart)
                    break;

                if (sibling is IfStatementSyntax ifStatement &&
                    ProvesWhenFalse(ifStatement.Condition, receiver, success) &&
                    EndsInExit(ifStatement.Statement))
                    return true;
            }
        }

        return false;
    }

    /// <summary>The right operand of <c>a &amp;&amp; b</c> runs when a is true; of <c>a || b</c>, when a is false.</summary>
    private static bool IsInsideShortCircuit(SyntaxNode access, string receiver, bool success)
    {
        for (var current = access; current != null; current = current.Parent)
        {
            if (current.Parent is not BinaryExpressionSyntax binary || !IsDescendantOf(access, binary.Right))
                continue;

            if (binary.IsKind(SyntaxKind.LogicalAndExpression) && ProvesWhenTrue(binary.Left, receiver, success))
                return true;

            if (binary.IsKind(SyntaxKind.LogicalOrExpression) && ProvesWhenFalse(binary.Left, receiver, success))
                return true;
        }

        return false;
    }

    private static bool IsInsideTernary(SyntaxNode access, string receiver, bool success)
    {
        for (var current = access; current != null; current = current.Parent)
        {
            if (current.Parent is not ConditionalExpressionSyntax ternary)
                continue;

            if (IsDescendantOf(access, ternary.WhenTrue) && ProvesWhenTrue(ternary.Condition, receiver, success))
                return true;

            if (IsDescendantOf(access, ternary.WhenFalse) && ProvesWhenFalse(ternary.Condition, receiver, success))
                return true;
        }

        return false;
    }

    // Why yield break counts: in an iterator it ends the sequence as return ends a method. yield return does not exit.
    private static bool EndsInExit(StatementSyntax statement)
    {
        var last = statement is BlockSyntax { Statements.Count: > 0 } block
            ? block.Statements[block.Statements.Count - 1]
            : statement;

        return last is ReturnStatementSyntax or ThrowStatementSyntax or ContinueStatementSyntax or BreakStatementSyntax ||
               last.IsKind(SyntaxKind.YieldBreakStatement);
    }

    private static bool IsDescendantOf(SyntaxNode node, SyntaxNode? ancestor)
    {
        if (ancestor == null)
            return false;

        for (var current = node; current != null; current = current.Parent)
        {
            if (ReferenceEquals(current, ancestor))
                return true;
        }

        return false;
    }
}
