using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fdw.Analyzers;

/// <summary>
/// Analyzer that detects catch blocks in methods returning IGenericResult where the caught
/// exception's information is not propagated in the returned result.
/// Fdw convention: Catch blocks in methods returning GenericResult must propagate
/// exception information through the result, either as a Failure with message or a Success
/// with message for retry scenarios.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ExceptionNotPropagatedAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for exception not propagated violation.
    /// </summary>
    public const string DiagnosticId = "FDW014";

    private const string Title = "Exception not propagated in GenericResult";
    private const string MessageFormat = "Exception caught in '{0}' but not propagated in the returned GenericResult";
    private const string Description = "Fdw convention: Catch blocks in methods returning GenericResult must propagate exception information through the result, either as a Failure with message or a Success with message for retry scenarios.";
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
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeCatchClause, SyntaxKind.CatchClause);
    }

    private static void AnalyzeCatchClause(SyntaxNodeAnalysisContext context)
    {
        // Skip test projects
        if (IsTestProject(context))
            return;

        var catchClause = (CatchClauseSyntax)context.Node;

        // Find the enclosing method or local function
        var enclosingMethod = FindEnclosingMethod(catchClause);
        if (enclosingMethod == null)
            return;

        string methodName;
        ITypeSymbol? returnType;

        if (enclosingMethod is MethodDeclarationSyntax methodDecl)
        {
            var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDecl);
            if (methodSymbol == null)
                return;

            methodName = methodSymbol.Name;
            returnType = methodSymbol.ReturnType;
        }
        else if (enclosingMethod is LocalFunctionStatementSyntax localFunc)
        {
            var localSymbol = context.SemanticModel.GetDeclaredSymbol(localFunc);
            if (localSymbol == null)
                return;

            methodName = localSymbol.Name;
            returnType = localSymbol.ReturnType;
        }
        else
        {
            return;
        }

        // Check if the return type is or implements IGenericResult (unwrapping Task<T>/ValueTask<T>)
        if (!IsGenericResultReturnType(returnType))
            return;

        // Check if the catch block has a throw statement (re-throwing is fine)
        if (HasThrowStatement(catchClause))
            return;

        // Check if the catch block has return statements
        var returnStatements = catchClause.Block.DescendantNodes()
            .OfType<ReturnStatementSyntax>()
            .ToList();

        // No return statement at all in a result-returning method catch block
        if (returnStatements.Count == 0)
        {
            // Skip empty catch blocks with no exception variable — intentional "swallow and continue"
            // e.g., catch { /* timezone fallback */ } — bare catch with no statements
            if (catchClause.Declaration == null && catchClause.Block.Statements.Count == 0)
                return;

            var diagnostic = Diagnostic.Create(
                Rule,
                catchClause.CatchKeyword.GetLocation(),
                methodName);

            context.ReportDiagnostic(diagnostic);
            return;
        }

        // Inspect each return statement
        foreach (var returnStatement in returnStatements)
        {
            // Skip return statements that are inside nested lambdas, anonymous methods, or local functions
            if (IsInsideNestedFunction(returnStatement, catchClause))
                continue;

            if (!IsReturnPropagatingException(returnStatement, context))
            {
                var diagnostic = Diagnostic.Create(
                    Rule,
                    returnStatement.GetLocation(),
                    methodName);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private static SyntaxNode? FindEnclosingMethod(SyntaxNode node)
    {
        var current = node.Parent;
        while (current != null)
        {
            if (current is MethodDeclarationSyntax || current is LocalFunctionStatementSyntax)
                return current;

            current = current.Parent;
        }

        return null;
    }

    private static bool IsGenericResultReturnType(ITypeSymbol? type)
    {
        if (type == null)
            return false;

        // Unwrap Task<T> or ValueTask<T>
        var unwrapped = UnwrapTaskType(type);

        return ImplementsIGenericResult(unwrapped);
    }

    private static ITypeSymbol UnwrapTaskType(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            var typeName = namedType.Name;
            var namespaceName = namedType.ContainingNamespace?.ToDisplayString();

            if ((string.Equals(typeName, "Task", System.StringComparison.Ordinal) ||
                 string.Equals(typeName, "ValueTask", System.StringComparison.Ordinal)) &&
                string.Equals(namespaceName, "System.Threading.Tasks", System.StringComparison.Ordinal) &&
                namedType.TypeArguments.Length == 1)
            {
                return namedType.TypeArguments[0];
            }
        }

        return type;
    }

    private static bool ImplementsIGenericResult(ITypeSymbol type)
    {
        // Check the type itself
        if (IsIGenericResultType(type))
            return true;

        // Check all interfaces
        foreach (var iface in type.AllInterfaces)
        {
            if (IsIGenericResultType(iface))
                return true;
        }

        return false;
    }

    private static bool IsIGenericResultType(ITypeSymbol type)
    {
        var typeName = type.Name;
        if (!string.Equals(typeName, "IGenericResult", System.StringComparison.Ordinal))
            return false;

        var namespaceName = type.ContainingNamespace?.ToDisplayString();
        return string.Equals(namespaceName, "Fdw.Results", System.StringComparison.Ordinal);
    }

    private static bool HasThrowStatement(CatchClauseSyntax catchClause)
    {
        foreach (var node in catchClause.Block.DescendantNodes())
        {
            if (node is ThrowStatementSyntax)
                return true;

            // Also check throw expressions (C# 7+)
            if (node is ThrowExpressionSyntax)
                return true;
        }

        return false;
    }

    private static bool IsInsideNestedFunction(ReturnStatementSyntax returnStatement, CatchClauseSyntax catchClause)
    {
        var current = returnStatement.Parent;
        while (current != null && current != catchClause)
        {
            if (current is LambdaExpressionSyntax ||
                current is AnonymousMethodExpressionSyntax ||
                current is LocalFunctionStatementSyntax)
            {
                return true;
            }

            current = current.Parent;
        }

        return false;
    }

    private static bool IsReturnPropagatingException(
        ReturnStatementSyntax returnStatement,
        SyntaxNodeAnalysisContext context)
    {
        var expression = returnStatement.Expression;
        if (expression == null)
            return false;

        // Handle await expressions: unwrap to get the inner invocation
        if (expression is AwaitExpressionSyntax awaitExpr)
            expression = awaitExpr.Expression;

        // Check if the returned expression is an invocation
        if (expression is InvocationExpressionSyntax invocation)
        {
            // Handle Task.FromResult(...) wrapping
            if (IsTaskFromResultCall(invocation, context) &&
                invocation.ArgumentList.Arguments.Count == 1)
            {
                var innerExpression = invocation.ArgumentList.Arguments[0].Expression;
                if (innerExpression is InvocationExpressionSyntax innerInvocation)
                    return IsAcceptableResultInvocation(innerInvocation, context);

                return false;
            }

            // Check if it's a direct GenericResult factory call
            if (IsAcceptableResultInvocation(invocation, context))
                return true;

            // Accept non-GenericResult helper methods whose return type implements IGenericResult
            // (delegating result creation to a helper method like HandleCancellation/HandleException)
            var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation);
            if (symbolInfo.Symbol is IMethodSymbol calledMethod)
            {
                var containingTypeName = calledMethod.ContainingType?.Name;
                var containingNamespace = calledMethod.ContainingType?.ContainingNamespace?.ToDisplayString();

                var isOnGenericResult =
                    string.Equals(containingTypeName, "GenericResult", System.StringComparison.Ordinal) &&
                    string.Equals(containingNamespace, "Fdw.Results", System.StringComparison.Ordinal);

                if (!isOnGenericResult)
                {
                    var returnType = UnwrapTaskType(calledMethod.ReturnType);
                    if (ImplementsIGenericResult(returnType))
                        return true;
                }
            }
        }

        return false;
    }

    private static bool IsAcceptableResultInvocation(
        InvocationExpressionSyntax invocation,
        SyntaxNodeAnalysisContext context)
    {
        // Get the method symbol
        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
            return false;

        var containingTypeName = methodSymbol.ContainingType?.Name;
        var containingNamespace = methodSymbol.ContainingType?.ContainingNamespace?.ToDisplayString();
        var invokedMethodName = methodSymbol.Name;

        // Check if it's on GenericResult in Fdw.Results
        if (!string.Equals(containingTypeName, "GenericResult", System.StringComparison.Ordinal) ||
            !string.Equals(containingNamespace, "Fdw.Results", System.StringComparison.Ordinal))
        {
            return false;
        }

        // Chain(...) is always OK
        if (string.Equals(invokedMethodName, "Chain", System.StringComparison.Ordinal))
            return true;

        // Failure(...) with at least one argument is OK
        if (string.Equals(invokedMethodName, "Failure", System.StringComparison.Ordinal))
        {
            var argCount = invocation.ArgumentList.Arguments.Count;
            return argCount >= 1;
        }

        // Success(...) inspection:
        // 0 args (bare Success()) → FDW014
        // 1 arg (just value, Success(value)) → FDW014
        // 2+ args (value + message(s)) → OK
        if (string.Equals(invokedMethodName, "Success", System.StringComparison.Ordinal))
        {
            var argCount = invocation.ArgumentList.Arguments.Count;
            return argCount >= 2;
        }

        // Unknown method on GenericResult — be conservative, don't report
        return true;
    }

    private static bool IsTaskFromResultCall(
        InvocationExpressionSyntax invocation,
        SyntaxNodeAnalysisContext context)
    {
        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
            return false;

        return string.Equals(methodSymbol.Name, "FromResult", System.StringComparison.Ordinal) &&
               string.Equals(methodSymbol.ContainingType?.Name, "Task", System.StringComparison.Ordinal) &&
               string.Equals(
                   methodSymbol.ContainingType?.ContainingNamespace?.ToDisplayString(),
                   "System.Threading.Tasks",
                   System.StringComparison.Ordinal);
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
