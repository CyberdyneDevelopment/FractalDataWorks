using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// The most keys one configuration read carries is a setting with its default declared on the collection, and the provider every reader takes states
/// it (FDW-4254). Before it was a literal in four readers.
/// </summary>
public sealed class ConfigurationGatewayProviderKeyChunkSizeTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void TheProviderStatesWhatTheCollectionDeclares()
    {
        var provider = new ConfigurationGatewayProvider();

        provider.KeyChunkSize.ShouldBe(ConfigurationGatewayTypes.KeyChunkSize);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void TheDeclaredDefaultIsAtLeastOne()
    {
        ConfigurationGatewayTypes.KeyChunkSize.ShouldBeGreaterThanOrEqualTo(1);
    }
}
