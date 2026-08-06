using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fdw.Registration.SourceGenerators;

/// <summary>
/// Generates module initializers in CONSUMING assemblies to register POCO mappers
/// from REFERENCED assemblies.
///
/// Key insight: Module initializers only run when their assembly loads. Library assemblies
/// (like Configuration.MsSql) may never load if nothing directly accesses them.
/// By generating the initializer in the consuming assembly (like Reference.Api), we guarantee
/// it runs when the application starts.
///
/// This generator:
/// 1. SKIPS library assemblies (OutputKind == DynamicallyLinkedLibrary)
/// 2. GENERATES in executable assemblies that reference libraries with [GenerateMapper] types
/// </summary>
[Generator]
public sealed class PocoMapperModuleInitializerGenerator : IIncrementalGenerator
{
    private const string GenerateMapperAttributeName = "Fdw.Data.GenerateMapperAttribute";

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(context.CompilationProvider, Execute);
    }

    private static void Execute(
        SourceProductionContext context,
        Compilation compilation)
    {
        // Library assemblies skip module initializer generation — only executables need it
        if (compilation.Options.OutputKind == OutputKind.DynamicallyLinkedLibrary)
            return;

        // Scan referenced assemblies for [GenerateMapper] types
        var (mappersFromReferences, diagnosticInfo) = DiscoverMappersInReferencedAssemblies(compilation);

        // Always generate a diagnostic file so we can see what happened
        var diagCode = GenerateDiagnosticFile(compilation.AssemblyName ?? "Unknown", diagnosticInfo, mappersFromReferences.Count);
        context.AddSource("PocoMapperModuleInitializer.Diagnostics.g.cs", SourceText.From(diagCode, Encoding.UTF8));

        if (mappersFromReferences.Count == 0)
            return;

        var code = GenerateModuleInitializer(mappersFromReferences, compilation.AssemblyName ?? "Unknown");
        context.AddSource(
            "PocoMapperModuleInitializer.g.cs",
            SourceText.From(code, Encoding.UTF8));
    }

    private static (List<PocoMapperModel> Mappers, List<string> DiagnosticInfo) DiscoverMappersInReferencedAssemblies(Compilation compilation)
    {
        var mappers = new List<PocoMapperModel>();
        var diagnostics = new List<string>();
        var attributeSymbol = compilation.GetTypeByMetadataName(GenerateMapperAttributeName);

        diagnostics.Add($"Assembly: {compilation.AssemblyName}");
        diagnostics.Add($"AttributeSymbol found: {attributeSymbol != null}");

        if (attributeSymbol == null)
        {
            diagnostics.Add("ERROR: Could not find GenerateMapperAttribute in compilation");
            return (mappers, diagnostics);
        }

        diagnostics.Add($"AttributeSymbol: {attributeSymbol.ToDisplayString()}");
        diagnostics.Add($"Reference count: {compilation.References.Count()}");

        int scannedAssemblies = 0;
        int skippedAssemblies = 0;

        // Scan all referenced assemblies
        foreach (var reference in compilation.References)
        {
            var assemblySymbol = compilation.GetAssemblyOrModuleSymbol(reference) as IAssemblySymbol;
            if (assemblySymbol == null)
                continue;

            // Skip system assemblies
            var assemblyName = assemblySymbol.Name;
            if (assemblyName.StartsWith("System", StringComparison.Ordinal) ||
                assemblyName.StartsWith("Microsoft", StringComparison.Ordinal) ||
                assemblyName.StartsWith("netstandard", StringComparison.Ordinal) ||
                assemblyName.StartsWith("mscorlib", StringComparison.Ordinal))
            {
                skippedAssemblies++;
                continue;
            }

            scannedAssemblies++;
            var foundInAssembly = new List<string>();

            // Find types with [GenerateMapper] attribute
            ScanNamespaceForMappers(assemblySymbol.GlobalNamespace, attributeSymbol, mappers, foundInAssembly);

            if (foundInAssembly.Count > 0)
            {
                diagnostics.Add($"Assembly '{assemblyName}': Found {foundInAssembly.Count} mappers");
                foreach (var found in foundInAssembly)
                    diagnostics.Add($"  - {found}");
            }
        }

        diagnostics.Add($"Scanned {scannedAssemblies} assemblies, skipped {skippedAssemblies} system assemblies");
        diagnostics.Add($"Total mappers found: {mappers.Count}");

        return (mappers, diagnostics);
    }

    private static void ScanNamespaceForMappers(
        INamespaceSymbol ns,
        INamedTypeSymbol attributeSymbol,
        List<PocoMapperModel> mappers,
        List<string> foundInAssembly)
    {
        foreach (var type in ns.GetTypeMembers())
        {
            ScanTypeForMappers(type, attributeSymbol, mappers, foundInAssembly);
        }

        foreach (var nestedNs in ns.GetNamespaceMembers())
        {
            ScanNamespaceForMappers(nestedNs, attributeSymbol, mappers, foundInAssembly);
        }
    }

    private static void ScanTypeForMappers(
        INamedTypeSymbol type,
        INamedTypeSymbol attributeSymbol,
        List<PocoMapperModel> mappers,
        List<string> foundInAssembly)
    {
        // Check nested types first
        foreach (var nestedType in type.GetTypeMembers())
        {
            ScanTypeForMappers(nestedType, attributeSymbol, mappers, foundInAssembly);
        }

        // Skip generic types - mappers are generated for concrete types only
        if (type.IsGenericType || type.TypeParameters.Length > 0)
            return;

        // Check for [GenerateMapper] attribute
        foreach (var attr in type.GetAttributes())
        {
            if (!SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attributeSymbol))
                continue;

            foundInAssembly.Add(type.ToDisplayString());

            // The mapper class is generated with name {TypeName}PocoMapper
            // in the same namespace as the type
            mappers.Add(new PocoMapperModel(
                TypeName: type.Name,
                TypeFullName: type.ToDisplayString(),
                Namespace: type.ContainingNamespace?.ToDisplayString() ?? "",
                MapperTypeName: $"{type.Name}PocoMapper"
            ));
        }
    }

    private static string GenerateModuleInitializer(
        List<PocoMapperModel> mappers,
        string assemblyName)
    {
        var sb = new StringBuilder();
        var namespaces = new HashSet<string>(StringComparer.Ordinal)
        {
            "System.Runtime.CompilerServices",
            "Fdw.Data.Abstractions.Mappers.PocoMappers"
        };

        // Collect all namespaces needed
        foreach (var mapper in mappers)
        {
            if (!string.IsNullOrEmpty(mapper.Namespace))
                namespaces.Add(mapper.Namespace);
        }

        // Use a unique namespace based on assembly name to avoid conflicts
        var safeAssemblyName = assemblyName.Replace(".", "_").Replace("-", "_");

        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();

        foreach (var ns in namespaces.OrderBy(n => n, StringComparer.Ordinal))
        {
            sb.AppendLine($"using {ns};");
        }

        sb.AppendLine();
        sb.AppendLine($"namespace {safeAssemblyName}.Generated");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Module initializer for registering POCO mappers from referenced assemblies.");
        sb.AppendLine("    /// This runs automatically when this assembly loads, before any user code executes.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    internal static class PocoMapperRegistration");
        sb.AppendLine("    {");
        sb.AppendLine("        [ModuleInitializer]");
        sb.AppendLine("        internal static void Initialize()");
        sb.AppendLine("        {");

        foreach (var mapper in mappers)
        {
            // Use fully qualified names to avoid ambiguity
            sb.AppendLine($"            PocoMapperCollection.RegisterMember(new global::{mapper.Namespace}.{mapper.MapperTypeName}());");
        }

        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string GenerateDiagnosticFile(string assemblyName, List<string> diagnosticInfo, int mappersCount)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("// Diagnostic output from PocoMapperModuleInitializerGenerator");
        sb.AppendLine();
        sb.AppendLine("/*");
        sb.AppendLine($"Generator ran for: {assemblyName}");
        sb.AppendLine($"Mappers found: {mappersCount}");
        sb.AppendLine();
        sb.AppendLine("Diagnostic Info:");
        foreach (var line in diagnosticInfo)
        {
            sb.AppendLine($"  {line}");
        }
        sb.AppendLine("*/");
        sb.AppendLine();
        sb.AppendLine("// This file is intentionally empty - it exists only for diagnostic purposes");

        return sb.ToString();
    }

    private readonly record struct PocoMapperModel(
        string TypeName,
        string TypeFullName,
        string Namespace,
        string MapperTypeName
    );
}
