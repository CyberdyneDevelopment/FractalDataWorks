using System;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fdw.Conventions.Analyzers;

/// <summary>
/// Analyzer that warns when method names contain underscores.
/// Skips test methods, P/Invoke declarations, explicit interface implementations,
/// external overrides, and external interface implementations.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MethodNameUnderscoreAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for method name underscore violation.
    /// </summary>
    public const string DiagnosticId = "FDW008";

    private const string Title = "Method name should not contain underscores";
    private const string MessageFormat = "Method '{0}' contains underscores; rename to '{1}'";
    private const string Description = "Fdw convention: method names should use PascalCase without underscores. Test methods are excluded from this rule.";
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

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var assemblyName = compilationContext.Compilation.AssemblyName ?? string.Empty;
            var isTestProject = assemblyName.EndsWith(".Tests", StringComparison.OrdinalIgnoreCase);

            // Skip entire test projects
            if (isTestProject)
                return;

            compilationContext.RegisterSyntaxNodeAction(
                AnalyzeMethod,
                SyntaxKind.MethodDeclaration);
        });
    }

    private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;
        var methodName = methodDeclaration.Identifier.Text;

        // No underscore = no problem
        if (!methodName.Contains("_"))
            return;

        // Skip explicit interface implementations
        if (methodDeclaration.ExplicitInterfaceSpecifier != null)
            return;

        // Skip test methods
        if (HasTestAttribute(methodDeclaration))
            return;

        // Skip P/Invoke methods
        if (HasPInvokeAttribute(methodDeclaration))
            return;

        // Need semantic model for override/interface checks
        var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDeclaration);
        if (methodSymbol == null)
            return;

        // Skip external overrides
        if (IsExternalOverride(methodSymbol))
            return;

        // Skip external interface implementations
        if (IsExternalInterfaceImplementation(methodSymbol))
            return;

        var suggestedName = RemoveUnderscores(methodName);
        if (string.Equals(suggestedName, methodName, StringComparison.Ordinal))
            return;

        var diagnostic = Diagnostic.Create(
            Rule,
            methodDeclaration.Identifier.GetLocation(),
            methodName,
            suggestedName);

        context.ReportDiagnostic(diagnostic);
    }

    private static bool HasTestAttribute(MethodDeclarationSyntax method)
    {
        foreach (var attrList in method.AttributeLists)
        {
            foreach (var attr in attrList.Attributes)
            {
                var name = attr.Name.ToString();
                if (string.Equals(name, "Fact", StringComparison.Ordinal) ||
                    string.Equals(name, "Theory", StringComparison.Ordinal) ||
                    string.Equals(name, "Test", StringComparison.Ordinal) ||
                    string.Equals(name, "TestMethod", StringComparison.Ordinal) ||
                    string.Equals(name, "FactAttribute", StringComparison.Ordinal) ||
                    string.Equals(name, "TheoryAttribute", StringComparison.Ordinal) ||
                    string.Equals(name, "TestAttribute", StringComparison.Ordinal) ||
                    string.Equals(name, "TestMethodAttribute", StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool HasPInvokeAttribute(MethodDeclarationSyntax method)
    {
        foreach (var attrList in method.AttributeLists)
        {
            foreach (var attr in attrList.Attributes)
            {
                var name = attr.Name.ToString();
                if (string.Equals(name, "DllImport", StringComparison.Ordinal) ||
                    string.Equals(name, "DllImportAttribute", StringComparison.Ordinal) ||
                    string.Equals(name, "LibraryImport", StringComparison.Ordinal) ||
                    string.Equals(name, "LibraryImportAttribute", StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
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

        var assemblyName = typeSymbol.ContainingAssembly?.Name;
        if (assemblyName is null || assemblyName.Length == 0)
            return false;

        return !assemblyName.StartsWith("Fdw", StringComparison.Ordinal);
    }

    internal static string RemoveUnderscores(string name)
    {
        if (string.IsNullOrEmpty(name) || !name.Contains("_"))
            return name;

        var sb = new StringBuilder(name.Length);
        var capitalizeNext = false;

        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (c == '_')
            {
                capitalizeNext = true;
                continue;
            }

            if (capitalizeNext)
            {
                sb.Append(char.ToUpperInvariant(c));
                capitalizeNext = false;
            }
            else
            {
                sb.Append(c);
            }
        }

        // Ensure first character is uppercase (PascalCase)
        if (sb.Length > 0 && char.IsLower(sb[0]))
        {
            sb[0] = char.ToUpperInvariant(sb[0]);
        }

        return sb.Length > 0 ? sb.ToString() : name;
    }
}
