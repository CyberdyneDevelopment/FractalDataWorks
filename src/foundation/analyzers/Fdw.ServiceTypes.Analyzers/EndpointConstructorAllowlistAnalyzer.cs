using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fdw.ServiceTypes.Analyzers;

/// <summary>
/// FDW060 (FDW-4641 A, FDW-4618 rules 1 and 4): an endpoint takes only its domain's provider interface, a logger and
/// the declared cross-cutting types through its constructor. A concrete class, a gateway, an
/// <c>IHttpClientFactory</c>, a service locator or any other service interface is refused: the endpoint takes the
/// domain's provider interface and calls one method on it.
/// </summary>
/// <remarks>
/// <para>An endpoint is any class whose base chain reaches <c>FastEndpoints.BaseEndpoint</c>, which covers
/// <c>Endpoint&lt;TRequest, TResponse&gt;</c>, <c>EndpointWithoutRequest</c> and every FDW endpoint base.</para>
/// <para>A parameter is allowed when its type is <c>ILogger</c> or <c>ILogger&lt;T&gt;</c>; an interface whose name
/// ends in <c>Provider</c>, except <c>IServiceProvider</c>, <c>IConfigurationGatewayProvider</c> and
/// <c>IDataGatewayProvider</c>; a type marked <c>[EndpointCrossCutting]</c>; or a type whose full name is in the
/// <c>fdw_endpoint_cross_cutting_types</c> .editorconfig key (comma-separated, generic types by metadata name such as
/// <c>Ns.IFoo`1</c>). That key is the one declared list of cross-cutting types Mike has not ruled on yet; when it is
/// absent nothing beyond the other three is allowed.</para>
/// <para>The rule ships disabled. A repository turns it on with
/// <c>dotnet_diagnostic.FDW060.severity = error</c> and switches it off per folder until that folder's domain story
/// lands.</para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EndpointConstructorAllowlistAnalyzer : DiagnosticAnalyzer
{
    /// <summary>Diagnostic ID for an endpoint constructor parameter outside the allowlist.</summary>
    public const string DiagnosticId = "FDW060";

    /// <summary>The .editorconfig key that names the cross-cutting types an endpoint may take.</summary>
    public const string CrossCuttingTypesOptionKey = "fdw_endpoint_cross_cutting_types";

    private const string BaseEndpointMetadataName = "FastEndpoints.BaseEndpoint";
    private const string CrossCuttingAttributeMetadataName = "Fdw.Services.Abstractions.EndpointCrossCuttingAttribute";
    private const string ProviderSuffix = "Provider";

    private static readonly string[] LoggerMetadataNames =
    [
        "Microsoft.Extensions.Logging.ILogger",
        "Microsoft.Extensions.Logging.ILogger`1",
    ];

    private static readonly string[] GatewayFullNames =
    [
        "Fdw.Services.Data.Abstractions.IDataGateway",
        "Fdw.Services.Data.Abstractions.IConfigurationGateway",
        "Fdw.Services.Data.Abstractions.IConfigurationGatewayProvider",
        "Fdw.Services.Data.Abstractions.IDataGatewayProvider",
    ];

    private static readonly string[] LocatorFullNames =
    [
        "System.IServiceProvider",
        "Microsoft.Extensions.DependencyInjection.IServiceScopeFactory",
    ];

    private const string HttpClientFactoryFullName = "System.Net.Http.IHttpClientFactory";

    private static readonly LocalizableString Title =
        "An endpoint takes only its domain's provider interface, a logger and the declared cross-cutting types";
    private static readonly LocalizableString MessageFormat =
        "Endpoint '{0}' takes '{1}' through parameter '{2}', which is {3}. Take the domain's provider interface and call one method on it.";
    private static readonly LocalizableString Description =
        "An endpoint is an HTTP orchestrator: validate, call one provider method, map, send (FDW-4618 rule 1). Its constructor takes ILogger, an interface whose name ends in Provider (not IServiceProvider, IConfigurationGatewayProvider or IDataGatewayProvider), a type marked [EndpointCrossCutting], or a type named in the fdw_endpoint_cross_cutting_types .editorconfig key. Never a concrete class (rule 4), a gateway, IHttpClientFactory, a service locator or another service interface.";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        Title,
        MessageFormat,
        "Design",
        DiagnosticSeverity.Error,
        isEnabledByDefault: false,
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
            var baseEndpoint = compilationContext.Compilation.GetTypeByMetadataName(BaseEndpointMetadataName);
            if (baseEndpoint == null)
                return;

            var loggers = LoggerMetadataNames
                .Select(compilationContext.Compilation.GetTypeByMetadataName)
                .Where(t => t != null)
                .Cast<INamedTypeSymbol>()
                .ToImmutableArray();

            var known = new KnownTypes(
                baseEndpoint,
                loggers,
                compilationContext.Compilation.GetTypeByMetadataName(CrossCuttingAttributeMetadataName));

            compilationContext.RegisterSymbolAction(symbolContext => AnalyzeNamedType(symbolContext, known), SymbolKind.NamedType);
        });
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context, KnownTypes known)
    {
        var classSymbol = (INamedTypeSymbol)context.Symbol;
        if (classSymbol.TypeKind != TypeKind.Class || !DerivesFrom(classSymbol, known.BaseEndpoint))
            return;

        var tree = classSymbol.Locations.FirstOrDefault(l => l.IsInSource)?.SourceTree;
        if (tree == null)
            return;

        var crossCutting = ReadCrossCuttingTypes(context.Options.AnalyzerConfigOptionsProvider.GetOptions(tree));

        foreach (var constructor in classSymbol.InstanceConstructors.Where(c => !c.IsImplicitlyDeclared))
        {
            foreach (var parameter in constructor.Parameters)
            {
                var reason = RefusalReason(parameter.Type, known, crossCutting);
                if (reason == null)
                    continue;

                var location = parameter.Locations.FirstOrDefault();
                if (location == null)
                    continue;

                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    location,
                    classSymbol.Name,
                    parameter.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                    parameter.Name,
                    reason));
            }
        }
    }

    /// <summary>Why the parameter type is refused, or null when it is allowed.</summary>
    private static string? RefusalReason(ITypeSymbol type, KnownTypes known, ImmutableHashSet<string> crossCutting)
    {
        var fullName = FullMetadataName(type);

        if (GatewayFullNames.Contains(fullName, StringComparer.Ordinal))
            return "a gateway (an endpoint holds no gateway)";

        if (LocatorFullNames.Contains(fullName, StringComparer.Ordinal))
            return "a service locator";

        if (string.Equals(fullName, HttpClientFactoryFullName, StringComparison.Ordinal))
            return "an ad hoc client factory";

        if (IsAllowed(type, fullName, known, crossCutting))
            return null;

        return type.TypeKind == TypeKind.Interface
            ? "a service interface, not a provider interface"
            : "a concrete type, not an interface";
    }

    private static bool IsAllowed(ITypeSymbol type, string fullName, KnownTypes known, ImmutableHashSet<string> crossCutting)
    {
        if (type is INamedTypeSymbol named && known.Loggers.Contains(named.OriginalDefinition, SymbolEqualityComparer.Default))
            return true;

        if (type.TypeKind == TypeKind.Interface && type.Name.EndsWith(ProviderSuffix, StringComparison.Ordinal))
            return true;

        if (crossCutting.Contains(fullName))
            return true;

        return known.CrossCuttingAttribute != null
            && type.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, known.CrossCuttingAttribute));
    }

    private static ImmutableHashSet<string> ReadCrossCuttingTypes(AnalyzerConfigOptions options)
    {
        if (!options.TryGetValue(CrossCuttingTypesOptionKey, out var value))
            return ImmutableHashSet<string>.Empty;

        return value
            .Split([','], StringSplitOptions.RemoveEmptyEntries)
            .Select(name => name.Trim())
            .Where(name => name.Length > 0)
            .ToImmutableHashSet(StringComparer.Ordinal);
    }

    private static string FullMetadataName(ITypeSymbol type)
    {
        var definition = type is INamedTypeSymbol named ? named.OriginalDefinition : type;
        var ns = definition.ContainingNamespace;
        return ns == null || ns.IsGlobalNamespace
            ? definition.MetadataName
            : ns.ToDisplayString() + "." + definition.MetadataName;
    }

    private static bool DerivesFrom(INamedTypeSymbol type, INamedTypeSymbol baseType)
    {
        for (var current = type.BaseType; current != null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, baseType))
                return true;
        }

        return false;
    }

    private sealed class KnownTypes(
        INamedTypeSymbol baseEndpoint,
        ImmutableArray<INamedTypeSymbol> loggers,
        INamedTypeSymbol? crossCuttingAttribute)
    {
        public INamedTypeSymbol BaseEndpoint { get; } = baseEndpoint;

        public ImmutableArray<INamedTypeSymbol> Loggers { get; } = loggers;

        public INamedTypeSymbol? CrossCuttingAttribute { get; } = crossCuttingAttribute;
    }
}
