using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Fdw.Analyzers;

/// <summary>
/// FDW059: no enums. A domain choice is a TypeCollection, and a library enum is reached through an option that carries
/// its value; so an enum declaration and a call to <c>Enum.Parse</c> or <c>Enum.TryParse</c> are refused (Mike, 2026-11-05:
/// enums are "expressly forbidden in fdw"; FDW-4143).
/// </summary>
/// <remarks>
/// Exempt: an enum named MessageSeverity (Mike: "replace all of those other than messageseverity"), and the Roslyn-side code
/// (Mike: "the roslyn ones are an allowable exception"), meaning an assembly whose name ends in <c>.SourceGenerators</c>,
/// <c>.Analyzers</c> or <c>.CodeFixes</c>, and a file under <c>/tooling/roslyn/</c>. Using a library enum where the library is
/// called is not reported; only declaring or parsing one is.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoEnumAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for an enum declared or parsed.
    /// </summary>
    public const string DiagnosticId = "FDW059";

    private const string ExemptEnumName = "MessageSeverity";
    private const string EnumTypeName = "System.Enum";
    private const string RoslynToolingPath = "/tooling/roslyn/";

    private static readonly string[] ExemptAssemblySuffixes = [".SourceGenerators", ".Analyzers", ".CodeFixes"];

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "An enum is declared or parsed",
        "{0}: write a TypeCollection, or reach a library enum through an option that carries its value",
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "FDW takes no enums. A choice is a TypeCollection whose options carry their behaviour; a library enum at a boundary is carried on an option and read only where the library is called. MessageSeverity and the Roslyn-side projects are exempt.");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            if (IsExemptAssembly(start.Compilation.AssemblyName))
                return;

            start.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
            start.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
        });
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        if (type.TypeKind != TypeKind.Enum || string.Equals(type.Name, ExemptEnumName, StringComparison.Ordinal))
            return;

        foreach (var location in type.Locations)
        {
            if (location.IsInSource && !IsRoslynTooling(location.SourceTree?.FilePath))
                context.ReportDiagnostic(Diagnostic.Create(Rule, location, $"enum {type.Name}"));
        }
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        var method = ((IInvocationOperation)context.Operation).TargetMethod;
        if ((!string.Equals(method.Name, "Parse", StringComparison.Ordinal) && !string.Equals(method.Name, "TryParse", StringComparison.Ordinal))
            || !string.Equals(method.ContainingType?.ToDisplayString(), EnumTypeName, StringComparison.Ordinal)
            || IsRoslynTooling(context.Operation.Syntax.SyntaxTree.FilePath))
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, context.Operation.Syntax.GetLocation(), $"Enum.{method.Name}"));
    }

    private static bool IsExemptAssembly(string? assemblyName)
    {
        if (assemblyName is null)
            return false;

        foreach (var suffix in ExemptAssemblySuffixes)
        {
            if (assemblyName.EndsWith(suffix, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static bool IsRoslynTooling(string? filePath) =>
        filePath is not null && filePath.Replace('\\', '/').IndexOf(RoslynToolingPath, StringComparison.Ordinal) >= 0;
}
