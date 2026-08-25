using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fdw.Analyzers;

/// <summary>
/// Analyzer that detects catch clauses where the caught exception is lost.
/// Emits two related diagnostics:
/// <list type="bullet">
/// <item>
/// <description>
/// FDW022 — the caught exception is neither observed (the exception variable is never referenced)
/// nor rethrown, so the failure is silently swallowed. This is the complement to FDW014: FDW014
/// only inspects the returned <c>IGenericResult</c> of result-returning methods, whereas FDW022
/// fires on any method (including <c>void</c>/<c>Task</c>) when the exception object itself is lost.
/// </description>
/// </item>
/// <item>
/// <description>
/// FDW023 — a broad <c>catch (Exception)</c> / bare <c>catch</c> with no more-specific catch clause
/// and no exception filter. Surfaces (as a suggestion) every place that catches the base
/// <see cref="System.Exception"/> without chaining the specific exceptions it could actually handle.
/// </description>
/// </item>
/// </list>
/// Fdw convention: catch, log AND return. The caught exception must be passed into a
/// MessageLogging method / result, or rethrown — never discarded.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SwallowedExceptionAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for a swallowed (neither observed nor rethrown) exception.
    /// </summary>
    public const string SwallowedDiagnosticId = "FDW022";

    /// <summary>
    /// Diagnostic ID for a broad System.Exception catch with no specific catch clauses.
    /// </summary>
    public const string BroadCatchDiagnosticId = "FDW023";

    private const string Category = "Usage";

    private static readonly DiagnosticDescriptor SwallowedRule = new(
        SwallowedDiagnosticId,
        title: "Exception swallowed",
        messageFormat: "Caught exception is neither observed nor rethrown — log the exception, return it in a result, or rethrow",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Fdw convention: catch, log AND return. A catch block that does not reference the caught exception and does not rethrow silently discards the failure. Pass the exception into a MessageLogging method / GenericResult.Failure, or rethrow.");

    private static readonly DiagnosticDescriptor BroadCatchRule = new(
        BroadCatchDiagnosticId,
        title: "Broad exception catch without specific handlers",
        messageFormat: "'catch' handles the base System.Exception with no more-specific catch clause — chain catch clauses for the specific exceptions you can deal with, or add a 'when' filter",
        Category,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Catching the base System.Exception with no preceding specific catch clause (and no 'when' filter) means every failure is handled the same way. Where distinct exception types warrant distinct handling, chain specific catch clauses before the broad one.");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [SwallowedRule, BroadCatchRule];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeCatchClause, SyntaxKind.CatchClause);
    }

    private static void AnalyzeCatchClause(SyntaxNodeAnalysisContext context)
    {
        // Why: analyzer surfaces production violations; test projects deliberately throw/swallow.
        if (IsTestProject(context))
            return;

        var catchClause = (CatchClauseSyntax)context.Node;

        // Why: a swallowed catch is the primary signal — FDW022 takes precedence and FDW023 (the
        // broad-catch survey) is suppressed for the same clause so a bare 'catch {}' reports once.
        // Re-throwing (throw; / throw new X(..., ex) / throw expression) preserves the failure.
        if (!HasThrowStatement(catchClause) && !IsExceptionObserved(catchClause))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                SwallowedRule,
                catchClause.CatchKeyword.GetLocation()));
            return;
        }

        if (IsUnchainedBroadCatch(catchClause, context))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                BroadCatchRule,
                catchClause.CatchKeyword.GetLocation()));
        }
    }

    // Why: the exception is "observed" when its variable is referenced anywhere in the catch
    // block or the 'when' filter — passed to a log/result, inspected, or stored. A catch with no
    // declared variable (bare 'catch' or 'catch (Exception)') can never observe the object.
    private static bool IsExceptionObserved(CatchClauseSyntax catchClause)
    {
        var declaration = catchClause.Declaration;
        if (declaration == null || declaration.Identifier.IsKind(SyntaxKind.None))
            return false;

        var name = declaration.Identifier.ValueText;
        if (string.IsNullOrEmpty(name))
            return false;

        if (ReferencesIdentifier(catchClause.Block, name))
            return true;

        return catchClause.Filter != null && ReferencesIdentifier(catchClause.Filter, name);
    }

    private static bool ReferencesIdentifier(SyntaxNode scope, string name)
    {
        foreach (var identifier in scope.DescendantNodes().OfType<IdentifierNameSyntax>())
        {
            if (string.Equals(identifier.Identifier.ValueText, name, System.StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static bool HasThrowStatement(CatchClauseSyntax catchClause)
    {
        foreach (var node in catchClause.Block.DescendantNodes())
        {
            if (node is ThrowStatementSyntax || node is ThrowExpressionSyntax)
                return true;
        }

        return false;
    }

    // Why: flags a broad 'catch (Exception)' / bare 'catch' only when nothing on the same try
    // distinguishes exception types — no more-specific catch clause and no 'when' filter. A broad
    // catch-all that FOLLOWS specific clauses, or a filtered catch, is the discriminating pattern.
    private static bool IsUnchainedBroadCatch(CatchClauseSyntax catchClause, SyntaxNodeAnalysisContext context)
    {
        // A 'when' filter is an explicit discrimination mechanism — not an unchained blanket catch.
        if (catchClause.Filter != null)
            return false;

        if (!IsBroadExceptionType(catchClause.Declaration, context))
            return false;

        if (catchClause.Parent is not TryStatementSyntax tryStatement)
            return false;

        foreach (var sibling in tryStatement.Catches)
        {
            if (sibling == catchClause)
                continue;

            // Any sibling that names a more-specific exception type means the failures ARE
            // being distinguished — the broad clause is the legitimate trailing catch-all.
            if (IsSpecificExceptionType(sibling.Declaration, context))
                return false;
        }

        return true;
    }

    private static bool IsBroadExceptionType(CatchDeclarationSyntax? declaration, SyntaxNodeAnalysisContext context)
    {
        // Bare 'catch' with no declaration catches everything — broadest possible.
        if (declaration == null)
            return true;

        var type = context.SemanticModel.GetTypeInfo(declaration.Type).Type;

        // Why: unresolved type — stay conservative and treat as NOT broad to avoid false positives.
        if (type == null)
            return false;

        return IsSystemException(type);
    }

    private static bool IsSpecificExceptionType(CatchDeclarationSyntax? declaration, SyntaxNodeAnalysisContext context)
    {
        if (declaration == null)
            return false;

        var type = context.SemanticModel.GetTypeInfo(declaration.Type).Type;
        if (type == null)
            return false;

        // Specific = an exception type other than System.Exception itself.
        return !IsSystemException(type);
    }

    private static bool IsSystemException(ITypeSymbol type)
    {
        return string.Equals(type.Name, "Exception", System.StringComparison.Ordinal) &&
               string.Equals(type.ContainingNamespace?.ToDisplayString(), "System", System.StringComparison.Ordinal);
    }

    private static bool IsTestProject(SyntaxNodeAnalysisContext context)
    {
        var assemblyName = context.SemanticModel.Compilation.AssemblyName;
        if (assemblyName == null)
            return false;

        return assemblyName.EndsWith(".Tests", System.StringComparison.OrdinalIgnoreCase) ||
               assemblyName.EndsWith(".Test", System.StringComparison.OrdinalIgnoreCase) ||
               assemblyName.IndexOf(".Tests.", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               assemblyName.IndexOf(".Test.", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
