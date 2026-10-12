using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fdw.Analyzers;

/// <summary>
/// Analyzer that warns against direct ILogger calls outside of MessageLogging static classes.
/// Fdw convention: All logging should go through [MessageLogging] attributed methods.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DirectLoggerCallAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for direct logger call violation.
    /// </summary>
    public const string DiagnosticId = "FDW003";

    private const string Title = "Use MessageLogging method instead of direct ILogger call";
    private const string MessageFormat = "Direct '{0}' call should be replaced with a MessageLogging method from a *Log static class";
    private const string Description = "Fdw convention: Use MessageLogging methods (e.g., DomainLog.OperationFailed) instead of direct ILogger calls. This ensures structured logging integration with the Result framework.";
    private const string Category = "Usage";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description);

    // Logger methods that should be flagged
    private static readonly ImmutableHashSet<string> LoggerMethods = ImmutableHashSet.Create(
        "Log",
        "LogTrace",
        "LogDebug",
        "LogInformation",
        "LogWarning",
        "LogError",
        "LogCritical");

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

        // Check if this is a method invocation with member access (e.g., _logger.LogError)
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return;

        var methodName = memberAccess.Name.Identifier.Text;

        // Only check logger methods
        if (!LoggerMethods.Contains(methodName))
            return;

        // Skip if we're inside a *Log static class (these are MessageLogging implementations)
        var containingClass = GetContainingClass(invocation);
        if (containingClass != null)
        {
            var className = containingClass.Identifier.Text;
            if (className.EndsWith("Log", System.StringComparison.Ordinal) &&
                containingClass.Modifiers.Any(SyntaxKind.StaticKeyword))
            {
                // This is likely a MessageLogging implementation class
                return;
            }
        }

        // Get the symbol to verify it's ILogger
        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
            return;

        // Check if the containing type is ILogger or implements ILogger
        var containingType = methodSymbol.ContainingType;
        if (!IsLoggerType(containingType))
            return;

        // Check if this is an extension method on ILogger
        if (methodSymbol.IsExtensionMethod)
        {
            var receiverType = methodSymbol.ReceiverType;
            if (receiverType != null && !IsLoggerType(receiverType as INamedTypeSymbol))
                return;
        }

        var diagnostic = Diagnostic.Create(
            Rule,
            invocation.GetLocation(),
            methodName);

        context.ReportDiagnostic(diagnostic);
    }

    private static ClassDeclarationSyntax? GetContainingClass(SyntaxNode node)
    {
        var current = node.Parent;
        while (current != null)
        {
            if (current is ClassDeclarationSyntax classDecl)
                return classDecl;
            current = current.Parent;
        }
        return null;
    }

    private static bool IsLoggerType(INamedTypeSymbol? type)
    {
        if (type == null)
            return false;

        // Check for ILogger or ILogger<T>
        var typeName = type.Name;
        if (string.Equals(typeName, "ILogger", System.StringComparison.Ordinal))
            return true;

        // Check interfaces
        foreach (var iface in type.AllInterfaces)
        {
            if (string.Equals(iface.Name, "ILogger", System.StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
