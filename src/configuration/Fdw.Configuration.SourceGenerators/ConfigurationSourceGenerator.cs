using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Fdw.Configuration.SourceGenerators.Analysis;
using Fdw.Configuration.SourceGenerators.EmbeddedSources;
using Fdw.Configuration.SourceGenerators.Generators;
using Fdw.Configuration.SourceGenerators.Models;

namespace Fdw.Configuration.SourceGenerators;

/// <summary>
/// Incremental source generator that creates DDL definitions, ConfigurationTypes collection,
/// and TypeCollection DDL from [ManagedConfiguration] attributes.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class ConfigurationSourceGenerator : IIncrementalGenerator
{
    private const string ManagedConfigurationAttribute = "Fdw.Configuration.ManagedConfigurationAttribute";

    /// <summary>
    /// Initializes the incremental generator.
    /// </summary>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // The attributes are emitted into every project that uses this generator, as internal types marked
        // [Embedded]. Why embedded: an internal type is still visible to an assembly the project grants
        // InternalsVisibleTo, and that assembly emits its own copy, so the two collide (CS0436). An embedded
        // type is never visible outside its own assembly, even through InternalsVisibleTo. Only this generator
        // reads the attributes, by metadata name, inside the compilation it runs in.
        // ConfigurationTypeOptionAttribute is not emitted: it lives in Fdw.Configuration.
        context.RegisterPostInitializationOutput(static ctx =>
        {
            ctx.AddEmbeddedAttributeDefinition();
            ctx.AddSource(ManagedConfigurationAttributeSource.FileName, ManagedConfigurationAttributeSource.Source);
            ctx.AddSource(ConfigurationOptionAttributeSource.FileName, ConfigurationOptionAttributeSource.Source);
            ctx.AddSource(DbTypeAttributeSource.FileName, DbTypeAttributeSource.Source);
        });

        // Find all classes with [ManagedConfiguration] attribute
        var configurationClasses = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                ManagedConfigurationAttribute,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (context, _) => ConfigurationAnalyzer.Analyze(context))
            .Where(static model => model != null)
            .Select(static (model, _) => model!);

        // Collect all configurations for batch processing
        var allConfigurations = configurationClasses.Collect();

        // Generate DDL for each configuration
        context.RegisterSourceOutput(
            allConfigurations,
            static (context, configs) =>
            {
                if (configs.IsDefaultOrEmpty)
                    return;

                try
                {
                    // Build parent/child graph for FK generation
                    var graph = ParentChildAnalyzer.BuildGraph(configs);

                    // Generate DDL for each configuration
                    foreach (var config in configs)
                    {
                        if (!config.GenerateDdl)
                            continue;

                        var ddlSource = DdlGenerator.Generate(config, graph);
                        context.AddSource($"{config.ClassName}.Ddl.g.cs", SourceText.From(ddlSource, Encoding.UTF8));
                    }
                }
                catch (Exception ex)
                {
                    ReportError(context, "CFG001", "DDL Generation Failed", ex.Message);
                }
            });


        // Generate TypeCollection DDL and registry
        context.RegisterSourceOutput(
            allConfigurations,
            static (context, configs) =>
            {
                if (configs.IsDefaultOrEmpty)
                    return;

                try
                {
                    // Collect unique TypeCollection references
                    var typeCollectionRefs = configs
                        .SelectMany(c => c.Properties)
                        .Where(p => p.TypeCollectionReference != null)
                        .Select(p => p.TypeCollectionReference!)
                        .GroupBy(tc => tc.TypeCollectionFullName, StringComparer.Ordinal)
                        .Select(g => g.First())
                        .ToImmutableArray();

                    if (typeCollectionRefs.IsEmpty)
                        return;

                    // Generate DDL for each TypeCollection
                    foreach (var tcRef in typeCollectionRefs)
                    {
                        var ddlSource = TypeCollectionDdlGenerator.Generate(tcRef);
                        context.AddSource(
                            $"{tcRef.TypeCollectionName}.TypeCollectionDdl.g.cs",
                            SourceText.From(ddlSource, Encoding.UTF8));
                    }

                    // Generate TypeCollectionDdlRegistry
                    var targetNamespace = ConfigurationTypesGenerator.DetermineTargetNamespace(configs);
                    var registrySource = TypeCollectionDdlGenerator.GenerateRegistry(typeCollectionRefs, targetNamespace);
                    context.AddSource(
                        "TypeCollectionDdlRegistry.g.cs",
                        SourceText.From(registrySource, Encoding.UTF8));
                }
                catch (Exception ex)
                {
                    ReportError(context, "CFG003", "TypeCollection DDL Generation Failed", ex.Message);
                }
            });
    }

    private static void ReportError(SourceProductionContext context, string id, string title, string message)
    {
        var diagnostic = Diagnostic.Create(
            new DiagnosticDescriptor(
                id,
                title,
                "{0}",
                "ConfigurationSourceGenerator",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true),
            Location.None,
            message);
        context.ReportDiagnostic(diagnostic);
    }
}
