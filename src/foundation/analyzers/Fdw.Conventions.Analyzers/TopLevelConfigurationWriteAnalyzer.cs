using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Fdw.Conventions.Analyzers;

/// <summary>
/// FDW054: a top-level configuration is written only by its domain configuration provider. Reports constructing
/// a ConfigurationWriteCommand&lt;T&gt; (ConfigurationSaveCommand and the rest) whose T is a top-level configuration,
/// one implementing IImplementationConfiguration or IDomainConfiguration, outside a configuration provider type.
/// </summary>
/// <remarks>
/// Why the type and not the place: child records (DataSetSample*, DataSetFieldStandIn*,
/// FieldMappingTransformationConfiguration) implement neither interface, so endpoint helpers may keep writing them.
/// A top-level configuration needs the provider's Save()/Update(): version-on-write and cache invalidation. The
/// provider path builds the command with an open type parameter, so an open T is never reported (FDW-3579).
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TopLevelConfigurationWriteAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for a top-level configuration written outside its domain provider.
    /// </summary>
    public const string DiagnosticId = "FDW054";

    private const string WriteCommandName = "Fdw.Commands.Data.ConfigurationWriteCommand`1";

    private static readonly string[] TopLevelInterfaces =
    [
        "Fdw.Configuration.IImplementationConfiguration",
        "Fdw.Configuration.IDomainConfiguration",
    ];

    private static readonly string[] ProviderBases =
    [
        "Fdw.Services.Configuration.DomainConfigurationProviderBase`1",
        "Fdw.Services.Configuration.ImplementationProviderBase`2",
        "Fdw.Services.Configuration.ConfigurationCommandBase`1",
    ];

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Top-level configuration written outside its provider",
        "'{0}' is a top-level configuration: write it through its domain configuration provider's Save() or Update(), not a ConfigurationWriteCommand",
        "Design",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A top-level configuration (IImplementationConfiguration or IDomainConfiguration) is written only through its domain provider, which applies version-on-write and cache invalidation. ConfigurationSaveCommand<T> and the other ConfigurationWriteCommand<T> commands stay available for child records.");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(start =>
        {
            var writeCommand = start.Compilation.GetTypeByMetadataName(WriteCommandName);
            var topLevel = TopLevelInterfaces
                .Select(name => start.Compilation.GetTypeByMetadataName(name))
                .OfType<INamedTypeSymbol>()
                .ToImmutableArray();
            if (writeCommand is null || topLevel.IsEmpty)
                return;

            var providers = ProviderBases
                .Select(name => start.Compilation.GetTypeByMetadataName(name))
                .OfType<INamedTypeSymbol>()
                .ToImmutableArray();

            start.RegisterOperationAction(
                operationContext => Analyze(operationContext, writeCommand, topLevel, providers),
                OperationKind.ObjectCreation);
        });
    }

    private static void Analyze(
        OperationAnalysisContext context,
        INamedTypeSymbol writeCommand,
        ImmutableArray<INamedTypeSymbol> topLevel,
        ImmutableArray<INamedTypeSymbol> providers)
    {
        var written = WrittenType(((IObjectCreationOperation)context.Operation).Type, writeCommand);
        if (written is not INamedTypeSymbol configuration ||
            !topLevel.Any(i => SymbolEqualityComparer.Default.Equals(configuration.OriginalDefinition, i) ||
                               configuration.AllInterfaces.Any(a => SymbolEqualityComparer.Default.Equals(a.OriginalDefinition, i))))
            return;

        for (var type = context.ContainingSymbol.ContainingType; type != null; type = type.ContainingType)
        {
            if (providers.Any(provider => DerivesFrom(type, provider)))
                return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, context.Operation.Syntax.GetLocation(), configuration.Name));
    }

    /// <summary>The T of the ConfigurationWriteCommand&lt;T&gt; that <paramref name="created"/> is or derives from.</summary>
    private static ITypeSymbol? WrittenType(ITypeSymbol? created, INamedTypeSymbol writeCommand)
    {
        for (var current = created as INamedTypeSymbol; current != null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, writeCommand))
                return current.TypeArguments[0];
        }

        return null;
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
}
