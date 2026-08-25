using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fdw.Analyzers;

/// <summary>
/// Analyzer that warns against using plain string messages in GenericResult.Failure().
/// Fdw convention: Use MessageLogging methods or ResultCodes for structured logging.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PlainStringFailureAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for plain string failure violation.
    /// </summary>
    public const string DiagnosticId = "FDW002";

    private const string Title = "Use MessageLogging or ResultCode instead of plain string";
    private const string MessageFormat = "'{0}' should use MessageLogging method or IResultCode instead of plain string '{1}'";
    private const string Description = "Fdw convention: Use MessageLogging methods that return IGenericMessage, or IResultCode for structured logging integration. Plain strings bypass the logging framework.";
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

        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        // Check if this is a method invocation with member access (e.g., GenericResult.Failure)
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return;

        var methodName = memberAccess.Name.Identifier.Text;

        // Only check Failure methods
        if (!string.Equals(methodName, "Failure", System.StringComparison.Ordinal))
            return;

        // Get the symbol to verify it's GenericResult or GenericResult<T>
        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
            return;

        var containingTypeName = methodSymbol.ContainingType?.Name;
        if (!string.Equals(containingTypeName, "GenericResult", System.StringComparison.Ordinal))
            return;

        // Check the containing namespace
        var containingNamespace = methodSymbol.ContainingType?.ContainingNamespace?.ToDisplayString();
        if (!string.Equals(containingNamespace, "Fdw.Results", System.StringComparison.Ordinal))
            return;

        // Check if the first argument is a string literal or interpolated string
        var arguments = invocation.ArgumentList.Arguments;
        if (arguments.Count == 0)
            return;

        var firstArg = arguments[0].Expression;

        // Check for string literal
        if (firstArg is LiteralExpressionSyntax literal &&
            literal.Kind() == SyntaxKind.StringLiteralExpression)
        {
            var stringValue = literal.Token.ValueText;
            var truncatedValue = stringValue.Length > 30 ? stringValue.Substring(0, 30) + "..." : stringValue;

            var diagnostic = Diagnostic.Create(
                Rule,
                invocation.GetLocation(),
                "GenericResult.Failure",
                truncatedValue);

            context.ReportDiagnostic(diagnostic);
            return;
        }

        // Check for interpolated string
        if (firstArg is InterpolatedStringExpressionSyntax)
        {
            var diagnostic = Diagnostic.Create(
                Rule,
                invocation.GetLocation(),
                "GenericResult.Failure",
                "$\"...\" (interpolated string)");

            context.ReportDiagnostic(diagnostic);
            return;
        }

        // Check if the argument type is string (but not IGenericMessage or IResultCode)
        var argType = context.SemanticModel.GetTypeInfo(firstArg).Type;
        if (argType?.SpecialType == SpecialType.System_String)
        {
            var diagnostic = Diagnostic.Create(
                Rule,
                invocation.GetLocation(),
                "GenericResult.Failure",
                "(string variable)");

            context.ReportDiagnostic(diagnostic);
        }
    }
}
