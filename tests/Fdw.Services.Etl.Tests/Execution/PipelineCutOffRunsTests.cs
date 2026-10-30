using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// Tests for what happens to the runs that were running when the ETL host stopped (FDW-3236): the restart limit is a setting with no default, a run is
/// started again while it has been started fewer times than the limit, and every other run is closed as failed with the reason it is not.
/// </summary>
public sealed class PipelineCutOffRunsTests
{
    private sealed class Settings(int? limit) : IEffectiveSettingsProvider
    {
        public int Reads { get; private set; }

        public T GetEffectiveValue<T>(string settingName, Guid? tenantId = null, string? roleName = null) => throw new NotSupportedException();

        public bool TryGetEffectiveValue<T>(string settingName, out T value, Guid? tenantId = null, string? roleName = null)
        {
            Reads++;
            if (limit is { } set && settingName == SettingDefinitions.MaxPipelineRunRestarts && typeof(T) == typeof(int))
            {
                value = (T)(object)set;
                return true;
            }

            value = default!;
            return false;
        }
    }

    private readonly Mock<IPipelineRunRequestStore> _store = new();

    private static InterruptedRun Run(string pipeline, int restarts) => new(
        new PipelineExecutionRequest
        {
            ExecutionId = Guid.NewGuid(), PipelineId = Guid.NewGuid(), PipelineName = pipeline, TriggerSource = "Api", TenantId = Guid.NewGuid(), RunRoles = ["DataWriter"]
        },
        restarts);

    private PipelineCutOffRuns Sut(Settings settings)
    {
        var accessor = new Mock<IAuthenticationContextAccessor>();
        accessor.SetupProperty(a => a.Current);
        var scopes = new ServiceCollection().AddSingleton<IEffectiveSettingsProvider>(settings).AddSingleton(accessor.Object).BuildServiceProvider()
            .GetRequiredService<IServiceScopeFactory>();
        _store.Setup(s => s.GiveUp(It.IsAny<InterruptedRun>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
        _store.Setup(s => s.StartAgain(It.IsAny<InterruptedRun>(), It.IsAny<CancellationToken>())).ReturnsAsync((InterruptedRun run, CancellationToken _) =>
            GenericResult<PipelineExecutionRequest>.Success(new PipelineExecutionRequest
            {
                ExecutionId = Guid.NewGuid(), PipelineId = run.Request.PipelineId, PipelineName = run.Request.PipelineName, TriggerSource = run.Request.TriggerSource,
                TenantId = run.Request.TenantId, RunRoles = run.Request.RunRoles
            }));
        return new PipelineCutOffRuns(_store.Object, scopes, NullLogger.Instance);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task WhileTheLimitIsNotSetNoRunIsStartedAgainAndEachIsClosedNamingTheSetting()
    {
        var sut = Sut(new Settings(null));
        var runs = new[] { Run("a", 0), Run("b", 0) };

        var started = await sut.StartAgain(runs, TestContext.Current.CancellationToken);

        started.ShouldBeEmpty();
        _store.Verify(s => s.StartAgain(It.IsAny<InterruptedRun>(), It.IsAny<CancellationToken>()), Times.Never);
        _store.Verify(s => s.GiveUp(It.IsAny<InterruptedRun>(), It.Is<string>(reason => reason.Contains("MaxPipelineRunRestarts")), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    [InlineData(1, 0, true)]
    [InlineData(1, 1, false)]
    [InlineData(3, 2, true)]
    [InlineData(3, 3, false)]
    [InlineData(0, 0, false)]
    public async Task ARunIsStartedAgainOnlyWhileItHasBeenStartedAgainFewerTimesThanTheLimit(int limit, int restarts, bool startedAgain)
    {
        var sut = Sut(new Settings(limit));
        var run = Run("monthly-sales", restarts);

        var started = await sut.StartAgain([run], TestContext.Current.CancellationToken);

        started.Count.ShouldBe(startedAgain ? 1 : 0);
        _store.Verify(s => s.StartAgain(run, It.IsAny<CancellationToken>()), startedAgain ? Times.Once : Times.Never);
        _store.Verify(s => s.GiveUp(run, It.IsAny<string>(), It.IsAny<CancellationToken>()), startedAgain ? Times.Never : Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TheNewRunsComeBackInTheOrderOfTheOldOnesAndOnlyForTheRunsThatMayStartAgain()
    {
        var sut = Sut(new Settings(1));
        var runs = new[] { Run("first", 0), Run("spent", 1), Run("third", 0) };

        var started = await sut.StartAgain(runs, TestContext.Current.CancellationToken);

        started.Select(request => request.PipelineName).ShouldBe(["first", "third"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task NoRunsMeansNoReadOfTheSettingAndNoCall()
    {
        var settings = new Settings(null);
        var sut = Sut(settings);

        var started = await sut.StartAgain([], TestContext.Current.CancellationToken);

        started.ShouldBeEmpty();
        settings.Reads.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARunThatCannotBeStartedAgainIsNotInTheListAndTheOthersStillAre()
    {
        var sut = Sut(new Settings(2));
        var bad = Run("bad", 0);
        var good = Run("good", 0);
        _store.Setup(s => s.StartAgain(bad, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<PipelineExecutionRequest>.Failure(new GenericMessage("no record")));

        var started = await sut.StartAgain([bad, good], TestContext.Current.CancellationToken);

        started.Select(request => request.PipelineName).ShouldBe(["good"]);
    }
}
