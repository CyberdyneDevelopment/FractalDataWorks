using System.Linq;
using System.Text.Json;
using Fdw.Aegis.Configuration;

namespace Fdw.Aegis.Abstractions.Tests;

public class ParameterAllowEntryTests
{
    [Fact]
    [Trait("Category", "Security")]
    public void ParameterAllowEntryRoundTripsThroughJson()
    {
        var entry = new ParameterAllowEntryConfiguration
        {
            ParameterName = "mode",
            PermittedValues = [new PermittedValueConfiguration { Value = "echo" }],
            Required = true,
        };

        var json = JsonSerializer.Serialize(entry);
        var roundTripped = JsonSerializer.Deserialize<ParameterAllowEntryConfiguration>(json);

        roundTripped.ShouldNotBeNull();
        roundTripped.ParameterName.ShouldBe("mode");
        roundTripped.PermittedValues.Select(v => v.Value).ShouldBe(["echo"]);
        roundTripped.Required.ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "Security")]
    public void PreApprovedCommandConfigurationCarriesTheAllowListEntry()
    {
        var config = new PreApprovedCommandConfiguration
        {
            SecretManagerName = "EnvSecrets",
            SecretKeyName = "AEGIS_SYNTHETIC_TOKEN",
        };
        config.ParameterAllowList.Add(new ParameterAllowEntryConfiguration
        {
            ParameterName = "mode",
            PermittedValues = [new PermittedValueConfiguration { Value = "echo" }],
            Required = true,
        });

        config.ParameterAllowList.Count.ShouldBe(1);
        config.ParameterAllowList[0].PermittedValues.Select(v => v.Value).ShouldContain("echo");
    }
}
