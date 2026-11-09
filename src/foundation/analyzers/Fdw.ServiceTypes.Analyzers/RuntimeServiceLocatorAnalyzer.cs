using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Fdw.ServiceTypes.Analyzers;

/// <summary>
/// FDW061 (FDW-4641 B, FDW-4618 rule 6): no runtime service locator. A field, property or parameter of type
/// <c>IServiceProvider</c> or <c>IServiceScopeFactory</c>, a call that resolves or opens a scope through one
/// (<c>GetService</c>, <c>GetRequiredService</c>, <c>GetServices</c>, the keyed forms, <c>CreateScope</c>,
/// <c>CreateAsyncScope</c>), a read of <c>HttpContext.RequestServices</c>, and FastEndpoints' <c>Resolve</c> and
/// <c>TryResolve</c> are refused. Generalizes FDW045 to every class.
/// </summary>
/// <remarks>
/// <para>Allowed, because the platform hands them a container on purpose:</para>
/// <list type="bullet">
/// <item>a class deriving from <c>BackgroundService</c> or implementing <c>IHostedService</c> (one scope per run);</item>
/// <item>a registration body: any method, local function or lambda with a parameter of type
/// <c>IHostApplicationBuilder</c>, <c>IServiceCollection</c> or <c>IHost</c> (the Configure, Register and Initialize
/// phase signatures, and helpers they call with <c>builder.Services</c>);</item>
/// <item>any lambda with an <c>IServiceProvider</c> parameter (the <c>sp =&gt; new X(sp.GetRequiredService&lt;IY&gt;())</c>
/// registration shape);</item>
/// <item>a class, member or parameter marked <c>[ServiceLocatorAllowed("reason")]</c> with a non-empty reason
/// (<c>IHealthCheckable.CheckHealth</c>, the FDW-4639 scope runner). A mark with an empty reason is itself reported and
/// allows nothing.</item>
/// </list>
/// <para>The rule ships disabled. A repository turns it on with <c>dotnet_diagnostic.FDW061.severity = error</c> and
/// switches it off per folder until that folder's domain story lands.</para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RuntimeServiceLocatorAnalyzer : DiagnosticAnalyzer
{
    /// <summary>Diagnostic ID for a runtime service locator.</summary>
    public const string DiagnosticId = "FDW061";

    private const string ServiceProviderMetadataName = "System.IServiceProvider";
    private const string ServiceScopeFactoryMetadataName = "Microsoft.Extensions.DependencyInjection.IServiceScopeFactory";
    private const string ServiceCollectionMetadataName = "Microsoft.Extensions.DependencyInjection.IServiceCollection";
    private const string HostApplicationBuilderMetadataName = "Microsoft.Extensions.Hosting.IHostApplicationBuilder";
    private const string HostMetadataName = "Microsoft.Extensions.Hosting.IHost";
    private const string BackgroundServiceMetadataName = "Microsoft.Extensions.Hosting.BackgroundService";
    private const string HostedServiceMetadataName = "Microsoft.Extensions.Hosting.IHostedService";
    private const string AllowedAttributeMetadataName = "Fdw.Services.Abstractions.ServiceLocatorAllowedAttribute";
    private const string HttpContextFullName = "Microsoft.AspNetCore.Http.HttpContext";
    private const string RequestServicesPropertyName = "RequestServices";
    private const string FastEndpointsNamespace = "FastEndpoints";

    private static readonly string[] LocatorMethodContainingTypes =
    [
        "System.IServiceProvider",
        "Microsoft.Extensions.DependencyInjection.IServiceScopeFactory",
        "Microsoft.Extensions.DependencyInjection.IKeyedServiceProvider",
        "Microsoft.Extensions.DependencyInjection.ISupportRequiredService",
        "Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions",
        "Microsoft.Extensions.DependencyInjection.ServiceProviderKeyedServiceExtensions",
    ];

    private static readonly string[] FastEndpointsResolveMethods = ["Resolve", "TryResolve"];

    private static readonly LocalizableString Title = "No runtime service locator";
    private static readonly LocalizableString MessageFormat =
        "'{0}' uses a runtime service locator: {1}. Take the domain's provider interface instead; only a hosted worker, a registration body, an sp => lambda or a member marked [ServiceLocatorAllowed(\"reason\")] may use one.";
    private static readonly LocalizableString Description =
        "A class gets a service from its domain provider, never from IServiceProvider, IServiceScopeFactory or HttpContext.RequestServices at run time (FDW-4618 rule 6). A singleton opening a scope per call is this rule in disguise.";

    private static readonly LocalizableString EmptyReasonTitle = "[ServiceLocatorAllowed] needs a reason";
    private static readonly LocalizableString EmptyReasonMessageFormat =
        "[ServiceLocatorAllowed] on '{0}' has an empty reason. State why a runtime service locator is needed here; a mark without a reason allows nothing.";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        Title,
        MessageFormat,
        "Design",
        DiagnosticSeverity.Error,
        isEnabledByDefault: false,
        description: Description);

    private static readonly DiagnosticDescriptor EmptyReasonRule = new(
        DiagnosticId,
        EmptyReasonTitle,
        EmptyReasonMessageFormat,
        "Design",
        DiagnosticSeverity.Error,
        isEnabledByDefault: false,
        description: Description);

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule, EmptyReasonRule];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var compilation = compilationContext.Compilation;
            var serviceProvider = compilation.GetTypeByMetadataName(ServiceProviderMetadataName);
            if (serviceProvider == null)
                return;

            var known = new KnownTypes(
                Types(compilation, ServiceProviderMetadataName, ServiceScopeFactoryMetadataName),
                Types(compilation, HostApplicationBuilderMetadataName, ServiceCollectionMetadataName, HostMetadataName),
                Types(compilation, BackgroundServiceMetadataName, HostedServiceMetadataName),
                serviceProvider,
                compilation.GetTypeByMetadataName(AllowedAttributeMetadataName));

            compilationContext.RegisterSymbolAction(c => AnalyzeField(c, known), SymbolKind.Field);
            compilationContext.RegisterSymbolAction(c => AnalyzeProperty(c, known), SymbolKind.Property);
            compilationContext.RegisterSymbolAction(c => AnalyzeMethod(c, known), SymbolKind.Method);
            compilationContext.RegisterSymbolAction(c => ReportEmptyReasons(c, known), SymbolKind.NamedType, SymbolKind.Field, SymbolKind.Property);
            compilationContext.RegisterOperationAction(c => AnalyzeInvocation(c, known), OperationKind.Invocation);
            compilationContext.RegisterOperationAction(c => AnalyzePropertyReference(c, known), OperationKind.PropertyReference);
        });
    }

    private static ImmutableArray<INamedTypeSymbol> Types(Compilation compilation, params string[] metadataNames)
        => metadataNames
            .Select(compilation.GetTypeByMetadataName)
            .Where(t => t != null)
            .Cast<INamedTypeSymbol>()
            .ToImmutableArray();

    private static void AnalyzeField(SymbolAnalysisContext context, KnownTypes known)
    {
        var field = (IFieldSymbol)context.Symbol;
        if (field.IsImplicitlyDeclared || !IsOrImplementsAny(field.Type, known.LocatorTypes) || IsExempt(field, known))
            return;

        Report(context, field.Locations, field.ContainingType, $"field '{field.Name}' of type {field.Type.Name}");
    }

    private static void AnalyzeProperty(SymbolAnalysisContext context, KnownTypes known)
    {
        var property = (IPropertySymbol)context.Symbol;
        if (property.IsImplicitlyDeclared || !IsOrImplementsAny(property.Type, known.LocatorTypes) || IsExempt(property, known))
            return;

        Report(context, property.Locations, property.ContainingType, $"property '{property.Name}' of type {property.Type.Name}");
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context, KnownTypes known)
    {
        var method = (IMethodSymbol)context.Symbol;
        if (method.IsImplicitlyDeclared || method.MethodKind is MethodKind.PropertyGet or MethodKind.PropertySet or MethodKind.DelegateInvoke)
            return;

        foreach (var parameter in method.Parameters)
        {
            ReportEmptyReason(context, parameter, known);

            if (!IsOrImplementsAny(parameter.Type, known.LocatorTypes) || HasAllowedMark(parameter, known) || IsExempt(method, known))
                continue;

            Report(context, parameter.Locations, method.ContainingType, $"parameter '{parameter.Name}' of type {parameter.Type.Name}");
        }

        ReportEmptyReason(context, method, known);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, KnownTypes known)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var method = invocation.TargetMethod;

        var isLocator = LocatorMethodContainingTypes.Contains(FullName(method.ContainingType), StringComparer.Ordinal)
            || (IsInNamespace(method.ContainingType, FastEndpointsNamespace)
                && FastEndpointsResolveMethods.Contains(method.Name, StringComparer.Ordinal));

        if (!isLocator || IsExempt(invocation, context.ContainingSymbol, known))
            return;

        Report(context, invocation.Syntax.GetLocation(), context.ContainingSymbol, $"call to {method.Name}");
    }

    private static void AnalyzePropertyReference(OperationAnalysisContext context, KnownTypes known)
    {
        var reference = (IPropertyReferenceOperation)context.Operation;
        if (!string.Equals(reference.Property.Name, RequestServicesPropertyName, StringComparison.Ordinal)
            || !DerivesFromFullName(reference.Property.ContainingType, HttpContextFullName)
            || IsExempt(reference, context.ContainingSymbol, known))
            return;

        Report(context, reference.Syntax.GetLocation(), context.ContainingSymbol, "read of HttpContext.RequestServices");
    }

    /// <summary>An operation is exempt inside an sp lambda or registration body, or when its containing symbols are.</summary>
    private static bool IsExempt(IOperation operation, ISymbol containingSymbol, KnownTypes known)
    {
        for (var current = operation.Parent; current != null; current = current.Parent)
        {
            var function = current switch
            {
                IAnonymousFunctionOperation anonymous => anonymous.Symbol,
                ILocalFunctionOperation local => local.Symbol,
                _ => null,
            };

            if (function == null)
                continue;

            if (function.Parameters.Any(p => IsOrImplementsAny(p.Type, known.RegistrationTypes) || IsOrImplements(p.Type, known.ServiceProvider)))
                return true;

            if (HasAllowedMark(function, known))
                return true;
        }

        return IsExempt(containingSymbol, known);
    }

    /// <summary>A symbol is exempt when it or a symbol containing it is a registration body, a hosted worker or marked.</summary>
    private static bool IsExempt(ISymbol symbol, KnownTypes known)
    {
        for (var current = symbol; current != null && current is not INamespaceSymbol; current = current.ContainingSymbol)
        {
            if (HasAllowedMark(current, known))
                return true;

            switch (current)
            {
                case IMethodSymbol method:
                    if (method.Parameters.Any(p => IsOrImplementsAny(p.Type, known.RegistrationTypes)))
                        return true;
                    if (method.AssociatedSymbol != null && HasAllowedMark(method.AssociatedSymbol, known))
                        return true;
                    break;
                case INamedTypeSymbol type when IsHostedWorker(type, known):
                    return true;
            }
        }

        return false;
    }

    private static bool IsHostedWorker(INamedTypeSymbol type, KnownTypes known)
    {
        foreach (var hosted in known.HostedTypes)
        {
            if (IsOrImplements(type, hosted))
                return true;

            for (var current = type.BaseType; current != null; current = current.BaseType)
            {
                if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, hosted))
                    return true;
            }
        }

        return false;
    }

    private static bool HasAllowedMark(ISymbol symbol, KnownTypes known)
        => AllowedMarks(symbol, known).Any(a => !string.IsNullOrWhiteSpace(Reason(a)));

    private static ImmutableArray<AttributeData> AllowedMarks(ISymbol symbol, KnownTypes known)
        => known.AllowedAttribute == null
            ? []
            : symbol.GetAttributes()
                .Where(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, known.AllowedAttribute))
                .ToImmutableArray();

    private static string? Reason(AttributeData attribute)
        => attribute.ConstructorArguments.Length == 1 ? attribute.ConstructorArguments[0].Value as string : null;

    private static void ReportEmptyReasons(SymbolAnalysisContext context, KnownTypes known)
        => ReportEmptyReason(context, context.Symbol, known);

    private static void ReportEmptyReason(SymbolAnalysisContext context, ISymbol symbol, KnownTypes known)
    {
        foreach (var attribute in AllowedMarks(symbol, known).Where(a => string.IsNullOrWhiteSpace(Reason(a))))
        {
            var location = attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation();
            if (location != null)
                context.ReportDiagnostic(Diagnostic.Create(EmptyReasonRule, location, symbol.Name));
        }
    }

    private static void Report(SymbolAnalysisContext context, ImmutableArray<Location> locations, INamedTypeSymbol owner, string what)
    {
        var location = locations.FirstOrDefault(l => l.IsInSource);
        if (location != null)
            context.ReportDiagnostic(Diagnostic.Create(Rule, location, owner.Name, what));
    }

    private static void Report(OperationAnalysisContext context, Location location, ISymbol containingSymbol, string what)
        => context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            location,
            containingSymbol.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat),
            what));

    private static bool IsOrImplementsAny(ITypeSymbol type, ImmutableArray<INamedTypeSymbol> candidates)
        => candidates.Any(candidate => IsOrImplements(type, candidate));

    private static bool IsOrImplements(ITypeSymbol type, INamedTypeSymbol candidate)
        => SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, candidate)
            || type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, candidate));

    private static bool DerivesFromFullName(INamedTypeSymbol? type, string fullName)
    {
        for (var current = type; current != null; current = current.BaseType)
        {
            if (string.Equals(FullName(current), fullName, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static bool IsInNamespace(INamedTypeSymbol type, string ns)
        => type.ContainingNamespace != null
            && string.Equals(type.ContainingNamespace.ToDisplayString(), ns, StringComparison.Ordinal);

    private static string FullName(INamedTypeSymbol type)
    {
        var definition = type.OriginalDefinition;
        var ns = definition.ContainingNamespace;
        return ns == null || ns.IsGlobalNamespace
            ? definition.MetadataName
            : ns.ToDisplayString() + "." + definition.MetadataName;
    }

    private sealed class KnownTypes(
        ImmutableArray<INamedTypeSymbol> locatorTypes,
        ImmutableArray<INamedTypeSymbol> registrationTypes,
        ImmutableArray<INamedTypeSymbol> hostedTypes,
        INamedTypeSymbol serviceProvider,
        INamedTypeSymbol? allowedAttribute)
    {
        public ImmutableArray<INamedTypeSymbol> LocatorTypes { get; } = locatorTypes;

        public ImmutableArray<INamedTypeSymbol> RegistrationTypes { get; } = registrationTypes;

        public ImmutableArray<INamedTypeSymbol> HostedTypes { get; } = hostedTypes;

        public INamedTypeSymbol ServiceProvider { get; } = serviceProvider;

        public INamedTypeSymbol? AllowedAttribute { get; } = allowedAttribute;
    }
}
