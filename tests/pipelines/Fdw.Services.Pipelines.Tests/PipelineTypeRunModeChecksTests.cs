using System;
using System.Linq;
using Fdw.Results;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Abstractions.Thresholds;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Pipelines.Notifications;
using Fdw.Services.Settings;
using Fdw.Services.Pipelines.Abstractions.Runs;
using Fdw.Services.Pipelines.TestDouble;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Fdw.Services.Pipelines.Tests;

/// <summary>
/// FDW-6244: a pipeline type's run mode is proven when the host starts, not at the first start of a run. A type whose run mode is none of the
/// <see cref="PipelineRunModes"/> options stops every host at Initialize; a run mode no registered runner carries out stops a host that runs pipelines (one
/// that names <see cref="PipelineTypes.OperationalConnection"/>). Each gap is its own coded message. The test double's <see cref="UnroutedPipelineType"/>
/// names an undeclared run mode and <see cref="UnrunPipelineType"/> names a declared one (<see cref="UnrunPipelineRunMode"/>) that no runner carries out. <see cref="ImmediatePipelineType"/>
/// and the Interactive type run AtOnce, which FDW ships a runner for, so neither is ever listed (FDW-6141).
/// </summary>
/// <remarks>
/// GraphPipelineType is collected in this assembly too (Fdw.Services.Pipelines.Endpoints references Fdw.Services.Etl), so every host here has its Queued
/// runner and the services its registration needs; a Queued type is never the one without a runner.
/// </remarks>
public sealed class PipelineTypeRunModeChecksTests : IDisposable
{
    // Why every assertion filters by code (FDW-6270): each test starts its own host, but option phases run once per process. Each host forces its
    // options' Register (see Initialize below); their Initialize is left as the domain runs it, so only the first host runs the test double's
    // Initialization bodies, and later hosts collect PIPELINES-60837 (ImplementationProviderNotRegistered) naming each. The filter by code is
    // deliberate: these tests judge the run-mode checks, not row providers.
    private const string RunModeNotDeclared = "PIPELINES-60834";
    private const string RunModeHasNoRunner = "PIPELINES-60835";
    private const bool Force = true;

    private readonly string? _before = PipelineTypes.OperationalConnection;

    public void Dispose() => PipelineTypes.OperationalConnection = _before;

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void APipelineTypeWhoseRunModeIsNoneOfTheRunModesStopsTheHostAtInitialize()
    {
        PipelineTypes.OperationalConnection = null;

        var initialized = Initialize(runnerRunMode: null);

        initialized.IsFailure.ShouldBeTrue();
        var message = initialized.Messages.Single(m => string.Equals(m.Code, RunModeNotDeclared, StringComparison.Ordinal));
        message.Message.ShouldContain(UnroutedPipelineType.OptionName);
        message.Message.ShouldContain(UnroutedPipelineType.RunMode);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void ARunModeNoRegisteredRunnerCarriesOutStopsAHostThatRunsPipelinesAtInitialize()
    {
        PipelineTypes.OperationalConnection = "OpsDb";

        var initialized = Initialize(runnerRunMode: null);

        initialized.IsFailure.ShouldBeTrue();
        // One type runs a declared run mode no runner carries out: the test double's. Each such type is its own message, so each is named once.
        var gaps = initialized.Messages.Where(m => string.Equals(m.Code, RunModeHasNoRunner, StringComparison.Ordinal)).ToList();
        var message = gaps.ShouldHaveSingleItem();
        message.Message.ShouldContain(UnrunPipelineType.OptionName);
        message.Message.ShouldContain(UnrunPipelineType.RunMode);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void TheAtOnceRunnerFDWShipsCarriesTheInteractiveAndTheImmediateTypesSoNeitherIsListed()
    {
        PipelineTypes.OperationalConnection = "OpsDb";

        var initialized = Initialize(runnerRunMode: null);

        var gaps = initialized.Messages.Where(m => string.Equals(m.Code, RunModeHasNoRunner, StringComparison.Ordinal)).ToList();
        gaps.ShouldNotContain(m => m.Message.Contains(InteractivePipelineType.OptionName, StringComparison.Ordinal));
        gaps.ShouldNotContain(m => m.Message.Contains(ImmediatePipelineType.OptionName, StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void ARunModeARegisteredRunnerCarriesOutIsNotRefused()
    {
        PipelineTypes.OperationalConnection = "OpsDb";

        var initialized = Initialize(runnerRunMode: UnrunPipelineType.RunMode);

        initialized.Messages.ShouldNotContain(m => string.Equals(m.Code, RunModeHasNoRunner, StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void AHostThatRunsNoPipelinesIsNotAskedForRunners()
    {
        // The API and the scheduler register this domain without running pipelines; the Queued runner's services (the status broadcaster among them) are
        // not theirs, so they are not asked for it.
        PipelineTypes.OperationalConnection = null;

        var initialized = Initialize(runnerRunMode: null);

        initialized.Messages.ShouldNotContain(m => string.Equals(m.Code, RunModeHasNoRunner, StringComparison.Ordinal));
    }

    // Each test's host is built on its own: it registers what a real host registers, and every option is registered for it, whatever an earlier host
    // in this process ran.
    private static IGenericResult<IHost> Initialize(string? runnerRunMode)
    {
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.Services.AddLogging();

        // What every host that goes through PlatformServices has: the authentication-context accessor (PlatformServiceEntry registers it for every
        // host), and the effective settings, of which the graph pipeline type's run services read the stop grace when the host is built.
        builder.Services.AddSingleton<IAuthenticationContextAccessor, AuthenticationContextAccessor>();
        var settings = new Mock<IEffectiveSettingsProvider>(MockBehavior.Strict);
        settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.PipelineStopGraceSeconds, null, null)).Returns(30);
        builder.Services.AddSingleton(settings.Object);

        // The test double's pipeline types register their row providers at Initialize, which builds the pipe.Pipeline domain provider; it holds a
        // gateway provider it does not call here. The graph pipeline type requires the threshold evaluator.
        builder.Services.AddSingleton(new Mock<IConfigurationGatewayProvider>(MockBehavior.Strict).Object);
        builder.Services.AddSingleton(new Mock<Fdw.Services.Descriptions.IDescriptionWriter>(MockBehavior.Strict).Object);
        builder.Services.AddSingleton(new Mock<Fdw.Services.Descriptions.Abstractions.IDescriptionReader>(MockBehavior.Strict).Object);
        builder.Services.AddSingleton(new Mock<IThresholdEvaluator>(MockBehavior.Strict).Object);

        // Why each option is registered here, forced, before the domain registers (FDW-6270): an option's phases run once per process, so a host after the
        // first would otherwise get none of its options' registrations. Registered first, each option is registered once for this host, and the domain's
        // own Register skips it.
        foreach (var type in PipelineTypes.All().Values)
        {
            type.Register(builder, NullLoggerFactory.Instance, force: true).IsSuccess.ShouldBeTrue(type.Name);
        }

        PipelineTypes.Configure(builder, NullLoggerFactory.Instance, Force).IsSuccess.ShouldBeTrue();
        PipelineTypes.Register(builder, NullLoggerFactory.Instance, Force).IsSuccess.ShouldBeTrue();

        // The services the graph pipeline type's Queued runner is built from, which a host that runs pipelines has; the runner check builds it. Added after
        // the registrations, so these are the ones resolved. Every stand-in here is strict: the runner's constructor only keeps them, and a call to any
        // would be a change these tests must hear of.
        builder.Services.AddSingleton(new Mock<IExecutionTracker>(MockBehavior.Strict).Object);
        builder.Services.AddSingleton(new Mock<IDataGatewayProvider>(MockBehavior.Strict).Object);
        builder.Services.AddSingleton(new Mock<IPipelineExecutionQueue>(MockBehavior.Strict).Object);
        builder.Services.AddSingleton(new Mock<IPipelineStatusBroadcaster>(MockBehavior.Strict).Object);
        builder.Services.AddSingleton(new Mock<IPipelineWaitObserver>(MockBehavior.Strict).Object);
        builder.Services.AddSingleton(new Mock<IPipelineRunOrigins>(MockBehavior.Strict).Object);
        if (runnerRunMode is not null)
        {
            var runner = new Mock<IPipelineRunner>(MockBehavior.Strict);
            runner.SetupGet(r => r.RunMode).Returns(PipelineRunModes.ByName(runnerRunMode));
            builder.Services.AddScoped(_ => runner.Object);
        }

        using var host = builder.Build();
        return PipelineTypes.Initialize(host, NullLoggerFactory.Instance, Force);
    }
}
