using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fdw.Analyzers;

/// <summary>
/// FDW057: <c>result.FailureReason()</c> is read only where the result is known to have failed: inside
/// <c>if (!result.IsSuccess)</c> or <c>if (result.IsFailure)</c>, in the else of <c>if (result.IsSuccess)</c>, after
/// <c>if (result.IsSuccess) return;</c>, on the right of <c>result.IsFailure &amp;&amp;</c> or <c>result.IsSuccess ||</c>,
/// or in the matching arm of a ternary. FailureReason throws on a success, as Value throws on a failure.
/// </summary>
/// <remarks>
/// Why a rule of its own beside FDW016: the guard is the mirror of Value's, and its message names FailureReason. The
/// guard logic is shared with FDW016 in <see cref="ResultGuards"/> (FDW-3580).
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UncheckedFailureReasonAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for a failure reason read without a failure check.
    /// </summary>
    public const string DiagnosticId = "FDW057";

    private const string ExtensionsName = "Fdw.Results.FailureMessageExtensions";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "FailureReason() read without a failure check",
        "'{0}.FailureReason()' is read where '{0}' may have succeeded: read it inside a check that it failed",
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "FailureReason() throws on a successful result. Read it only where the result is known to have failed: in the branch of !result.IsSuccess or result.IsFailure, or after an early exit on result.IsSuccess.");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        var receiver = Receiver(invocation);
        if (receiver is null || IsResultsNamespace(context))
            return;

        if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol method ||
            !string.Equals(method.Name, "FailureReason", StringComparison.Ordinal) ||
            !string.Equals(method.ContainingType.ToDisplayString(), ExtensionsName, StringComparison.Ordinal))
            return;

        var key = ResultGuards.Key(receiver);
        if (ResultGuards.IsGuarded(invocation, key, success: false))
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(), key));
    }

    // Why Fdw.Results is skipped: the results' own members read FailureReason on this, guarded by a bare IsSuccess the
    // receiver-text match cannot pair; FDW016 skips the namespace for the same reason.
    private static bool IsResultsNamespace(SyntaxNodeAnalysisContext context) =>
        context.ContainingSymbol?.ContainingNamespace?.ToDisplayString() is { } name &&
        name.StartsWith("Fdw.Results", StringComparison.Ordinal);

    /// <summary>The result read: <c>r</c> in <c>r.FailureReason()</c> or in <c>FailureMessageExtensions.FailureReason(r)</c>.</summary>
    private static ExpressionSyntax? Receiver(InvocationExpressionSyntax invocation) => invocation.Expression switch
    {
        MemberAccessExpressionSyntax { Name.Identifier.ValueText: "FailureReason" } access when invocation.ArgumentList.Arguments.Count == 0 =>
            access.Expression,
        MemberAccessExpressionSyntax { Name.Identifier.ValueText: "FailureReason" } when invocation.ArgumentList.Arguments.Count == 1 =>
            invocation.ArgumentList.Arguments[0].Expression,
        _ => null,
    };
}
