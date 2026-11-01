using System;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// Tests for how many pipeline triggers the host holds before it starts them (FDW-3155): the number is the setting
/// MaxQueuedPipelineTriggers, and one that is not positive fails, naming it.
/// </summary>
public sealed class PipelineTriggerQueueLimitTests
{
    private static Mock<IEffectiveSettingsProvider> Settings(int capacity)
    {
        var settings = new Mock<IEffectiveSettingsProvider>();
        settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.MaxQueuedPipelineTriggers, null, null)).Returns(capacity);
        return settings;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheNumberIsTheSettingAsItIsStated()
    {
        var read = PipelineTriggerQueueLimit.Read(Settings(250).Object, NullLogger.Instance);

        read.IsSuccess.ShouldBeTrue();
        read.Value.ShouldBe(250);
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    [InlineData(0)]
    [InlineData(-3)]
    public void ANumberThatIsNotPositiveFailsNamingTheSetting(int capacity)
    {
        var read = PipelineTriggerQueueLimit.Read(Settings(capacity).Object, NullLogger.Instance);

        read.IsFailure.ShouldBeTrue();
        read.CurrentMessage.ShouldContain("MaxQueuedPipelineTriggers");
    }
}
