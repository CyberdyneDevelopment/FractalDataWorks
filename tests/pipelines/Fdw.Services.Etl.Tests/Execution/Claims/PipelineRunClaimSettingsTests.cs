using System.Linq;
using Fdw.Services.Etl.Execution.Claims;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Execution.Claims;

/// <summary>
/// FDW-6629: the four numbers of the durable claim are settings, and none is assumed: how long a claim is leased, how often the holder renews it, how often a waiting run asks again and how long a
/// call waits for the claim lock. A setting that is absent or not usable fails the read, naming it.
/// </summary>
public sealed class PipelineRunClaimSettingsTests
{
    private static Mock<IEffectiveSettingsProvider> Settings(int lease, int renewal, int poll, int lockMilliseconds)
    {
        var settings = new Mock<IEffectiveSettingsProvider>(MockBehavior.Strict);
        settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.PipelineRunClaimLeaseSeconds, null, null)).Returns(lease);
        settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.PipelineRunClaimRenewalSeconds, null, null)).Returns(renewal);
        settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.PipelineRunClaimWaitingPollSeconds, null, null)).Returns(poll);
        settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.PipelineRunClaimLockTimeoutMilliseconds, null, null)).Returns(lockMilliseconds);
        return settings;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void TheFourSettingsAreRead()
    {
        var read = PipelineRunClaimSettings.Read(Settings(60, 20, 5, 30000).Object, NullLogger.Instance);

        read.IsSuccess.ShouldBeTrue(read.CurrentMessage);
        read.Value.ShouldBe(new PipelineRunClaimSettings(60, 20, 5, 30000));
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    [InlineData(0, 20, 5, 30000, nameof(SettingDefinitions.PipelineRunClaimLeaseSeconds))]
    [InlineData(60, 0, 5, 30000, nameof(SettingDefinitions.PipelineRunClaimRenewalSeconds))]
    [InlineData(60, 20, 0, 30000, nameof(SettingDefinitions.PipelineRunClaimWaitingPollSeconds))]
    [InlineData(60, 20, 5, -1, nameof(SettingDefinitions.PipelineRunClaimLockTimeoutMilliseconds))]
    public void ASettingThatIsNotSetOrNotUsableFailsTheReadNamingIt(int lease, int renewal, int poll, int lockMilliseconds, string named)
    {
        var read = PipelineRunClaimSettings.Read(Settings(lease, renewal, poll, lockMilliseconds).Object, NullLogger.Instance);

        read.IsFailure.ShouldBeTrue();
        read.Messages.Select(message => message.Code).ShouldContain("PIPELINERUNCLAIM-60860");
        read.CurrentMessage.ShouldNotBeNull().ShouldContain(named);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void ARenewalNotShorterThanTheLeaseFailsTheReadBecauseTheLeaseWouldLapseBeforeItIsRenewed()
    {
        var read = PipelineRunClaimSettings.Read(Settings(60, 60, 5, 30000).Object, NullLogger.Instance);

        read.IsFailure.ShouldBeTrue();
        read.Messages.Select(message => message.Code).ShouldContain("PIPELINERUNCLAIM-60860");
        read.CurrentMessage.ShouldNotBeNull().ShouldContain(nameof(SettingDefinitions.PipelineRunClaimRenewalSeconds));
    }
}
