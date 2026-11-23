using System;
using System.Linq;
using Fdw.Services.Etl.Execution.Claims;
using Fdw.Services.Pipelines;
using Fdw.Services.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Execution.Claims;

/// <summary>
/// FDW-6939 (slice 3 of FDW-6141): a host that runs pipelines proves, when it initializes, that the four settings of the durable claim are usable, so it stops at start naming
/// the setting and not at the first claim, hours later. A host that does not run pipelines (the API, the scheduler) is not asked.
/// </summary>
[Collection(EtlOperationalConnectionCollection.Name)]
public sealed class PipelineRunClaimSettingsGapsTests : IDisposable
{
    private readonly string? _before = PipelineTypes.OperationalConnection;

    public void Dispose() => PipelineTypes.OperationalConnection = _before;

    private static ServiceProvider Host(int lease, int renewal, int poll, int lockMilliseconds)
    {
        var settings = new Mock<IEffectiveSettingsProvider>(MockBehavior.Strict);
        settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.PipelineRunClaimLeaseSeconds, null, null)).Returns(lease);
        settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.PipelineRunClaimRenewalSeconds, null, null)).Returns(renewal);
        settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.PipelineRunClaimWaitingPollSeconds, null, null)).Returns(poll);
        settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.PipelineRunClaimLockTimeoutMilliseconds, null, null)).Returns(lockMilliseconds);
        var services = new ServiceCollection();
        services.AddSingleton(settings.Object);
        return services.BuildServiceProvider();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void AHostThatDoesNotRunPipelinesIsNotAskedAndNeedsNoSettings()
    {
        PipelineTypes.OperationalConnection = null;
        using var host = new ServiceCollection().BuildServiceProvider();

        PipelineRunClaimSettings.Gaps(host, NullLogger.Instance).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void AHostThatRunsPipelinesAndStatesUsableSettingsHasNoGap()
    {
        PipelineTypes.OperationalConnection = "OpsDb";
        using var host = Host(60, 20, 5, 30000);

        PipelineRunClaimSettings.Gaps(host, NullLogger.Instance).ShouldBeEmpty();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    [InlineData(0, 20, 5, 30000, nameof(SettingDefinitions.PipelineRunClaimLeaseSeconds))]
    [InlineData(60, 0, 5, 30000, nameof(SettingDefinitions.PipelineRunClaimRenewalSeconds))]
    [InlineData(60, 20, 0, 30000, nameof(SettingDefinitions.PipelineRunClaimWaitingPollSeconds))]
    [InlineData(60, 20, 5, -1, nameof(SettingDefinitions.PipelineRunClaimLockTimeoutMilliseconds))]
    public void AHostThatRunsPipelinesAndStatesASettingThatIsNotUsableDoesNotStartNamingIt(int lease, int renewal, int poll, int lockMilliseconds, string named)
    {
        PipelineTypes.OperationalConnection = "OpsDb";
        using var host = Host(lease, renewal, poll, lockMilliseconds);

        var gaps = PipelineRunClaimSettings.Gaps(host, NullLogger.Instance).ToList();

        gaps.ShouldNotBeEmpty();
        gaps.Select(gap => gap.Code).ShouldContain("PIPELINERUNCLAIM-60860");
        gaps.Select(gap => gap.Message).ShouldContain(message => message.Contains(named, StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void AHostThatRunsPipelinesAndRegistersNoSettingsServiceDoesNotStartWithTheCodedGap()
    {
        PipelineTypes.OperationalConnection = "OpsDb";
        using var host = new ServiceCollection().BuildServiceProvider();

        var gaps = PipelineRunClaimSettings.Gaps(host, NullLogger.Instance).ToList();

        gaps.Select(gap => gap.Code).ShouldBe(["PIPELINERUNCLAIM-60870"]);
    }
}
