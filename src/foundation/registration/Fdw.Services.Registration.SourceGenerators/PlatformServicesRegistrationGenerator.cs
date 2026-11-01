using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fdw.Services.Registration.SourceGenerators;

/// <summary>
/// Opt-in generator that discovers every <c>[ServiceTypeCollection]</c>-decorated class and emits a
/// <c>[ModuleInitializer]</c> registering each one into <c>Fdw.ServiceTypes.PlatformServices</c>, plus
/// a per-category accessor extension member so a specific domain can be looked up and driven manually
/// (e.g. <c>PlatformServices.Connection.Initialize(app.Services, loggerFactory)</c>).
/// </summary>
/// <remarks>
/// <para>
/// Runs only in entry-point assemblies (executables), and only when the consumer's compilation can see
/// <c>Fdw.ServiceTypes.PlatformServices</c> (i.e. it references <c>Fdw.Services.Registration</c>) — an
/// app that doesn't reference either project gets none of this; the feature is fully opt-in.
/// </para>
/// <para>
/// This generator emits no ordering. A host controls order by running a domain's own entry ahead of
/// the collect, or deferring it out of the collect and running it afterwards. It does not compute a
/// cross-domain dependency order automatically:
/// Roslyn cannot see method-body syntax for types compiled in a referenced assembly, only current-
/// compilation source has a syntax tree, so an automatic dependency scan is not implementable here.
/// </para>
/// <para>
/// Every discovered class is guaranteed to declare the required static
/// <c>Configure&lt;TBuilder&gt;(TBuilder, ILoggerFactory?)</c> / <c>Register(IServiceCollection, ILoggerFactory?)</c>
/// / <c>Initialize(IServiceProvider, ILoggerFactory?)</c> shape before this generator ever runs — the
/// <c>Fdw.ServiceTypes.Analyzers.ServiceTypeCollectionPhaseMethodsAnalyzer</c> (FDW024) enforces it as a
/// build ERROR on every <c>[ServiceTypeCollection]</c>-decorated class, so this generator emits the
/// registration for every discovered class unconditionally.
/// </para>
/// <para>
/// This generator's ONLY job is collecting each discovered domain's Configure/Register/Initialize METHOD
/// GROUPS into a <c>ServiceTypeCollectionDescriptor</c> and wiring that into <c>PlatformServices.Add(...)</c>
/// — it never wraps, resolves a service, or otherwise alters what a domain's Initialize does. A domain
/// that needs something extra around its own Initialize (e.g. a boot-time elevation scope) does that
/// itself, via the domain's own <c>Initialization(customFunc)</c> override — never here, since baking a
/// domain-specific concern into every host's registration file forces it on hosts that never need it.
/// </para>
/// </remarks>
[Generator]
public sealed class PlatformServicesRegistrationGenerator : IIncrementalGenerator
{
    private const string ServiceTypeCollectionAttributeName = "Fdw.Collections.ServiceTypeCollectionAttribute";
    private const string PlatformServiceProviderAttributeName = "Fdw.Collections.PlatformServiceProviderAttribute";
    private const string PlatformServicesTypeName = "Fdw.ServiceTypes.PlatformServices";

    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(context.CompilationProvider, Execute);
    }

    private static void Execute(SourceProductionContext context, Compilation compilation)
    {
        if (compilation.Options.OutputKind == OutputKind.DynamicallyLinkedLibrary)
            return;

        var serviceTypeCollectionAttributeSymbol = compilation.GetTypeByMetadataName(ServiceTypeCollectionAttributeName);
        var platformServiceProviderAttributeSymbol = compilation.GetTypeByMetadataName(PlatformServiceProviderAttributeName);
        if (serviceTypeCollectionAttributeSymbol is null && platformServiceProviderAttributeSymbol is null)
            return;

        var platformServicesSymbol = compilation.GetTypeByMetadataName(PlatformServicesTypeName);
        if (platformServicesSymbol is null)
            return;

        var discovered = new List<INamedTypeSymbol>();
        foreach (var reference in compilation.References)
        {
            if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol assemblySymbol)
                continue;

            var assemblyName = assemblySymbol.Name;
            if (assemblyName.StartsWith("System", StringComparison.Ordinal)
                || assemblyName.StartsWith("Microsoft", StringComparison.Ordinal)
                || assemblyName.StartsWith("netstandard", StringComparison.Ordinal)
                || assemblyName.StartsWith("mscorlib", StringComparison.Ordinal))
            {
                continue;
            }

            ScanNamespace(assemblySymbol.GlobalNamespace, serviceTypeCollectionAttributeSymbol, platformServiceProviderAttributeSymbol, discovered);
        }

        // Also scan the current compilation's own namespace tree so single-assembly entry-points catch
        // collections defined alongside Program.cs.
        ScanNamespace(compilation.GlobalNamespace, serviceTypeCollectionAttributeSymbol, platformServiceProviderAttributeSymbol, discovered);

        var uniqueTypes = discovered
            .GroupBy(t => t, SymbolEqualityComparer.Default)
            .Select(g => (INamedTypeSymbol)g.Key!)
            .ToList();

        var models = uniqueTypes
            .Select(type => BuildModel(type, serviceTypeCollectionAttributeSymbol, platformServiceProviderAttributeSymbol))
            .ToList();

        var ordered = models.OrderBy(m => m.CategoryName, StringComparer.Ordinal).ToList();

        var assemblySafe = (compilation.AssemblyName ?? "Generated").Replace(".", "_").Replace("-", "_");
        context.AddSource(
            "PlatformServicesRegistration.g.cs",
            SourceText.From(GenerateSource(ordered, assemblySafe), Encoding.UTF8));
    }

    private static void ScanNamespace(
        INamespaceSymbol ns,
        INamedTypeSymbol? serviceTypeCollectionAttributeSymbol,
        INamedTypeSymbol? platformServiceProviderAttributeSymbol,
        List<INamedTypeSymbol> discovered)
    {
        foreach (var type in ns.GetTypeMembers())
            ScanType(type, serviceTypeCollectionAttributeSymbol, platformServiceProviderAttributeSymbol, discovered);

        foreach (var nestedNs in ns.GetNamespaceMembers())
            ScanNamespace(nestedNs, serviceTypeCollectionAttributeSymbol, platformServiceProviderAttributeSymbol, discovered);
    }

    private static void ScanType(
        INamedTypeSymbol type,
        INamedTypeSymbol? serviceTypeCollectionAttributeSymbol,
        INamedTypeSymbol? platformServiceProviderAttributeSymbol,
        List<INamedTypeSymbol> discovered)
    {
        foreach (var nested in type.GetTypeMembers())
            ScanType(nested, serviceTypeCollectionAttributeSymbol, platformServiceProviderAttributeSymbol, discovered);

        var attributes = type.GetAttributes();
        var hasServiceTypeCollectionAttribute = serviceTypeCollectionAttributeSymbol != null
            && attributes.Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, serviceTypeCollectionAttributeSymbol));
        var hasPlatformServiceProviderAttribute = platformServiceProviderAttributeSymbol != null
            && attributes.Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, platformServiceProviderAttributeSymbol));

        if (!hasServiceTypeCollectionAttribute && !hasPlatformServiceProviderAttribute)
            return;

        if (type.IsGenericType || type.TypeParameters.Length > 0)
            return;
        if (hasServiceTypeCollectionAttribute && type.IsAbstract)
            return;

        discovered.Add(type);
    }

    private static CollectionModel BuildModel(
        INamedTypeSymbol type,
        INamedTypeSymbol? serviceTypeCollectionAttributeSymbol,
        INamedTypeSymbol? platformServiceProviderAttributeSymbol)
    {
        var attribute = (serviceTypeCollectionAttributeSymbol != null
                ? type.GetAttributes().FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, serviceTypeCollectionAttributeSymbol))
                : null)
            ?? (platformServiceProviderAttributeSymbol != null
                ? type.GetAttributes().FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, platformServiceProviderAttributeSymbol))
                : null)
            ?? throw new InvalidOperationException($"'{type.Name}' was discovered without either PlatformServices marker attribute.");

        var categoryArg = attribute.NamedArguments
            .FirstOrDefault(kvp => string.Equals(kvp.Key, "ServiceCategory", StringComparison.Ordinal)).Value;
        var categoryName = categoryArg.Value as string;
        if (string.IsNullOrEmpty(categoryName))
        {
            var className = type.Name;
            if (className.EndsWith("Types", StringComparison.Ordinal) && className.Length > "Types".Length)
                categoryName = className.Substring(0, className.Length - "Types".Length);
            else if (className.EndsWith("Provider", StringComparison.Ordinal) && className.Length > "Provider".Length)
                categoryName = className.Substring(0, className.Length - "Provider".Length);
            else
                categoryName = className;
        }

        return new CollectionModel(categoryName!, type.ToDisplayString());
    }

    private sealed record CollectionModel(string CategoryName, string CollectionFullName);

    private static string GenerateSource(List<CollectionModel> models, string assemblySafe)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Runtime.CompilerServices;");
        sb.AppendLine("using Fdw.ServiceTypes;");
        sb.AppendLine();
        sb.AppendLine($"namespace {assemblySafe}.Generated");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Registers every discovered ServiceTypeCollection into PlatformServices on assembly load, each into");
        sb.AppendLine("    /// its own field — the sole source of truth its dot-walked property reads from. No lookup of any kind");
        sb.AppendLine("    /// (no dictionary, no ByName) happens at read time. This class only WIRES each domain's own");
        sb.AppendLine("    /// Configure/Register/Initialize method groups into a descriptor — it never wraps or alters them.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    internal static class PlatformServicesRegistration");
        sb.AppendLine("    {");
        foreach (var m in models)
        {
            sb.AppendLine($"        internal static PlatformServiceEntry? _{FieldNameFor(m.CategoryName)};");
        }
        sb.AppendLine();
        sb.AppendLine("        [ModuleInitializer]");
        sb.AppendLine("        internal static void Initialize()");
        sb.AppendLine("        {");
        if (models.Count == 0)
        {
            sb.AppendLine("            // No [ServiceTypeCollection] decorated classes discovered.");
        }
        else
        {
            foreach (var m in models)
            {
                sb.AppendLine($"            _{FieldNameFor(m.CategoryName)} = PlatformServices.Add(");
                sb.AppendLine($"                \"{m.CategoryName}\",");
                sb.AppendLine($"                new ServiceTypeCollectionDescriptor(");
                sb.AppendLine($"                    \"{m.CategoryName}\",");
                sb.AppendLine($"                    typeof({m.CollectionFullName}),");
                sb.AppendLine($"                    {m.CollectionFullName}.Configure,");
                sb.AppendLine($"                    {m.CollectionFullName}.Register,");
                sb.AppendLine($"                    {m.CollectionFullName}.Initialize));");
            }
        }
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>C# 14 extension members exposing per-category dot-walkable properties on PlatformServices.</summary>");
        sb.AppendLine("    public static class PlatformServicesExtensions");
        sb.AppendLine("    {");
        sb.AppendLine("        extension(PlatformServices)");
        sb.AppendLine("        {");
        foreach (var m in models)
        {
            sb.AppendLine($"            /// <summary>The registered entry for the <c>{m.CategoryName}</c> ServiceTypeCollection.</summary>");
            sb.AppendLine($"            public static PlatformServiceEntry {m.CategoryName}");
            sb.AppendLine($"                => PlatformServicesRegistration._{FieldNameFor(m.CategoryName)}");
            sb.AppendLine($"                   ?? throw new InvalidOperationException(");
            sb.AppendLine($"                       \"PlatformServices.{m.CategoryName} was read before its [ModuleInitializer] registered it.\");");
        }
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string FieldNameFor(string categoryName)
        => categoryName.Length == 0
            ? "entry"
            : char.ToLowerInvariant(categoryName[0]) + categoryName.Substring(1) + "Entry";
}
