using System.Linq;
using Fdw.Operations.Endpoints.ConfigurationMetadata;

namespace Fdw.Operations.Endpoints.Tests;

/// <summary>FDW-3025: a setting's label is its name with a space before each capital, not a literal "$1".</summary>
public sealed class ConfigurationPropertyExtractorLabelTests
{
    private sealed class Sample
    {
        public bool IsEnabled { get; set; }

        public bool CaseSensitive { get; set; }

        public string? Prefix { get; set; }

        public string? AuthSecretKeyName { get; set; }
    }

    [Theory]
    [InlineData("IsEnabled", "Is Enabled")]
    [InlineData("CaseSensitive", "Case Sensitive")]
    [InlineData("Prefix", "Prefix")]
    [InlineData("AuthSecretKeyName", "Auth Secret Key Name")]
    [Trait("Priority", "P0")]
    public void ASettingsLabelHasASpaceBeforeEachCapitalAfterTheFirst(string name, string label)
    {
        ConfigurationPropertyExtractor.ExtractProperties(typeof(Sample)).Single(property => property.Name == name).DisplayName.ShouldBe(label);
    }
}
