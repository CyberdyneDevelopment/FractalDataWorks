using System.Text.Json;
using Fdw.Aegis.Configuration;

namespace Fdw.Aegis.Abstractions.Tests;

public class ParameterAllowEntryTests
{
    [Fact]
    [Trait("Category", "Security")]
    public void ParameterAllowEntryRoundTripsThroughJson()
    {
        var entry = new ParameterAllowEntry
        {
            ParameterName = "mode",
            PermittedValues = ["echo"],
            Required = true,
        };

        var json = JsonSerializer.Serialize(entry);
        var roundTripped = JsonSerializer.Deserialize<ParameterAllowEntry>(json);

        roundTripped.ShouldNotBeNull();
        roundTripped.ParameterName.ShouldBe("mode");
        roundTripped.PermittedValues.ShouldBe(["echo"]);
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
        config.ParameterAllowList.Add(new ParameterAllowEntry
        {
            ParameterName = "mode",
            PermittedValues = ["echo"],
            Required = true,
        });

        config.ParameterAllowList.Count.ShouldBe(1);
        config.ParameterAllowList[0].PermittedValues.ShouldContain("echo");
    }
}
