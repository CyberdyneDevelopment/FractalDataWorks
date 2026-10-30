using System.Linq;
using Fdw.Operations.Endpoints.ConfigurationMetadata;

namespace Fdw.Operations.Endpoints.Tests;

/// <summary>FDW-3044: a setting's description says whether it can be emptied, by the one rule the secret managers' null refusal uses.</summary>
public sealed class ConfigurationPropertyExtractorCanBeEmptyTests
{
    private sealed class Sample
    {
        public string Prefix { get; set; } = string.Empty;

        public string? ClientSecret { get; set; }

        public int Port { get; set; }

        public int? Timeout { get; set; }

        public bool IsEnabled { get; set; }
    }

    [Theory]
    [InlineData("Prefix", false)]
    [InlineData("ClientSecret", true)]
    [InlineData("Port", false)]
    [InlineData("Timeout", true)]
    [InlineData("IsEnabled", false)]
    [Trait("Priority", "P0")]
    public void ASettingCanBeEmptyOnlyWhenItsTypeAllowsNull(string name, bool canBeEmpty)
    {
        ConfigurationPropertyExtractor.ExtractProperties(typeof(Sample)).Single(property => property.Name == name).CanBeEmpty.ShouldBe(canBeEmpty);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void TheMethodAndTheDescriptionAgree()
    {
        foreach (var described in ConfigurationPropertyExtractor.ExtractProperties(typeof(Sample)))
        {
            ConfigurationPropertyExtractor.CanBeEmpty(typeof(Sample).GetProperty(described.Name)!).ShouldBe(described.CanBeEmpty);
        }
    }
}
