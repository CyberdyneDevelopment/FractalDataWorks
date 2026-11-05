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
        var expressionText = ResultGuards.Key(memberAccess.Expression);

        // Check if this .Value access is properly guarded
        if (ResultGuards.IsGuarded(memberAccess, expressionText, success: true))
            return;

        var diagnostic = Diagnostic.Create(
            Rule,
            memberAccess.GetLocation(),
            expressionText);

        context.ReportDiagnostic(diagnostic);
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
