using Fdw.Services.Etl.Execution;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// Tests for the time a stopping host gives its runs (FDW-3786): a setting with no default that must be positive, read for the host, and a
/// shutdown timeout twice as long so the records of the runs it stopped can be closed.
/// </summary>
public sealed class PipelineStopGraceTests
{
    private static IEffectiveSettingsProvider Settings(int seconds)
    {
        var settings = new Mock<IEffectiveSettingsProvider>();
        settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.PipelineStopGraceSeconds, null, null)).Returns(seconds);
        return settings.Object;
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    [InlineData(1)]
    [InlineData(60)]
    public void APositiveNumberOfSecondsIsTheTimeAndTheShutdownTimeoutIsTwiceIt(int seconds)
    {
        var read = PipelineStopGrace.Read(Settings(seconds), NullLogger.Instance);

        read.IsSuccess.ShouldBeTrue(read.CurrentMessage);
        read.Value!.Duration.ShouldBe(TimeSpan.FromSeconds(seconds));
        read.Value.ShutdownTimeout.ShouldBe(TimeSpan.FromSeconds(seconds * 2));
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    [InlineData(0)]
    [InlineData(-5)]
    public void ANumberThatIsNotPositiveFailsNamingTheSettingAndNoTimeIsAssumed(int seconds)
    {
        var read = PipelineStopGrace.Read(Settings(seconds), NullLogger.Instance);

        read.IsFailure.ShouldBeTrue();
        read.CurrentMessage.ShouldNotBeNull().ShouldContain("PipelineStopGraceSeconds");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ATimeThatIsNotPositiveCannotBeBuilt()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => PipelineStopGrace.Of(TimeSpan.Zero));
    }
}
