using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Pipelines.Abstractions.Runs;
using Fdw.Services.Pipelines.Logging;
using Fdw.Services.Pipelines.Runs;
using Fdw.ServiceTypes;
using Fdw.ServiceTypes.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Pipelines;

/// <summary>
/// The Pipeline domain (FDW-6244): one service type collection whose options are the domain's implementations. A pipeline's domain row in pipe.Pipeline
/// names its implementation, and that name is an option's name here (today <c>Graph</c>); <see cref="IPipelineRuns"/> reads the option to learn the run mode
/// its runs are carried out in.
/// </summary>
/// <remarks>
/// <para>
/// Options are found by their <c>[Implementation(typeof(PipelineTypes), name)]</c>, in whichever assembly declares them; an implementation closes
/// <see cref="PipelineTypeBase{TService,TFactory,TConfiguration}"/> over its own pipeline and row, and every option is looked up through
/// <see cref="IPipelineType"/>. The domain itself registers the pipe.Pipeline configuration provider every implementation row hangs from, and the run
/// contract <see cref="IPipelineRuns"/>.
/// </para>
/// <para>
/// <see cref="PipelineServiceTypes"/> stays a separate collection in its own category ("PipelineService"): stored scripts, webhook triggers and the services a
/// host gives pipelines, none of which is a pipeline.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[ServiceTypeCollection(
    typeof(PipelineTypeBase<IPipeline, IServiceFactory<IPipeline, IPipelineImplementationConfiguration>, IPipelineImplementationConfiguration>),
    typeof(IPipelineType),
    typeof(PipelineTypes),
    ServiceInterface = typeof(IPipeline),
    ServiceCategory = "Pipeline")]
public partial class PipelineTypes : ServiceTypeCollectionBase<
    PipelineTypeBase<IPipeline, IServiceFactory<IPipeline, IPipelineImplementationConfiguration>, IPipelineImplementationConfiguration>,
    IPipelineType>
{
    /// <summary>
    /// The connection this domain's configuration rows are read from and written to.
    /// </summary>
    public static string ConfigurationConnection => PlatformConnection.Name;

    /// <summary>
    /// The connection this domain's operational rows live in (the runs, their requests and their trigger keys). The host must set it; there is no default.
    /// </summary>
    /// <remarks>
    /// Deliberately without an initializer, unlike <see cref="ConfigurationConnection"/>. That one may default because <c>PlatformConfiguration</c> is
    /// declared in <c>configurationSchema.json</c> and is therefore known before any row is read. An operational store is a row INSIDE that store, so a
    /// default here would name a store the application merely hopes exists. Not proven at registration: every host registers this domain as part of the
    /// platform's registration, including hosts that never touch its operational store, so refusing to start them would demand a value a correct host
    /// cannot supply. The sites that read it report it instead.
    /// </remarks>
    public static string? OperationalConnection { get; set; }

    /// <summary>
    /// Sets this collection's Register body: the domain's configuration provider and run contract, then the option collect.
    /// </summary>
    /// <remarks>
    /// The configuration provider is registered first because the options' own registrations read it (an implementation registers its row's provider into
    /// it at Initialize). Setting it as the phase's body is what makes it replaceable: an application calling <c>Registration(...)</c> replaces the collect
    /// and these registrations together.
    /// </remarks>
    static PipelineTypes()
    {
        var providerService = typeof(IPipelineConfigurationProvider).ToString();

        Registration((builder, loggerFactory) =>
        {
            var log = loggerFactory?.CreateLogger<PipelineTypes>() ?? NullLogger<PipelineTypes>.Instance;

            builder.Services.TryAddSingleton<IPipelineConfigurationProvider>(sp =>
                new PipelineServiceConfigurationProvider(
                    sp.GetRequiredService<ILogger<PipelineServiceConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(), ConfigurationConnection, "pipe", "Pipeline",
                    ConfigurationPlacementSource.From(sp), sp.GetRequiredService<IDescriptionWriter>(), sp.GetRequiredService<IDescriptionReader>()));

            // Also published as the concrete class because the list, lookup, query and templates bases read the domain's records
            // (IDomainRecordProvider<DomainConfiguration>), which the netstandard2.0 interface cannot declare (FDW-4638).
            builder.Services.TryAddSingleton<PipelineServiceConfigurationProvider>(
                sp => (PipelineServiceConfigurationProvider)sp.GetRequiredService<IPipelineConfigurationProvider>());
            builder.Services.TryAddSingleton<IDomainConfigurationProvider<IPipelineImplementationConfiguration>>(
                sp => sp.GetRequiredService<IPipelineConfigurationProvider>());

            // The run contract (FDW-6131): every start goes through it and is handed to the runner of its pipeline type's run mode. Scoped: it reads through
            // the scope's providers and remembers the runs it started to be awaited. The runners are registered by the packages that host each run mode.
            builder.Services.TryAddScoped<IPipelineRuns, PipelineRuns>();

            foreach (var option in Options)
            {
                var optionRegistered = option.Register(builder, loggerFactory);
                if (optionRegistered.IsFailure)
                    return optionRegistered;
            }

            var declaredOptions = Options;
            var optionNames = string.Join(", ", declaredOptions.Select(option => option.Name));

            ServiceTypeLog.DomainOptionsCollected(log, nameof(PipelineTypes), declaredOptions.Length, optionNames);
            ServiceTypeLog.DomainProviderDeclared(log, nameof(PipelineTypes), providerService);

            if (declaredOptions.Length == 0)
                ServiceTypeLog.DomainRegisteredWithNoOptions(log, nameof(PipelineTypes), providerService);
            else
                ServiceTypeLog.DomainRegistered(log, nameof(PipelineTypes), declaredOptions.Length, optionNames, providerService);

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        // Why the run modes are proven here (FDW-6244): a pipeline type names its run mode by name, so a mode no option declares, or one no runner in a host
        // that runs pipelines carries out, would otherwise be found at the first start of a run of that type. Every gap is named in one pass, one message
        // each: the run-mode gaps, each option's own Initialize failure, then each option with no row provider.
        Initialization((host, loggerFactory) =>
        {
            var log = loggerFactory?.CreateLogger<PipelineTypes>() ?? NullLogger<PipelineTypes>.Instance;
            var gaps = RunModeGaps(host.Services, log);
            foreach (var option in Options)
            {
                var optionInitialized = option.Initialize(host, loggerFactory);
                if (optionInitialized.IsFailure)
                    gaps.AddRange(optionInitialized.Messages);
            }

            // Why after every option (FDW-6244): a pipeline type registers its row provider in its own Initialization, so only then is the absence of one
            // a gap. A type that set no phase registered none.
            gaps.AddRange(ImplementationProviderGaps(host.Services, log));
            return gaps.Count == 0 ? GenericResult<IHost>.Success(host) : GenericResult<IHost>.Failure(gaps);
        });
    }

    /// <summary>
    /// Names every pipeline type that has no row provider registered under its name on the pipe.Pipeline domain provider. A pipe.Pipeline row names an
    /// implementation, so every option must be readable.
    /// </summary>
    /// <remarks>
    /// It reads what is registered with the domain provider (its <c>Implementation(name)</c>, a lookup of the registered providers), never the
    /// configuration store. The provider itself is required of every option (PIPELINES-60833), and the
    /// collection checks that before this body runs.
    /// </remarks>
    private static IEnumerable<IGenericMessage> ImplementationProviderGaps(IServiceProvider services, ILogger log)
    {
        var domain = services.GetRequiredService<IPipelineConfigurationProvider>();
        return [.. All().Values
            .Where(type => domain.Implementation(type.Name).IsFailure)
            .Select(type => PipelineTypesLog.ImplementationProviderNotRegistered(log, type.Name))];
    }

    /// <summary>
    /// Names every pipeline type whose run mode is none of the <see cref="PipelineRunModes"/> options and, in a host that runs pipelines, every one whose run
    /// mode no registered <see cref="IPipelineRunner"/> carries out.
    /// </summary>
    /// <remarks>
    /// The runners are asked only of a host that names <see cref="OperationalConnection"/>, the same test the run services use to decide whether a host
    /// hosts runs: the API and the scheduler register this domain without running pipelines, and the Queued runner's services (the status broadcaster
    /// among them) are not theirs. A runner is scoped, so it is read in a scope of its own.
    /// </remarks>
    private static List<IGenericMessage> RunModeGaps(IServiceProvider services, ILogger log)
    {
        string[]? carriedOut = null;
        if (OperationalConnection is { Length: > 0 })
        {
            using var scope = services.CreateScope();
            carriedOut = [.. scope.ServiceProvider.GetServices<IPipelineRunner>().Select(runner => runner.RunMode.Name)];
        }

        var gaps = new List<IGenericMessage>();
        foreach (var type in All().Values)
        {
            var mode = PipelineRunModes.ByName(type.RunModeName);
            if (ReferenceEquals(mode, PipelineRunModes.NotFound))
                gaps.Add(PipelineTypesLog.RunModeNotDeclared(log, type.Name, type.RunModeName));
            else if (carriedOut is not null && !carriedOut.Contains(mode.Name, StringComparer.Ordinal))
                gaps.Add(PipelineTypesLog.RunModeHasNoRunner(log, type.Name, mode.Name));
        }

        return gaps;
    }
}
