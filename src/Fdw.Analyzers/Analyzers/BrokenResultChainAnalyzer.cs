using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fdw.Analyzers;

/// <summary>
/// Analyzer that detects when a new GenericResult is created by extracting properties
/// from an existing result instead of using Chain() to preserve the full error chain.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BrokenResultChainAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for broken result chain violation.
    /// </summary>
    public const string DiagnosticId = "FDW015";

    private const string Title = "Result chain broken — use ToNewResult() or Chain() to preserve context";
    private const string MessageFormat = "'{0}' copies properties from another result instead of using ToNewResult() or Chain() to preserve the full error chain";
    private const string Description = "Fdw convention: When propagating a failure from an inner result, use result.ToNewResult<T>() for cross-type conversion or GenericResult.Chain() when adding a result code.";
    private const string Category = "Usage";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description);

    private static readonly ImmutableHashSet<string> ResultPropertyNames = ImmutableHashSet.Create(
        System.StringComparer.Ordinal,
        "Messages",
        "Code",
        "Details",
        "CurrentMessage");

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
        // Skip test projects
        if (IsTestProject(context))
            return;

        var invocation = (InvocationExpressionSyntax)context.Node;

        // Must be a member access expression (e.g., GenericResult.Failure(...))
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return;

        var methodName = memberAccess.Name.Identifier.Text;

        // Only check Failure methods
        if (!string.Equals(methodName, "Failure", System.StringComparison.Ordinal))
            return;

        // Verify the containing type is GenericResult or GenericResult<T> in the correct namespace
        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
            return;

        var containingTypeName = methodSymbol.ContainingType?.Name;
        if (!string.Equals(containingTypeName, "GenericResult", System.StringComparison.Ordinal))
            return;

        var containingNamespace = methodSymbol.ContainingType?.ContainingNamespace?.ToDisplayString();
        if (!string.Equals(containingNamespace, "Fdw.Results", System.StringComparison.Ordinal))
            return;

        // Inspect each argument for member access on an IGenericResult-typed variable
        var arguments = invocation.ArgumentList.Arguments;
        if (arguments.Count == 0)
            return;

        foreach (var argument in arguments)
        {
            var expression = argument.Expression;

            if (IsResultPropertyExtraction(expression, context))
            {
                var diagnostic = Diagnostic.Create(
                    Rule,
                    invocation.GetLocation(),
                    "GenericResult.Failure");

                context.ReportDiagnostic(diagnostic);
                return;
            }
        }
    }

    /// <summary>
    /// Determines whether the given expression extracts a property from an IGenericResult-typed variable.
    /// Handles direct member access (result.Messages) and chained invocations (result.Messages.ToArray()).
    /// </summary>
    private static bool IsResultPropertyExtraction(ExpressionSyntax expression, SyntaxNodeAnalysisContext context)
    {
        // Case 1: Direct member access — e.g., result.Messages, result.Code
        if (expression is MemberAccessExpressionSyntax directAccess)
        {
            return IsMemberAccessOnGenericResult(directAccess, context);
        }

        // Case 2: Method call on a member — e.g., result.Messages.ToArray(), result.Messages.ToList()
        if (expression is InvocationExpressionSyntax chainedInvocation &&
            chainedInvocation.Expression is MemberAccessExpressionSyntax outerAccess)
        {
            // The receiver of the method call (e.g., result.Messages for .ToArray())
            var receiver = outerAccess.Expression;

            if (receiver is MemberAccessExpressionSyntax innerAccess)
            {
                return IsMemberAccessOnGenericResult(innerAccess, context);
            }
        }

        return false;
    }

    /// <summary>
    /// Checks whether a member access expression accesses one of the tracked properties
    /// on a variable whose type implements IGenericResult.
    /// </summary>
    private static bool IsMemberAccessOnGenericResult(MemberAccessExpressionSyntax memberAccess, SyntaxNodeAnalysisContext context)
    {
        var memberName = memberAccess.Name.Identifier.Text;

        // Only flag the known result properties
        if (!ResultPropertyNames.Contains(memberName))
            return false;

        // Get the type of the object being accessed (e.g., the type of 'result' in 'result.Messages')
        var receiverTypeInfo = context.SemanticModel.GetTypeInfo(memberAccess.Expression);
        var receiverType = receiverTypeInfo.Type;

        if (receiverType == null)
            return false;

        return IsGenericResultType(receiverType);
    }

    /// <summary>
    /// Checks whether a type is IGenericResult, GenericResult, or implements IGenericResult.
    /// </summary>
    private static bool IsGenericResultType(ITypeSymbol type)
    {
        // Direct check: the type itself is IGenericResult or GenericResult
        if (IsGenericResultTypeByName(type))
            return true;

        // Check all implemented interfaces
        foreach (var iface in type.AllInterfaces)
        {
            if (IsGenericResultTypeByName(iface))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Checks whether a specific type symbol matches IGenericResult or GenericResult by name and namespace.
    /// </summary>
    private static bool IsGenericResultTypeByName(ITypeSymbol type)
    {
        var typeName = type.Name;
        var namespaceName = type.ContainingNamespace?.ToDisplayString();

        if (string.Equals(typeName, "IGenericResult", System.StringComparison.Ordinal) &&
            string.Equals(namespaceName, "Fdw.Results", System.StringComparison.Ordinal))
        {
            return true;
        }

        if (string.Equals(typeName, "GenericResult", System.StringComparison.Ordinal) &&
            string.Equals(namespaceName, "Fdw.Results", System.StringComparison.Ordinal))
        {
            return true;
        }

        return false;
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
