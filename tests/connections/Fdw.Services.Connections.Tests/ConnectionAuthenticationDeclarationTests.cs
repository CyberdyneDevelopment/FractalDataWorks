using Fdw.Services.Connections.Abstractions.Authentication;
using Fdw.Services.Connections.MsSql;
using Fdw.Services.Connections.MsSql.Authentication;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Tests;

/// <summary>
/// FDW-4607: every connection kind states its authentication position. A kind that says it has no authentication dictionary while its
/// configuration holds one is named, so the kind is refused where it is used instead of storing keys nobody checked.
/// </summary>
public sealed class ConnectionAuthenticationDeclarationTests
{
    private sealed class ConfigurationWithoutADictionary
    {
        public string Root { get; set; } = string.Empty;
    }

    [Fact]
    public void ANoneDeclarationNamesTheStringDictionariesItLeavesUnaccountedFor()
    {
        ConnectionAuthentication<MsSqlConnectionConfiguration>.None.UndeclaredDictionaries.ShouldContain(nameof(MsSqlConnectionConfiguration.AdditionalProperties));
    }

    [Fact]
    public void ADeclaredDictionaryLeavesNothingUnaccountedFor()
    {
        var declaration = ConnectionAuthentication<MsSqlConnectionConfiguration>.Of(
            nameof(MsSqlConnectionConfiguration.AdditionalProperties),
            configuration => MsSqlAuthenticationTypes.ByName(configuration.AuthenticationType));

        declaration.UndeclaredDictionaries.ShouldBeEmpty();
        declaration.DictionaryProperty.ShouldBe(nameof(MsSqlConnectionConfiguration.AdditionalProperties));
    }

    [Fact]
    public void ANoneDeclarationOverAConfigurationWithoutADictionaryIsComplete()
    {
        ConnectionAuthentication<ConfigurationWithoutADictionary>.None.UndeclaredDictionaries.ShouldBeEmpty();
    }

    [Fact]
    public void ADeclarationNeedsADictionaryProperty()
    {
        Should.Throw<System.ArgumentException>(() => ConnectionAuthentication<MsSqlConnectionConfiguration>.Of(" ", configuration => MsSqlAuthenticationTypes.ByName(configuration.AuthenticationType)));
    }
}
