using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fdw.Analyzers;

/// <summary>
/// Analyzer that warns against using the 'Async' suffix on method names.
/// Fdw convention: async methods should not have the 'Async' suffix.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AsyncSuffixAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for async suffix violation.
    /// </summary>
    public const string DiagnosticId = "FDW001";

    private const string Title = "Method name should not end with 'Async'";
    private const string MessageFormat = "Method '{0}' should not end with 'Async' suffix. Rename to '{1}'.";
    private const string Description = "Fdw convention: async methods should not have the 'Async' suffix. The return type (Task/ValueTask) already indicates the method is async.";
    private const string Category = "Naming";

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

        context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;
        var methodName = methodDeclaration.Identifier.Text;

        // Check if method name ends with "Async"
        if (!methodName.EndsWith("Async", System.StringComparison.Ordinal))
            return;

        // Skip interface implementations from external assemblies (like IHostedService.StartAsync)
        var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDeclaration);
        if (methodSymbol == null)
            return;

        // Check if this is implementing an interface method from an external assembly
        if (IsExternalInterfaceImplementation(methodSymbol))
            return;

        // Check if this is an override of a method from an external assembly
        if (IsExternalOverride(methodSymbol))
            return;

        // Calculate the suggested name
        var suggestedName = methodName.Substring(0, methodName.Length - 5);
        if (string.IsNullOrEmpty(suggestedName))
            suggestedName = methodName; // Don't suggest empty name

        var diagnostic = Diagnostic.Create(
            Rule,
            methodDeclaration.Identifier.GetLocation(),
            methodName,
            suggestedName);

        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsExternalInterfaceImplementation(IMethodSymbol methodSymbol)
    {
        // Check explicit interface implementations
        if (methodSymbol.ExplicitInterfaceImplementations.Length > 0)
        {
            foreach (var interfaceMethod in methodSymbol.ExplicitInterfaceImplementations)
            {
                if (IsFromExternalAssembly(interfaceMethod.ContainingType))
                    return true;
            }
        }

        // Check implicit interface implementations
        var containingType = methodSymbol.ContainingType;
        if (containingType == null)
            return false;

        foreach (var iface in containingType.AllInterfaces)
        {
            foreach (var member in iface.GetMembers())
            {
                if (member is IMethodSymbol interfaceMethod &&
                    SymbolEqualityComparer.Default.Equals(
                        containingType.FindImplementationForInterfaceMember(interfaceMethod),
                        methodSymbol))
                {
                    if (IsFromExternalAssembly(iface))
                        return true;
                }
            }
        }

        return false;
    }

    private static bool IsExternalOverride(IMethodSymbol methodSymbol)
    {
        // Check if this is an override of a method from an external assembly
        if (!methodSymbol.IsOverride)
            return false;

        var overriddenMethod = methodSymbol.OverriddenMethod;
        while (overriddenMethod != null)
        {
            if (IsFromExternalAssembly(overriddenMethod.ContainingType))
                return true;

            overriddenMethod = overriddenMethod.OverriddenMethod;
        }

        return false;
    }

    private static bool IsFromExternalAssembly(INamedTypeSymbol? typeSymbol)
    {
        if (typeSymbol == null)
            return false;

        // Check if the type is from an external assembly (not Fdw)
        var assemblyName = typeSymbol.ContainingAssembly?.Name;
        if (assemblyName is null || assemblyName.Length == 0)
            return false;

        // Our assemblies start with "Fdw"
        return !assemblyName.StartsWith("Fdw", System.StringComparison.Ordinal);
    }
}
