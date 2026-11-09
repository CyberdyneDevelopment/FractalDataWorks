using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Fdw.ServiceTypes.Analyzers;

/// <summary>
/// FDW062 (FDW-4641 C, FDW-4618 rule 5): a registration body registers through <c>builder.Services</c> and hands the
/// domain provider a <c>Func</c>. It never builds a second container (<c>BuildServiceProvider</c>), never edits
/// another component's registration (<c>Remove</c>, <c>RemoveAt</c>, <c>RemoveAll</c>, <c>Replace</c>,
/// <c>Clear</c>, <c>Insert</c> on an <c>IServiceCollection</c>, or reading a descriptor's
/// <c>ImplementationFactory</c>, <c>ImplementationInstance</c> or <c>ImplementationType</c>), and no code declares an
/// <c>Add*</c> extension method on <c>IServiceCollection</c> or <c>IHostApplicationBuilder</c> (the DI ownership
/// rule: registration is the ServiceTypeCollection three-phase lifecycle only).
/// </summary>
/// <remarks>
/// These shapes are refused wherever they appear, since a registration body is the only place they could occur.
/// The rule ships disabled. A repository turns it on with <c>dotnet_diagnostic.FDW062.severity = error</c> and
/// switches it off per folder until that folder's domain story lands.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RegistrationSurgeryAnalyzer : DiagnosticAnalyzer
{
    /// <summary>Diagnostic ID for registration-body surgery or an AddXxx extension.</summary>
    public const string DiagnosticId = "FDW062";

    private const string ServiceCollectionMetadataName = "Microsoft.Extensions.DependencyInjection.IServiceCollection";
    private const string HostApplicationBuilderMetadataName = "Microsoft.Extensions.Hosting.IHostApplicationBuilder";
    private const string ServiceDescriptorFullName = "Microsoft.Extensions.DependencyInjection.ServiceDescriptor";
    private const string ContainerBuilderExtensionsFullName = "Microsoft.Extensions.DependencyInjection.ServiceCollectionContainerBuilderExtensions";
    private const string BuildServiceProviderName = "BuildServiceProvider";
    private const string AddPrefix = "Add";

    private static readonly string[] CollectionEditMethods = ["Remove", "RemoveAt", "RemoveAll", "RemoveAllKeyed", "Replace", "Clear", "Insert"];

    private static readonly string[] DescriptorImplementationProperties =
    [
        "ImplementationFactory",
        "ImplementationInstance",
        "ImplementationType",
        "KeyedImplementationFactory",
        "KeyedImplementationInstance",
        "KeyedImplementationType",
    ];

    private static readonly LocalizableString Title =
        "A registration body registers; it does not build a container, edit another registration or declare an AddXxx extension";
    private static readonly LocalizableString MessageFormat =
        "'{0}' {1}. A registration body registers through builder.Services and hands the domain provider a Func; it never builds a second container, edits another component's registration, or declares an AddXxx extension.";
    private static readonly LocalizableString Description =
        "FDW-4618 rule 5: a Configure/Register/Initialize body may register through builder.Services and give the domain provider a Func (provider.Register(Name, () => sp.GetRequiredService<IXImplementationProvider>())). It may not call BuildServiceProvider, remove or replace another component's descriptor, or declare an AddXxx extension for a service (DI ownership rule).";

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
            var serviceCollection = compilationContext.Compilation.GetTypeByMetadataName(ServiceCollectionMetadataName);
            if (serviceCollection == null)
                return;

            var extensionTargets = new[] { serviceCollection, compilationContext.Compilation.GetTypeByMetadataName(HostApplicationBuilderMetadataName) }
                .Where(t => t != null)
                .Cast<INamedTypeSymbol>()
                .ToImmutableArray();

            compilationContext.RegisterOperationAction(c => AnalyzeInvocation(c, serviceCollection), OperationKind.Invocation);
            compilationContext.RegisterOperationAction(AnalyzePropertyReference, OperationKind.PropertyReference);
            compilationContext.RegisterSymbolAction(c => AnalyzeMethod(c, extensionTargets), SymbolKind.Method);
        });
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, INamedTypeSymbol serviceCollection)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var method = invocation.TargetMethod;

        if (string.Equals(method.Name, BuildServiceProviderName, StringComparison.Ordinal)
            && string.Equals(FullName(method.ContainingType), ContainerBuilderExtensionsFullName, StringComparison.Ordinal))
        {
            Report(context, invocation.Syntax.GetLocation(), "calls BuildServiceProvider, which builds a second container");
            return;
        }

        if (!CollectionEditMethods.Contains(method.Name, StringComparer.Ordinal))
            return;

        var receiver = ReceiverType(invocation);
        if (receiver == null || !IsOrImplements(receiver, serviceCollection))
            return;

        Report(context, invocation.Syntax.GetLocation(), $"calls {method.Name} on the service collection, editing another component's registration");
    }

    private static void AnalyzePropertyReference(OperationAnalysisContext context)
    {
        var reference = (IPropertyReferenceOperation)context.Operation;
        var property = reference.Property;

        if (!DescriptorImplementationProperties.Contains(property.Name, StringComparer.Ordinal)
            || !string.Equals(FullName(property.ContainingType), ServiceDescriptorFullName, StringComparison.Ordinal))
            return;

        Report(context, reference.Syntax.GetLocation(), $"reads ServiceDescriptor.{property.Name}, reaching into another component's registration");
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context, ImmutableArray<INamedTypeSymbol> extensionTargets)
    {
        var method = (IMethodSymbol)context.Symbol;
        if (!method.IsExtensionMethod
            || !method.Name.StartsWith(AddPrefix, StringComparison.Ordinal)
            || method.Parameters.Length == 0
            || !extensionTargets.Any(target => IsOrImplements(method.Parameters[0].Type, target)))
            return;

        var location = method.Locations.FirstOrDefault(l => l.IsInSource);
        if (location == null)
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            location,
            method.ContainingType.Name,
            $"declares the extension '{method.Name}' on {method.Parameters[0].Type.Name}"));
    }

    /// <summary>The type of what the method is called on: the instance, or an extension method's this argument.</summary>
    private static ITypeSymbol? ReceiverType(IInvocationOperation invocation)
    {
        var receiver = invocation.TargetMethod.IsExtensionMethod && invocation.Arguments.Length > 0
            ? invocation.Arguments[0].Value
            : invocation.Instance;

        while (receiver is IConversionOperation conversion)
            receiver = conversion.Operand;

        return receiver?.Type;
    }

    private static void Report(OperationAnalysisContext context, Location location, string what)
        => context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            location,
            context.ContainingSymbol.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat),
            what));

    private static bool IsOrImplements(ITypeSymbol type, INamedTypeSymbol candidate)
        => SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, candidate)
            || type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, candidate));

    private static string FullName(INamedTypeSymbol type)
    {
        var definition = type.OriginalDefinition;
        var ns = definition.ContainingNamespace;
        return ns == null || ns.IsGlobalNamespace
            ? definition.MetadataName
            : ns.ToDisplayString() + "." + definition.MetadataName;
    }
}
