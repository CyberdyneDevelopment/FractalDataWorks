using System.Linq;
using Fdw.Configuration;
using Fdw.Operations.Endpoints.ConfigurationMetadata;

namespace Fdw.Operations.Endpoints.Tests;

/// <summary>
/// FDW-3002: whether a setting is a secret is decided by [SecretValue] on the member, not by what its name contains.
/// </summary>
public sealed class ConfigurationPropertyExtractorSecretTests
{
    private sealed class Sample
    {
        public string? SecretManagerName { get; set; }

        public string? SecretKeyName { get; set; }

        public string? PasswordSecretName { get; set; }

        [SecretValue]
        public string? Token { get; set; }

        [SecretValue]
        public string? ClientSecret { get; set; }

        public string? Prefix { get; set; }
    }

    private static ConfigurationPropertyInfoDto Setting(string name)
        => ConfigurationPropertyExtractor.ExtractProperties(typeof(Sample)).Single(property => property.Name == name);

    [Theory]
    [InlineData("Token")]
    [InlineData("ClientSecret")]
    [Trait("Priority", "P0")]
    public void AMemberMarkedSecretValueIsASecretWhateverItIsCalled(string name)
    {
        Setting(name).IsSecret.ShouldBeTrue();
    }

    [Theory]
    [InlineData("SecretManagerName")]
    [InlineData("SecretKeyName")]
    [InlineData("PasswordSecretName")]
    [InlineData("Prefix")]
    [Trait("Priority", "P0")]
    public void AMemberNotMarkedIsNotASecretEvenWhenItsNameSaysSecretOrPassword(string name)
    {
        Setting(name).IsSecret.ShouldBeFalse();
    }
}
