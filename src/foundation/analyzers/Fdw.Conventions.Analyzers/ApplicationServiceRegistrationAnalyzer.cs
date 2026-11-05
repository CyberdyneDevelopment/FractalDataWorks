using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Fdw.Conventions.Analyzers;

/// <summary>
/// FDW056: every service in the DI container belongs to FDW. In an application (an executable that is not a test
/// project) it reports a call to an IServiceCollection method or extension named Add* or TryAdd*, and the
/// declaration of an extension method named Add* whose first parameter is IServiceCollection or
/// IHostApplicationBuilder.
/// </summary>
/// <remarks>
/// <para>
/// An application is told apart by the compilation's output kind: FDW's packages are libraries, the hosts are
/// executables. A test project is an executable too under xunit v3, so a compilation whose
/// <c>build_property.IsTestProject</c> is <c>true</c> is not checked.
/// </para>
/// <para>
/// Registration belongs in an FDW package's ServiceTypeCollection, run by the three PlatformServices phases
/// (FDW-3582).
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ApplicationServiceRegistrationAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for service registration in an application.
    /// </summary>
    public const string DiagnosticId = "FDW056";

    private const string ServiceCollectionName = "Microsoft.Extensions.DependencyInjection.IServiceCollection";
    private const string HostApplicationBuilderName = "Microsoft.Extensions.Hosting.IHostApplicationBuilder";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Service registration in an application",
        "{0}: register services through an FDW package's ServiceTypeCollection and the PlatformServices phases, not in the application",
        "Design",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Every service registered in the DI container is FDW's. An application never calls services.Add*/TryAdd* and never declares its own Add* extension on IServiceCollection or IHostApplicationBuilder; it runs PlatformServices.Configure, Register and Initialize.");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(start =>
        {
            if (!IsApplication(start.Compilation.Options.OutputKind) || IsTestProject(start.Options.AnalyzerConfigOptionsProvider))
                return;

            var serviceCollection = start.Compilation.GetTypeByMetadataName(ServiceCollectionName);
            if (serviceCollection is null)
                return;

            var hostApplicationBuilder = start.Compilation.GetTypeByMetadataName(HostApplicationBuilderName);

            start.RegisterOperationAction(
                operationContext => AnalyzeInvocation(operationContext, serviceCollection),
                OperationKind.Invocation);

            start.RegisterSymbolAction(
                symbolContext => AnalyzeMethod(symbolContext, serviceCollection, hostApplicationBuilder),
                SymbolKind.Method);
        });
    }

    private static bool IsApplication(OutputKind outputKind) =>
        outputKind is OutputKind.ConsoleApplication or OutputKind.WindowsApplication or OutputKind.WindowsRuntimeApplication;

    private static bool IsTestProject(AnalyzerConfigOptionsProvider options) =>
        options.GlobalOptions.TryGetValue("build_property.IsTestProject", out var value) &&
        string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);

    private static bool IsRegistrationName(string name) =>
        name.StartsWith("Add", StringComparison.Ordinal) || name.StartsWith("TryAdd", StringComparison.Ordinal);

    private static void AnalyzeInvocation(OperationAnalysisContext context, INamedTypeSymbol serviceCollection)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var method = invocation.TargetMethod;
        if (!IsRegistrationName(method.Name))
            return;

        var receiver = method.IsExtensionMethod
            ? method.Parameters.FirstOrDefault()?.Type
            : invocation.Instance?.Type;

        if (receiver is null || !IsOrImplements(receiver, serviceCollection))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            invocation.Syntax.GetLocation(),
            $"'{method.Name}' registers a service in an application"));
    }

    private static void AnalyzeMethod(
        SymbolAnalysisContext context,
        INamedTypeSymbol serviceCollection,
        INamedTypeSymbol? hostApplicationBuilder)
    {
        var method = (IMethodSymbol)context.Symbol;
        if (!method.IsExtensionMethod || !method.Name.StartsWith("Add", StringComparison.Ordinal))
            return;

        var extended = method.Parameters[0].Type;
        if (!SymbolEqualityComparer.Default.Equals(extended, serviceCollection) &&
            !SymbolEqualityComparer.Default.Equals(extended, hostApplicationBuilder))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            method.Locations[0],
            $"'{method.Name}' is an Add* extension on {extended.Name} declared in an application"));
    }

    private static bool IsOrImplements(ITypeSymbol type, INamedTypeSymbol serviceCollection) =>
        SymbolEqualityComparer.Default.Equals(type, serviceCollection) ||
        type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, serviceCollection));
}
