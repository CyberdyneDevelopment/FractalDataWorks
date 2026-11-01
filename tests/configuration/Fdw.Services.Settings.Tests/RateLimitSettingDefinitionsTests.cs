using Fdw.Services.RateLimiting.Abstractions;
using Fdw.Services.Settings;
using Shouldly;
using Xunit;

namespace Fdw.Services.Settings.Tests;

/// <summary>
/// Tests that the settings registry knows the rate limit settings of every policy, made from the policy collection and not
/// listed by name (FDW-1945).
/// </summary>
public sealed class RateLimitSettingDefinitionsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void EverySettingOfEveryPolicyIsInTheRegistryWithItsKindAndDescription()
    {
        foreach (var policy in RateLimitPolicies.All())
        {
            foreach (var field in RateLimitAllowanceFields.All)
            {
                var name = RateLimitAllowanceFields.SettingName(policy.Name, field);

                SettingDefinitions.TryGet(name, out var definition).ShouldBeTrue(name);
                definition.DataType.ShouldBe(field.DataType);
                definition.Description.ShouldBe(field.Description);
            }
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void TheRegistryIsCaseInsensitiveLikeTheRestOfIt()
    {
        SettingDefinitions.TryGet("ratelimit.authenticated.requestsperwindow", out _).ShouldBeTrue();
    }

    [Theory]
    [InlineData("RateLimit.Nobody.RequestsPerWindow")]
    [InlineData("RateLimit.Admin.Colour")]
    [InlineData("RateLimit.Admin")]
    [InlineData("RateLimit")]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ANameThatIsNotAPolicyAndAFieldIsNotInTheRegistry(string name)
    {
        SettingDefinitions.TryGet(name, out _).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void TheSettingsAlreadyInTheRegistryAreStillThere()
    {
        SettingDefinitions.TryGet(SettingDefinitions.MaxPaginationSize, out var definition).ShouldBeTrue();
        definition.DataType.ShouldBe("Int32");
    }
}
