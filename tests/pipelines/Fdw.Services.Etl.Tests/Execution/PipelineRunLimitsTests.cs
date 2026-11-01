using System;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// Tests for the pipeline run limits (FDW-3085): the server limit is read for the host, the tenant limit for the run's tenant, and a
/// limit that is not set refuses the run, naming it.
/// </summary>
public sealed class PipelineRunLimitsTests
{
    private static Mock<IEffectiveSettingsProvider> Settings(int server, int tenant, Guid? tenantId)
    {
        var settings = new Mock<IEffectiveSettingsProvider>();
        settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.MaxConcurrentPipelineRunsPerServer, null, null)).Returns(server);
        settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.MaxConcurrentPipelineRunsPerTenant, tenantId, null)).Returns(tenant);
        return settings;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheServerLimitIsReadForTheHostAndTheTenantLimitForTheRunsTenant()
    {
        var tenantId = Guid.NewGuid();

        var read = PipelineRunLimits.Read(Settings(6, 2, tenantId).Object, tenantId, NullLogger.Instance);

        read.IsSuccess.ShouldBeTrue();
        read.Value.ShouldBe(new PipelineRunLimitValues(6, 2));
    }

    [Theory]
    [InlineData(0, 2, "MaxConcurrentPipelineRunsPerServer")]
    [InlineData(-1, 2, "MaxConcurrentPipelineRunsPerServer")]
    [InlineData(6, 0, "MaxConcurrentPipelineRunsPerTenant")]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ALimitThatIsNotPositiveRefusesTheRunNamingTheSetting(int server, int tenant, string setting)
    {
        var read = PipelineRunLimits.Read(Settings(server, tenant, null).Object, null, NullLogger.Instance);

        read.IsFailure.ShouldBeTrue();
        read.CurrentMessage.ShouldContain(setting);
    }
}
