using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Fdw.Conventions.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fdw.Conventions.Analyzers;

/// <summary>
/// Analyzer that detects implementation-specific types in Abstractions or base service assemblies.
/// Types whose name starts with a non-domain prefix (e.g., "Email" in a Notifications assembly)
/// should be in their own implementation package (e.g., *.Email).
///
/// FDW010 Info:   Any type with non-domain implementation prefix in an Abstractions/base assembly.
/// FDW010 Warning: TypeOption, Configuration, or Service type with non-domain implementation prefix.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MisplacedImplementationTypeAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for misplaced implementation type.
    /// </summary>
    public const string DiagnosticId = "FDW010";

    private const string Title = "Implementation-specific type in base assembly";
    private const string MessageFormat = "Type '{0}' has implementation prefix '{1}' — consider moving to a '{2}.{1}' assembly";
    private const string Description = "Types with implementation-specific name prefixes (Email, Teams, MsSql) should be in their own dedicated assembly, not in Abstractions or base service packages.";
    private const string Category = "Design";

    /// <summary>
    /// Diagnostic ID for misplaced implementation type at warning severity.
    /// </summary>
    public const string WarningDiagnosticId = "FDW011";

    private static readonly DiagnosticDescriptor InfoRule = new(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: Description,
        customTags: [WellKnownDiagnosticTags.CompilationEnd]);

    private static readonly DiagnosticDescriptor WarningRule = new(
        WarningDiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "TypeOption, Configuration, or Service types with implementation-specific name prefixes should be in their own dedicated assembly.",
        customTags: [WellKnownDiagnosticTags.CompilationEnd]);

    /// <inheritdoc/>
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [InfoRule, WarningRule];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var assemblyName = compilationContext.Compilation.AssemblyName ?? string.Empty;

            // Determine if this is a base/abstractions assembly and extract domain info
            var analysisContext = ClassifyAssembly(assemblyName);
            if (analysisContext == null)
                return;

            // Collect implementation prefixes from referenced "more derived" assemblies
            var derivedPrefixes = FindDerivedAssemblyPrefixes(
                compilationContext.Compilation, analysisContext.BaseAssemblyName);

            // Also detect prefix clusters from source types
            var sourceTypes = new List<(INamedTypeSymbol Symbol, string Prefix)>();

            compilationContext.RegisterSymbolAction(symbolContext =>
            {
                var typeSymbol = (INamedTypeSymbol)symbolContext.Symbol;

                // Only source-defined, top-level, non-implicit types
                if (typeSymbol.Locations.Length == 0 || !typeSymbol.Locations[0].IsInSource)
                    return;
                if (typeSymbol.ContainingType != null)
                    return;
                if (typeSymbol.IsImplicitlyDeclared)
                    return;

                var typeName = typeSymbol.Name;
                if (typeName.Length == 0)
                    return;

                // Strip leading 'I' for interfaces to get the real prefix
                var nameForPrefix = typeSymbol.TypeKind == TypeKind.Interface && typeName.Length > 1
                    && typeName[0] == 'I' && char.IsUpper(typeName[1])
                    ? typeName.Substring(1)
                    : typeName;

                var prefix = ExtractPascalCasePrefix(nameForPrefix);
                if (prefix.Length == 0)
                    return;

                // Skip domain-matching prefixes
                if (IsDomainPrefix(prefix, analysisContext.DomainNames))
                    return;

                // Skip generic naming patterns
                if (IsGenericPrefix(prefix))
                    return;

                lock (sourceTypes)
                {
                    sourceTypes.Add((typeSymbol, prefix));
                }
            }, SymbolKind.NamedType);

            compilationContext.RegisterCompilationEndAction(endContext =>
            {
                // Build prefix clusters
                var clusters = new Dictionary<string, List<INamedTypeSymbol>>(StringComparer.Ordinal);
                lock (sourceTypes)
                {
                    foreach (var (symbol, prefix) in sourceTypes)
                    {
                        if (!clusters.TryGetValue(prefix, out var list))
                        {
                            list = new List<INamedTypeSymbol>();
                            clusters[prefix] = list;
                        }
                        list.Add(symbol);
                    }
                }

                foreach (var cluster in clusters)
                {
                    var prefix = cluster.Key;
                    var types = cluster.Value;

                    // Flag if prefix matches a known derived assembly (any count)
                    // OR if there are 3+ types with this prefix (a strong cluster signal)
                    var isDerivedPrefix = derivedPrefixes.Contains(prefix);
                    if (!isDerivedPrefix && types.Count < 3)
                        continue;

                    foreach (var typeSymbol in types)
                    {
                        var isHighSeverity = IsServiceOrConfigOrTypeOption(typeSymbol);
                        var rule = isHighSeverity ? WarningRule : InfoRule;

                        foreach (var location in typeSymbol.Locations)
                        {
                            if (location.IsInSource)
                            {
                                var diagnostic = Diagnostic.Create(
                                    rule,
                                    location,
                                    typeSymbol.Name,
                                    prefix,
                                    analysisContext.BaseAssemblyName);

                                endContext.ReportDiagnostic(diagnostic);
                                break;
                            }
                        }
                    }
                }
            });
        });
    }

    private static AssemblyAnalysisContext? ClassifyAssembly(string assemblyName)
    {
        if (assemblyName.Length == 0)
            return null;

        var segments = assemblyName.Split('.');

        // Only apply to service domain assemblies: *.Services.{Domain}.Abstractions
        // or base service assemblies: *.Services.{Domain}
        // NOT to root framework packages like Fdw.Abstractions
        var servicesIndex = Array.IndexOf(segments, "Services");
        if (servicesIndex < 0)
            return null;

        // Check for *.Services.{Domain}.Abstractions pattern
        if (assemblyName.EndsWith(".Abstractions", StringComparison.Ordinal) &&
            servicesIndex < segments.Length - 2)
        {
            var baseAssemblyName = assemblyName.Substring(0, assemblyName.Length - ".Abstractions".Length);
            var domainNames = ExtractDomainNames(baseAssemblyName);
            return domainNames.Count > 0
                ? new AssemblyAnalysisContext(baseAssemblyName, domainNames)
                : null;
        }

        // Check for base service assembly pattern: *.Services.{Domain}
        // (but NOT *.Services.{Domain}.{Implementation} — those are implementation packages)
        if (servicesIndex >= 0 && servicesIndex == segments.Length - 2)
        {
            // Skip Fdw.Services.Abstractions — that's the root services package, not a domain
            var lastSegment = segments[segments.Length - 1];
            if (string.Equals(lastSegment, "Abstractions", StringComparison.Ordinal))
                return null;

            // This is a base service assembly like Fdw.Services.Notifications
            var domainNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                lastSegment // "Notifications"
            };

            // Also add singular form
            var domain = segments[segments.Length - 1];
            if (domain.EndsWith("s", StringComparison.Ordinal) && domain.Length > 1)
            {
                domainNames.Add(domain.Substring(0, domain.Length - 1)); // "Notification"
            }

            return new AssemblyAnalysisContext(assemblyName, domainNames);
        }

        return null;
    }

    private static HashSet<string> ExtractDomainNames(string baseAssemblyName)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var segments = baseAssemblyName.Split('.');
        if (segments.Length == 0)
            return names;

        // Last segment is the domain (e.g., "Notifications" from "Fdw.Services.Notifications")
        var domain = segments[segments.Length - 1];
        names.Add(domain);

        // Also add singular form for pluralized domains
        if (domain.EndsWith("s", StringComparison.Ordinal) && domain.Length > 1)
        {
            names.Add(domain.Substring(0, domain.Length - 1));
        }

        // Add "Default" as it's common for base implementations
        names.Add("Default");

        return names;
    }

    private static HashSet<string> FindDerivedAssemblyPrefixes(
        Compilation compilation, string baseAssemblyName)
    {
        var prefixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var prefix = baseAssemblyName + ".";

        foreach (var reference in compilation.ReferencedAssemblyNames)
        {
            var refName = reference.Name;
            if (refName.StartsWith(prefix, StringComparison.Ordinal))
            {
                // Extract the implementation segment: "Email" from "*.Notifications.Email"
                var suffix = refName.Substring(prefix.Length);
                var dotIndex = suffix.IndexOf('.');
                var implName = dotIndex >= 0 ? suffix.Substring(0, dotIndex) : suffix;

                if (implName.Length > 0 &&
                    !string.Equals(implName, "Abstractions", StringComparison.Ordinal))
                {
                    prefixes.Add(implName);
                }
            }
        }

        return prefixes;
    }

    internal static string ExtractPascalCasePrefix(string name)
    {
        if (name.Length == 0)
            return string.Empty;

        // Find the end of the first PascalCase word
        // "EmailConfiguration" -> "Email"
        // "TeamsChannel" -> "Teams"
        // "MsSqlConnection" -> "Ms" -- too short, try next boundary
        // Handle common multi-word prefixes

        for (var i = 1; i < name.Length; i++)
        {
            if (char.IsUpper(name[i]))
            {
                var prefix = name.Substring(0, i);
                // Skip very short prefixes (1-2 chars) as they're likely abbreviation parts
                if (prefix.Length >= 3)
                    return prefix;
            }
        }

        // Entire name is one word
        return name;
    }

    private static bool IsDomainPrefix(string prefix, HashSet<string> domainNames)
    {
        // Exact match
        if (domainNames.Contains(prefix))
            return true;

        // Check if prefix is a leading substring of any domain name
        // e.g., "Secret" is a prefix of "SecretManager" → belongs to this domain
        foreach (var domain in domainNames)
        {
            if (domain.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static bool IsGenericPrefix(string prefix)
    {
        // Skip common generic prefixes that don't indicate implementation specificity
        return string.Equals(prefix, "Base", StringComparison.Ordinal)
            || string.Equals(prefix, "Abstract", StringComparison.Ordinal)
            || string.Equals(prefix, "Generic", StringComparison.Ordinal)
            || string.Equals(prefix, "Default", StringComparison.Ordinal)
            || string.Equals(prefix, "Internal", StringComparison.Ordinal)
            || string.Equals(prefix, "Invalid", StringComparison.Ordinal)
            || string.Equals(prefix, "Missing", StringComparison.Ordinal)
            || string.Equals(prefix, "Unknown", StringComparison.Ordinal)
            || string.Equals(prefix, "Unsupported", StringComparison.Ordinal);
    }

    private static bool IsServiceOrConfigOrTypeOption(INamedTypeSymbol typeSymbol)
    {
        var name = typeSymbol.Name;

        // Check name patterns for configuration and service types
        if (name.Contains("Configuration") || name.Contains("Service") || name.Contains("Factory"))
            return true;

        // Check for TypeOption/ServiceTypeOption attributes
        foreach (var attr in typeSymbol.GetAttributes())
        {
            var attrName = attr.AttributeClass?.Name ?? string.Empty;
            if (attrName.Contains("TypeOption") || attrName.Contains("ServiceTypeOption"))
                return true;
        }

        // Check base type chain for configuration/service/channel bases
        var baseType = typeSymbol.BaseType;
        while (baseType != null)
        {
            var baseName = baseType.Name;
            if (baseName.Contains("ConfigurationBase") ||
                baseName.Contains("ChannelBase") ||
                baseName.Contains("ServiceBase") ||
                baseName.Contains("TypeOptionBase"))
            {
                return true;
            }
            baseType = baseType.BaseType;
        }

        return false;
    }

    private sealed class AssemblyAnalysisContext
    {
        public AssemblyAnalysisContext(string baseAssemblyName, HashSet<string> domainNames)
        {
            BaseAssemblyName = baseAssemblyName;
            DomainNames = domainNames;
        }

        public string BaseAssemblyName { get; }
        public HashSet<string> DomainNames { get; }
    }
}
