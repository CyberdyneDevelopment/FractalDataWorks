using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fdw.ServiceTypes.Analyzers;

/// <summary>
/// Analyzer that ensures a service-type-option service (a class implementing an
/// <c>IServiceOption</c>-derived interface — i.e. the <c>ServiceInterface</c> of a
/// <c>[ServiceTypeCollection]</c>) never injects another such service directly through its
/// constructor. It must instead depend on the other service's
/// <c>IPlatformServiceProvider&lt;TService, TConfiguration&gt;</c> and resolve the concrete instance by
/// name at call time.
/// </summary>
/// <remarks>
/// Detection is purely semantic (symbol-based): a class is in scope only when its
/// <see cref="ITypeSymbol.AllInterfaces"/> contains <c>Fdw.Services.Abstractions.IServiceOption</c>
/// (this also catches interfaces that transitively extend a marked <c>ServiceInterface</c>, since
/// <c>AllInterfaces</c> flattens the entire interface graph). Each instance constructor parameter is
/// then checked the same way; a parameter typed as an <c>IServiceOption</c>-derived interface (or as
/// <c>IServiceOption</c> itself) is flagged UNLESS the parameter's type is one of the
/// <c>IPlatformServiceProvider</c> generic arities, which is the correct indirection shape.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ServiceProviderInjectionAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for a service-option service that injects another service-option service
    /// directly instead of injecting its <c>IPlatformServiceProvider&lt;TService, TConfiguration&gt;</c>.
    /// </summary>
    public const string DirectServiceInjectionDiagnosticId = "FDW044";

    private const string ServiceOptionMetadataName = "Fdw.Services.Abstractions.IServiceOption";
    private const string ServiceOptionDependencyAttributeMetadataName = "Fdw.Services.Abstractions.ServiceOptionDependencyAttribute";

    private static readonly string[] ServiceProviderMetadataNames =
    [
        "Fdw.ServiceTypes.IPlatformServiceProvider`1",
        "Fdw.ServiceTypes.IPlatformServiceProvider`2",
        "Fdw.ServiceTypes.IPlatformServiceProvider`4",
    ];

    private static readonly LocalizableString Title =
        "Service-option service must inject another service-option service through its provider";
    private static readonly LocalizableString MessageFormat =
        "'{0}' is a service-option service but injects the service '{1}' directly. Inject IPlatformServiceProvider<{1}, TConfiguration> and resolve it by name instead.";
    private static readonly LocalizableString Description =
        "A service-type-option service (a class implementing an IServiceOption-derived interface) must depend on another such service through its IPlatformServiceProvider<TService, TConfiguration>, resolving the instance by name — never by injecting the service interface or implementation directly through the constructor.";

    private const string Category = "Usage";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        DirectServiceInjectionDiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: Description);

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [Rule];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var serviceOptionType = compilationContext.Compilation.GetTypeByMetadataName(ServiceOptionMetadataName);
            if (serviceOptionType == null)
                return;

            var serviceProviderTypes = ServiceProviderMetadataNames
                .Select(compilationContext.Compilation.GetTypeByMetadataName)
                .Where(t => t != null)
                .Cast<INamedTypeSymbol>()
                .ToImmutableArray();

            var serviceOptionDependencyAttributeType =
                compilationContext.Compilation.GetTypeByMetadataName(ServiceOptionDependencyAttributeMetadataName);

            compilationContext.RegisterSymbolAction(
                symbolContext => AnalyzeNamedType(symbolContext, serviceOptionType, serviceProviderTypes, serviceOptionDependencyAttributeType),
                SymbolKind.NamedType);
        });
    }

    private static void AnalyzeNamedType(
        SymbolAnalysisContext context,
        INamedTypeSymbol serviceOptionType,
        ImmutableArray<INamedTypeSymbol> serviceProviderTypes,
        INamedTypeSymbol? serviceOptionDependencyAttributeType)
    {
        var classSymbol = (INamedTypeSymbol)context.Symbol;

        if (classSymbol.TypeKind != TypeKind.Class || !IsServiceOptionType(classSymbol, serviceOptionType))
            return;

        foreach (var constructor in classSymbol.GetMembers().OfType<IMethodSymbol>()
                     .Where(m => m.MethodKind == MethodKind.Constructor && !m.IsStatic))
        {
            foreach (var parameter in constructor.Parameters)
            {
                if (!IsServiceOptionType(parameter.Type, serviceOptionType))
                    continue;

                if (IsServiceProviderType(parameter.Type, serviceProviderTypes))
                    continue;

                if (HasServiceOptionDependencyAttribute(parameter, serviceOptionDependencyAttributeType))
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

    private static bool IsServiceOptionType(ITypeSymbol type, INamedTypeSymbol serviceOptionType)
        => SymbolEqualityComparer.Default.Equals(type, serviceOptionType)
            || type.AllInterfaces.Contains(serviceOptionType, SymbolEqualityComparer.Default);

    private static bool IsServiceProviderType(ITypeSymbol type, ImmutableArray<INamedTypeSymbol> serviceProviderTypes)
        => type is INamedTypeSymbol namedType
            && serviceProviderTypes.Contains(namedType.OriginalDefinition, SymbolEqualityComparer.Default);

    private static bool HasServiceOptionDependencyAttribute(IParameterSymbol parameter, INamedTypeSymbol? serviceOptionDependencyAttributeType)
        => serviceOptionDependencyAttributeType != null
            && parameter.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, serviceOptionDependencyAttributeType));
}
