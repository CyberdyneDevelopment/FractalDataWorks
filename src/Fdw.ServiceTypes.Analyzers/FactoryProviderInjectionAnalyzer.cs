using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fdw.ServiceTypes.Analyzers;

/// <summary>
/// Analyzer that keeps service factories PURE: a class implementing
/// <c>Fdw.Abstractions.IServiceFactory</c> must not take an <c>IPlatformServiceProvider</c> or an
/// <c>IServiceScopeFactory</c> as a constructor parameter. The owning provider resolves what the
/// service needs and hands the resolved value to a domain <c>Create</c> overload; a factory never
/// resolves its own dependencies.
/// </summary>
/// <remarks>
/// <para>
/// Two production defects motivate this rule, both a factory reaching for a scoped, provider-backed
/// service during construction:
/// </para>
/// <list type="number">
/// <item>A factory ctor-injecting its OWN collection's <c>IPlatformServiceProvider</c> re-enters that
/// provider's generated scoped resolver lambda during realization — unbounded recursion that MEDI's
/// StackGuard turns into a SILENT hang (no exception, no log) until the host is killed (FDW-615, FDW-560).</item>
/// <item>A singleton factory that cannot hold a scoped provider grabs one per-call via
/// <c>IServiceScopeFactory.CreateScope().ServiceProvider.GetService&lt;IPlatformServiceProvider&lt;...&gt;&gt;()</c>
/// and blocks on it — a raw-container service locator plus sync-over-async thread-pool-starvation freeze.</item>
/// </list>
/// <para>
/// A dependency wrapped in <c>Lazy&lt;IPlatformServiceProvider&lt;...&gt;&gt;</c> defers resolution past
/// construction and is NOT flagged — it cannot re-enter the resolver lambda. A direct (non-Lazy)
/// provider parameter, or any <c>IServiceScopeFactory</c>, is flagged.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class FactoryProviderInjectionAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for a service factory that injects a service provider or scope factory through
    /// its constructor instead of receiving already-resolved values from its owning provider.
    /// </summary>
    public const string FactoryProviderInjectionDiagnosticId = "FDW045";

    private const string ServiceFactoryMetadataName = "Fdw.Abstractions.IServiceFactory";
    private const string ScopeFactoryMetadataName = "Microsoft.Extensions.DependencyInjection.IServiceScopeFactory";
    private const string LazyMetadataName = "System.Lazy`1";

    private static readonly string[] ServiceProviderMetadataNames =
    [
        "Fdw.ServiceTypes.IPlatformServiceProvider`1",
        "Fdw.ServiceTypes.IPlatformServiceProvider`2",
        "Fdw.ServiceTypes.IPlatformServiceProvider`4",
    ];

    private static readonly LocalizableString Title =
        "Service factory must not inject a provider or scope factory";
    private static readonly LocalizableString MessageFormat =
        "Factory '{0}' injects '{1}' through its constructor. Factories are pure — the owning provider resolves dependencies and passes them to a Create overload. Injecting a provider risks resolver-lambda re-entrancy (silent hang); an IServiceScopeFactory is a raw-container service locator. Remove it (wrap in Lazy<T> only if unavoidable).";
    private static readonly LocalizableString Description =
        "A class implementing IServiceFactory must not take an IPlatformServiceProvider or IServiceScopeFactory as a constructor parameter. The provider owns async resolution and hands resolved values to the factory's Create overload. A non-Lazy provider dependency re-enters that provider's scoped resolver lambda during realization (unbounded silent recursion); an IServiceScopeFactory is used to locate a provider from the raw container and block on it (sync-over-async).";

    private const string Category = "Usage";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        FactoryProviderInjectionDiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Error,
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
            var serviceFactoryType = compilationContext.Compilation.GetTypeByMetadataName(ServiceFactoryMetadataName);
            if (serviceFactoryType == null)
                return;

            var forbiddenTypes = ServiceProviderMetadataNames
                .Append(ScopeFactoryMetadataName)
                .Select(compilationContext.Compilation.GetTypeByMetadataName)
                .Where(t => t != null)
                .Cast<INamedTypeSymbol>()
                .ToImmutableArray();
            if (forbiddenTypes.IsEmpty)
                return;

            var lazyType = compilationContext.Compilation.GetTypeByMetadataName(LazyMetadataName);

            compilationContext.RegisterSymbolAction(
                symbolContext => AnalyzeNamedType(symbolContext, serviceFactoryType, forbiddenTypes, lazyType),
                SymbolKind.NamedType);
        });
    }

    private static void AnalyzeNamedType(
        SymbolAnalysisContext context,
        INamedTypeSymbol serviceFactoryType,
        ImmutableArray<INamedTypeSymbol> forbiddenTypes,
        INamedTypeSymbol? lazyType)
    {
        var classSymbol = (INamedTypeSymbol)context.Symbol;

        if (classSymbol.TypeKind != TypeKind.Class
            || classSymbol.IsAbstract
            || !classSymbol.AllInterfaces.Contains(serviceFactoryType, SymbolEqualityComparer.Default))
            return;

        foreach (var constructor in classSymbol.GetMembers().OfType<IMethodSymbol>()
                     .Where(m => m.MethodKind == MethodKind.Constructor && !m.IsStatic))
        {
            foreach (var parameter in constructor.Parameters)
            {
                if (IsLazyWrapped(parameter.Type, lazyType))
                    continue;

                if (!IsForbiddenType(parameter.Type, forbiddenTypes))
                    continue;

                var location = parameter.Locations.FirstOrDefault();
                if (location == null)
                    continue;

                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    location,
                    classSymbol.Name,
                    parameter.Type.Name));
            }
        }
    }

    private static bool IsForbiddenType(ITypeSymbol type, ImmutableArray<INamedTypeSymbol> forbiddenTypes)
        => type is INamedTypeSymbol namedType
            && forbiddenTypes.Contains(namedType.OriginalDefinition, SymbolEqualityComparer.Default);

    private static bool IsLazyWrapped(ITypeSymbol type, INamedTypeSymbol? lazyType)
        => lazyType != null
            && type is INamedTypeSymbol namedType
            && SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, lazyType);
}
