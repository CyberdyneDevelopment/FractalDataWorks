using Fdw.Services.Etl.Execution;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>Tests for reading how many times the host starts a cut-off run again (FDW-3236): no default in code, a number someone sets, zero allowed.</summary>
public sealed class PipelineRunRestartLimitTests
{
    private sealed class Settings(bool isSet, int value) : IEffectiveSettingsProvider
    {
        public T GetEffectiveValue<T>(string settingName, Guid? tenantId = null, string? roleName = null) => throw new NotSupportedException();

        public bool TryGetEffectiveValue<T>(string settingName, out T found, Guid? tenantId = null, string? roleName = null)
        {
            found = isSet && settingName == SettingDefinitions.MaxPipelineRunRestarts ? (T)(object)value : default!;
            return isSet && settingName == SettingDefinitions.MaxPipelineRunRestarts;
        }
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    public void ANumberSomeoneSetsIsTheLimitAndZeroIsANumber(int set)
    {
        var limit = PipelineRunRestartLimit.Read(new Settings(true, set), NullLogger.Instance);

        limit.IsSuccess.ShouldBeTrue(limit.CurrentMessage);
        limit.Value.ShouldBe(set);
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData(false, 0)]
    [InlineData(false, 3)]
    [InlineData(true, -1)]
    public void ASettingThatIsNotSetOrIsNegativeIsAFailureNamingItAndNeverAnAssumedNumber(bool isSet, int value)
    {
        var limit = PipelineRunRestartLimit.Read(new Settings(isSet, value), NullLogger.Instance);

        limit.IsFailure.ShouldBeTrue();
        limit.CurrentMessage.ShouldNotBeNull().ShouldContain("MaxPipelineRunRestarts");
    }
}
