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
/// Exempt: an enum named MessageSeverity (Mike: "replace all of those other than messageseverity"), and the TypeCollection layer:
/// the assemblies that define the TypeCollection types and what they depend on (Fdw.Collections, Fdw.Collections.SourceGenerators,
/// Fdw.Abstractions, Fdw.Results.Abstractions). Mike: "there should not be any enums outside of type collections", and "the packages
/// that contain the type collection types can stay. because type collections don't exist prior to that" (FDW-5188, FDW-5194). That
/// replaces the earlier exemption of the Roslyn-side code. Using a library enum where the library is called is not reported; only
/// declaring or parsing one is.
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

    // The TypeCollection layer: Fdw.Collections defines the TypeCollection types, Fdw.Collections.SourceGenerators generates them,
    // and the other two are what those reference. Nothing below them can use a TypeCollection.
    private static readonly string[] TypeCollectionLayer =
        ["Fdw.Collections", "Fdw.Collections.SourceGenerators", "Fdw.Abstractions", "Fdw.Results.Abstractions"];

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "An enum is declared or parsed",
        "{0}: write a TypeCollection, or reach a library enum through an option that carries its value",
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "FDW takes no enums. A choice is a TypeCollection whose options carry their behaviour; a library enum at a boundary is carried on an option and read only where the library is called. MessageSeverity and the TypeCollection layer (Fdw.Collections, Fdw.Collections.SourceGenerators, Fdw.Abstractions, Fdw.Results.Abstractions) are exempt.");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            if (IsTypeCollectionLayer(start.Compilation.AssemblyName))
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
            if (location.IsInSource)
                context.ReportDiagnostic(Diagnostic.Create(Rule, location, $"enum {type.Name}"));
        }
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        var method = ((IInvocationOperation)context.Operation).TargetMethod;
        if ((!string.Equals(method.Name, "Parse", StringComparison.Ordinal) && !string.Equals(method.Name, "TryParse", StringComparison.Ordinal))
            || !string.Equals(method.ContainingType?.ToDisplayString(), EnumTypeName, StringComparison.Ordinal))
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, context.Operation.Syntax.GetLocation(), $"Enum.{method.Name}"));
    }

    private static bool IsTypeCollectionLayer(string? assemblyName)
    {
        if (assemblyName is null)
            return false;

        foreach (var layerAssembly in TypeCollectionLayer)
        {
            if (string.Equals(assemblyName, layerAssembly, StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
