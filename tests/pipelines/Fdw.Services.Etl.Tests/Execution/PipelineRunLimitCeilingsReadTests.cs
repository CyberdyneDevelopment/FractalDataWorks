using System;
using System.Linq;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// Tests for <see cref="PipelineRunLimitCeilings.Read"/> (FDW-6141, slice 2): the ceilings on an Interactive pipeline's time and steps are two settings, read for the run's
/// tenant; a ceiling that is not set to a positive number refuses the run and names the setting, and none is assumed.
/// </summary>
public sealed class PipelineRunLimitCeilingsReadTests
{
    private static readonly Guid Tenant = Guid.Parse("22222222-0000-0000-0000-000000000002");

    private static Mock<IEffectiveSettingsProvider> Settings(int runSeconds, int steps)
    {
        var settings = new Mock<IEffectiveSettingsProvider>(MockBehavior.Strict);
        settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.InteractiveMaxRunSecondsCeiling, Tenant, null)).Returns(runSeconds);
        settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.InteractiveMaxStepsCeiling, Tenant, null)).Returns(steps);
        return settings;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void TheCeilingsAreTheTwoSettingsReadForTheRunsTenant()
    {
        var read = PipelineRunLimitCeilings.Read(Settings(runSeconds: 600, steps: 200).Object, Tenant, NullLogger.Instance);

        read.IsSuccess.ShouldBeTrue(read.CurrentMessage);
        read.Value.ShouldBe(new PipelineRunLimitCeilings(600, 200));
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    [InlineData(0, 200, nameof(SettingDefinitions.InteractiveMaxRunSecondsCeiling))]
    [InlineData(-1, 200, nameof(SettingDefinitions.InteractiveMaxRunSecondsCeiling))]
    [InlineData(600, 0, nameof(SettingDefinitions.InteractiveMaxStepsCeiling))]
    public void ACeilingThatIsNotPositiveRefusesTheRunNamingTheSetting(int runSeconds, int steps, string named)
    {
        var read = PipelineRunLimitCeilings.Read(Settings(runSeconds, steps).Object, Tenant, NullLogger.Instance);

        read.IsFailure.ShouldBeTrue();
        read.Messages.Select(message => message.Code).ShouldContain("PIPELINERUN-20844");
        read.CurrentMessage.ShouldNotBeNull().ShouldContain(named);
    }
}
