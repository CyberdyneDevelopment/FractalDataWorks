using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fdw.Conventions;

namespace Fdw.Registration.SourceGenerators;

/// <summary>
/// Generates module initializers to register [ServiceTypeOption] types cross-assembly.
/// </summary>
/// <remarks>
/// <para>
/// <b>This generator is referenced only by entry point projects</b> (executables like Reference.Api,
/// Reference.UI). Library packages do NOT reference it — they only define [ServiceTypeOption] types.
/// </para>
/// <para>
/// Two modes exist (selected automatically based on OutputKind):
/// </para>
/// <list type="number">
/// <item>
/// <b>DLL mode</b> (DynamicallyLinkedLibrary): Scans the DLL's OWN types for [ServiceTypeOption]
/// attributes that reference a collection in a DIFFERENT assembly. Generates a [ModuleInitializer]
/// in the DLL itself so that when the DLL loads, it self-registers into the external collection.
/// </item>
/// <item>
/// <b>Executable mode</b> (ConsoleApplication, WindowsApplication, etc.): Scans ALL referenced
/// assemblies for [ServiceTypeOption] types and generates a single [ModuleInitializer] in the
/// executable that registers every discovered option. This is the primary registration path.
/// </item>
/// </list>
/// </remarks>
[Generator]
public class ServiceTypeOptionModuleInitializerGenerator : IIncrementalGenerator
{
    private const string ServiceTypeOptionAttributeName = "Fdw.Collections.ServiceTypeOptionAttribute";
    private const string ReplacesAttributeName = "Fdw.Collections.Attributes.ReplacesAttribute";

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(context.CompilationProvider, Execute);
    }

    private static void Execute(
        SourceProductionContext context,
        Compilation compilation)
    {
        var assemblyName = compilation.AssemblyName ?? "Unknown";
        var isDll = compilation.Options.OutputKind == OutputKind.DynamicallyLinkedLibrary;

        List<ModuleInitOptionModel> options;
        List<string> diagnosticInfo;

        if (isDll)
        {
            // DLL mode: find own [ServiceTypeOption] types that reference collections in OTHER
            // assemblies. The DLL self-registers when it loads, before any user code runs.
            // If there are no cross-assembly options, emit nothing at all (not even diagnostics).
            (options, diagnosticInfo) = DiscoverOwnCrossAssemblyServiceTypeOptions(compilation);

            if (options.Count == 0)
                return;
        }
        else
        {
            // Executable mode: scan referenced assemblies for [ServiceTypeOption] types.
            (options, diagnosticInfo) = DiscoverOptionsInReferencedAssembliesWithDiagnostics(compilation);
        }

        var replacementMap = BuildReplacementMap(compilation, diagnosticInfo);
        options = options
            .Where(o => !replacementMap.ContainsKey(o.OptionFullTypeName))
            .ToList();

        diagnosticInfo.Add($"Replacement map entries: {replacementMap.Count}");
        foreach (var kvp in replacementMap)
        {
            diagnosticInfo.Add($"  [Replaces] {kvp.Key} => {kvp.Value}");
        }
        diagnosticInfo.Add($"Options after replacement filtering: {options.Count}");

        // Always generate a diagnostic file for executables so we can see what happened.
        // DLL mode only reaches here when there are cross-assembly options to register.
        var diagCode = GenerateDiagnosticFile(assemblyName, diagnosticInfo, options.Count);
        context.AddSource("ServiceTypeOptionModuleInitializer.Diagnostics.g.cs", SourceText.From(diagCode, Encoding.UTF8));

        if (options.Count == 0)
            return;

        // Group by collection
        var byCollection = options
            .GroupBy(o => o.CollectionFullName, StringComparer.Ordinal)
            .ToList();

        var code = GenerateModuleInitializer(byCollection, assemblyName);
        context.AddSource(
            "ServiceTypeOptionModuleInitializer.g.cs",
            SourceText.From(code, Encoding.UTF8));
    }

#pragma warning disable MA0051 // Source generator emits complete module initializer — splitting scatters the template
    private static string GenerateModuleInitializer(
        List<IGrouping<string, ModuleInitOptionModel>> byCollection,
        string assemblyName)
    {
        var sb = new StringBuilder();
        var namespaces = new HashSet<string>(StringComparer.Ordinal)
        {
            "System.Runtime.CompilerServices"
        };

        // Collect all namespaces needed
        foreach (var group in byCollection)
        {
            var firstOption = group.First();
            if (!string.IsNullOrEmpty(firstOption.CollectionNamespace))
                namespaces.Add(firstOption.CollectionNamespace);

            foreach (var option in group)
            {
                if (!string.IsNullOrEmpty(option.OptionNamespace))
                    namespaces.Add(option.OptionNamespace);
            }
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
        sb.AppendLine("    /// Module initializer for registering ServiceTypeOptions from referenced assemblies.");
        sb.AppendLine("    /// This runs automatically when this assembly loads, before any user code executes.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    internal static class ServiceTypeOptionRegistration");
        sb.AppendLine("    {");
        sb.AppendLine("        [ModuleInitializer]");
        sb.AppendLine("        internal static void Initialize()");
        sb.AppendLine("        {");
        sb.AppendLine("            try");
        sb.AppendLine("            {");

        foreach (var group in byCollection)
        {
            var collectionClassName = group.First().CollectionClassName;
            sb.AppendLine($"                // Register with {collectionClassName}");

            foreach (var option in group)
            {
                // Use fully qualified names to avoid ambiguity
                sb.AppendLine($"                global::{option.CollectionFullName}.RegisterMember(new global::{option.OptionFullTypeName}());");
            }
            sb.AppendLine();
        }

        sb.AppendLine("            }");
        sb.AppendLine("            catch (global::System.InvalidOperationException ex) when (ex.Message.Contains(\"frozen\", global::System.StringComparison.Ordinal))");
        sb.AppendLine("            {");
        sb.AppendLine($"                throw new global::System.InvalidOperationException(");
        sb.AppendLine($"                    \"[ServiceTypeOption Registration] Assembly '{assemblyName}' could not register service type options because one or more \" +");
        sb.AppendLine($"                    \"target collections were already frozen. Ensure this assembly is loaded before any \" +");
        sb.AppendLine($"                    \"code accesses the collection. Detail: \" + ex.Message, ex);");
        sb.AppendLine("            }");
        sb.AppendLine("            catch (global::System.Exception ex)");
        sb.AppendLine("            {");
        sb.AppendLine($"                throw new global::System.InvalidOperationException(");
        sb.AppendLine($"                    \"[ServiceTypeOption Registration] Assembly '{assemblyName}' failed to register service type options \" +");
        sb.AppendLine($"                    \"during module initialization. Detail: \" + ex.Message, ex);");
        sb.AppendLine("            }");
        sb.AppendLine("            finally");
        sb.AppendLine("            {");
        sb.AppendLine($"                global::System.Diagnostics.Debug.WriteLine(");
        sb.AppendLine($"                    \"[ServiceTypeOption Registration] Module initializer complete for '{assemblyName}'.\");");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private record struct ModuleInitOptionModel(
        string OptionTypeName,
        string OptionFullTypeName,
        string OptionNamespace,
        string CollectionFullName,
        string CollectionNamespace,
        string CollectionClassName
    );

    /// <summary>
    /// DLL mode: finds [ServiceTypeOption] types in the current compilation that point to
    /// a collection in a DIFFERENT assembly (cross-assembly registration candidates).
    /// Same-assembly options are handled by Collections.SourceGenerators static constructors.
    /// </summary>
    private static (List<ModuleInitOptionModel> Options, List<string> DiagnosticInfo) DiscoverOwnCrossAssemblyServiceTypeOptions(
        Compilation compilation)
    {
        var options = new List<ModuleInitOptionModel>();
        var diagnostics = new List<string>();
        var currentAssemblyName = compilation.AssemblyName ?? "";

        diagnostics.Add($"Assembly: {currentAssemblyName} (DLL mode — scanning own types for cross-assembly ServiceTypeOptions)");

        var attributeSymbol = compilation.GetTypeByMetadataName(ServiceTypeOptionAttributeName);
        diagnostics.Add($"ServiceTypeOptionAttribute found: {attributeSymbol != null}");

        if (attributeSymbol == null)
        {
            diagnostics.Add("ServiceTypeOptionAttribute not in compilation — skipping DLL scan");
            return (options, diagnostics);
        }

        ScanNamespaceForOwnOptions(compilation.GlobalNamespace, attributeSymbol, currentAssemblyName, options, diagnostics);

        diagnostics.Add($"Total cross-assembly ServiceTypeOptions found: {options.Count}");
        return (options, diagnostics);
    }

    private static void ScanNamespaceForOwnOptions(
        INamespaceSymbol ns,
        INamedTypeSymbol attributeSymbol,
        string currentAssemblyName,
        List<ModuleInitOptionModel> options,
        List<string> diagnostics)
    {
        foreach (var type in ns.GetTypeMembers())
            ScanTypeForOwnOptions(type, attributeSymbol, currentAssemblyName, options, diagnostics);

        foreach (var nestedNs in ns.GetNamespaceMembers())
            ScanNamespaceForOwnOptions(nestedNs, attributeSymbol, currentAssemblyName, options, diagnostics);
    }

    private static void ScanTypeForOwnOptions(
        INamedTypeSymbol type,
        INamedTypeSymbol attributeSymbol,
        string currentAssemblyName,
        List<ModuleInitOptionModel> options,
        List<string> diagnostics)
    {
        foreach (var nestedType in type.GetTypeMembers())
            ScanTypeForOwnOptions(nestedType, attributeSymbol, currentAssemblyName, options, diagnostics);

        if (type.IsGenericType || type.TypeParameters.Length > 0 || type.IsAbstract)
            return;

        var hasParameterlessConstructor = type.InstanceConstructors
            .Any(c => c.Parameters.Length == 0 && c.DeclaredAccessibility == Accessibility.Public);
        if (!hasParameterlessConstructor)
            return;

        foreach (var attr in type.GetAttributes())
        {
            if (!SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attributeSymbol))
                continue;

            if (attr.ConstructorArguments.Length < 2)
                continue;

            var collectionType = attr.ConstructorArguments[0].Value as ITypeSymbol;
            if (collectionType == null)
                continue;

            // Only generate registration for cross-assembly options.
            // Same-assembly options are handled by Collections.SourceGenerators static constructors.
            var collectionAssemblyName = collectionType.ContainingAssembly?.Name ?? "";
            if (string.Equals(collectionAssemblyName, currentAssemblyName, StringComparison.Ordinal))
                continue;

            var collectionFullName = collectionType.ToDisplayString();
            diagnostics.Add($"Found: {type.Name} → {collectionFullName} (collection in: {collectionAssemblyName})");

            options.Add(new ModuleInitOptionModel(
                OptionTypeName: type.Name,
                OptionFullTypeName: type.ToDisplayString(),
                OptionNamespace: type.ContainingNamespace?.ToDisplayString() ?? "",
                CollectionFullName: collectionFullName,
                CollectionNamespace: collectionType.ContainingNamespace?.ToDisplayString() ?? "",
                CollectionClassName: collectionType.Name
            ));
        }
    }

    private static (List<ModuleInitOptionModel> Options, List<string> DiagnosticInfo) DiscoverOptionsInReferencedAssembliesWithDiagnostics(Compilation compilation)
    {
        var options = new List<ModuleInitOptionModel>();
        var diagnostics = new List<string>();
        var attributeSymbol = compilation.GetTypeByMetadataName(ServiceTypeOptionAttributeName);

        diagnostics.Add($"Assembly: {compilation.AssemblyName}");
        diagnostics.Add($"AttributeSymbol found: {attributeSymbol != null}");

        if (attributeSymbol == null)
        {
            diagnostics.Add("ERROR: Could not find ServiceTypeOptionAttribute in compilation");
            return (options, diagnostics);
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

            // Find types with [ServiceTypeOption] attribute
            ScanNamespaceForOptionsWithDiagnostics(assemblySymbol.GlobalNamespace, attributeSymbol, options, foundInAssembly);

            if (foundInAssembly.Count > 0)
            {
                diagnostics.Add($"Assembly '{assemblyName}': Found {foundInAssembly.Count} options");
                foreach (var found in foundInAssembly)
                    diagnostics.Add($"  - {found}");
            }
        }

        diagnostics.Add($"Scanned {scannedAssemblies} assemblies, skipped {skippedAssemblies} system assemblies");
        diagnostics.Add($"Total options found: {options.Count}");

        return (options, diagnostics);
    }

    private static void ScanNamespaceForOptionsWithDiagnostics(
        INamespaceSymbol ns,
        INamedTypeSymbol attributeSymbol,
        List<ModuleInitOptionModel> options,
        List<string> foundInAssembly)
    {
        foreach (var type in ns.GetTypeMembers())
        {
            ScanTypeForOptionsWithDiagnostics(type, attributeSymbol, options, foundInAssembly);
        }

        foreach (var nestedNs in ns.GetNamespaceMembers())
        {
            ScanNamespaceForOptionsWithDiagnostics(nestedNs, attributeSymbol, options, foundInAssembly);
        }
    }

    [ConventionOverride(MaxCyclomaticComplexity = 19)]  // Roslyn symbol inspection walking type hierarchy — nested attribute discovery
    private static void ScanTypeForOptionsWithDiagnostics(
        INamedTypeSymbol type,
        INamedTypeSymbol attributeSymbol,
        List<ModuleInitOptionModel> options,
        List<string> foundInAssembly)
    {
        // Check nested types first
        foreach (var nestedType in type.GetTypeMembers())
        {
            ScanTypeForOptionsWithDiagnostics(nestedType, attributeSymbol, options, foundInAssembly);
        }

        // Skip generic types - can't instantiate with new()
        if (type.IsGenericType || type.TypeParameters.Length > 0)
            return;

        // Skip abstract types
        if (type.IsAbstract)
            return;

        // Must have parameterless constructor
        var hasParameterlessConstructor = type.InstanceConstructors
            .Any(c => c.Parameters.Length == 0 && c.DeclaredAccessibility == Accessibility.Public);
        if (!hasParameterlessConstructor)
            return;

        // Check for [ServiceTypeOption] attribute
        foreach (var attr in type.GetAttributes())
        {
            if (!SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attributeSymbol))
                continue;

            if (attr.ConstructorArguments.Length < 2)
                continue;

            var collectionType = attr.ConstructorArguments[0].Value as ITypeSymbol;
            if (collectionType == null)
                continue;

            foundInAssembly.Add(type.ToDisplayString());

            options.Add(new ModuleInitOptionModel(
                OptionTypeName: type.Name,
                OptionFullTypeName: type.ToDisplayString(),
                OptionNamespace: type.ContainingNamespace?.ToDisplayString() ?? "",
                CollectionFullName: collectionType.ToDisplayString(),
                CollectionNamespace: collectionType.ContainingNamespace?.ToDisplayString() ?? "",
                CollectionClassName: collectionType.Name
            ));
        }
    }

    /// <summary>
    /// Builds a replacement map from [Replaces] attributes across all referenced assemblies.
    /// </summary>
    /// <remarks>
    /// Why: Identical logic to TypeOptionModuleInitializerGenerator.BuildReplacementMap — both generators
    /// need the same [Replaces] resolution because TypeOptions and ServiceTypeOptions can both be replaced.
    /// </remarks>
    private static Dictionary<string, string> BuildReplacementMap(
        Compilation compilation,
        List<string> diagnostics)
    {
        var replacesAttrSymbol = compilation.GetTypeByMetadataName(ReplacesAttributeName);
        var map = new Dictionary<string, string>(StringComparer.Ordinal);

        if (replacesAttrSymbol == null)
        {
            diagnostics.Add("[Replaces] ReplacesAttribute not found in compilation — skipping replacement scan");
            return map;
        }

        var rawReplacements = new List<(string ReplacementFullName, string OriginalFullName)>();

        foreach (var reference in compilation.References)
        {
            var assemblySymbol = compilation.GetAssemblyOrModuleSymbol(reference) as IAssemblySymbol;
            if (assemblySymbol == null)
                continue;

            var asmName = assemblySymbol.Name;
            if (asmName.StartsWith("System", StringComparison.Ordinal) ||
                asmName.StartsWith("Microsoft", StringComparison.Ordinal) ||
                asmName.StartsWith("netstandard", StringComparison.Ordinal) ||
                asmName.StartsWith("mscorlib", StringComparison.Ordinal))
                continue;

            ScanNamespaceForReplaces(assemblySymbol.GlobalNamespace, replacesAttrSymbol, rawReplacements);
        }

        // Also scan current compilation
        ScanNamespaceForReplaces(compilation.Assembly.GlobalNamespace, replacesAttrSymbol, rawReplacements);

        var byOriginal = rawReplacements
            .GroupBy(r => r.OriginalFullName, StringComparer.Ordinal);

        foreach (var group in byOriginal)
        {
            var replacers = group.ToList();
            if (replacers.Count > 1)
            {
                diagnostics.Add($"  WARNING: Multiple replacements for '{group.Key}': {string.Join(", ", replacers.Select(r => r.ReplacementFullName))}");
            }
            else
            {
                map[group.Key] = replacers[0].ReplacementFullName;
            }
        }

        ResolveReplacementChains(map);
        return map;
    }

    private static void ResolveReplacementChains(Dictionary<string, string> map)
    {
        var resolved = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var key in map.Keys.ToList())
        {
            var current = key;
            var visited = new HashSet<string>(StringComparer.Ordinal) { current };

            while (map.TryGetValue(current, out var next))
            {
                if (map.ContainsKey(next) && !visited.Contains(next))
                {
                    visited.Add(next);
                    current = next;
                }
                else
                {
                    break;
                }
            }

            var terminal = map[current];
            foreach (var node in visited)
            {
                if (map.ContainsKey(node))
                    resolved[node] = terminal;
            }
        }

        foreach (var kvp in resolved)
        {
            map[kvp.Key] = kvp.Value;
        }
    }

    private static void ScanNamespaceForReplaces(
        INamespaceSymbol ns,
        INamedTypeSymbol replacesAttrSymbol,
        List<(string ReplacementFullName, string OriginalFullName)> results)
    {
        foreach (var type in ns.GetTypeMembers())
        {
            foreach (var attr in type.GetAttributes())
            {
                if (!SymbolEqualityComparer.Default.Equals(attr.AttributeClass, replacesAttrSymbol))
                    continue;

                if (attr.ConstructorArguments.Length < 1)
                    continue;

                var originalType = attr.ConstructorArguments[0].Value as ITypeSymbol;
                if (originalType == null)
                    continue;

                results.Add((type.ToDisplayString(), originalType.ToDisplayString()));
            }

            ScanNestedTypesForReplaces(type, replacesAttrSymbol, results);
        }

        foreach (var nestedNs in ns.GetNamespaceMembers())
        {
            ScanNamespaceForReplaces(nestedNs, replacesAttrSymbol, results);
        }
    }

    private static void ScanNestedTypesForReplaces(
        INamedTypeSymbol type,
        INamedTypeSymbol replacesAttrSymbol,
        List<(string ReplacementFullName, string OriginalFullName)> results)
    {
        foreach (var nested in type.GetTypeMembers())
        {
            foreach (var attr in nested.GetAttributes())
            {
                if (!SymbolEqualityComparer.Default.Equals(attr.AttributeClass, replacesAttrSymbol))
                    continue;

                if (attr.ConstructorArguments.Length < 1)
                    continue;

                var originalType = attr.ConstructorArguments[0].Value as ITypeSymbol;
                if (originalType == null)
                    continue;

                results.Add((nested.ToDisplayString(), originalType.ToDisplayString()));
            }

            ScanNestedTypesForReplaces(nested, replacesAttrSymbol, results);
        }
    }

    private static string GenerateDiagnosticFile(string assemblyName, List<string> diagnosticInfo, int optionsCount)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("// Diagnostic output from ServiceTypeOptionModuleInitializerGenerator");
        sb.AppendLine();
        sb.AppendLine("/*");
        sb.AppendLine($"Generator ran for: {assemblyName}");
        sb.AppendLine($"Options found: {optionsCount}");
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
}
