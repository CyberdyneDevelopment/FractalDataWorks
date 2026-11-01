using System.Text.Json;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Endpoints;
using Fdw.Services.SecretManagers.TestDouble;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.SecretManagers.Tests;

/// <summary>
/// FDW-2890: the settings a person sends for a secret manager are read as that type's own, an update lays them over the stored ones, a setting the
/// type does not take is refused by name instead of being ignored, and the type list asks the platform administrator's policy.
/// </summary>
public sealed class SecretManagerSettingsTests
{
    private const string Type = SyntheticSecretManagerType.OptionName;

    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();

    private sealed class Types(Microsoft.Extensions.Logging.ILogger<ListSecretManagerTypesEndpointBase> logger) : ListSecretManagerTypesEndpointBase(logger)
    {
        internal string Policy => ReadPolicy;

        internal SecretManagerTypeDto Describe(ISecretManagerType type) => MapToDto(type);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ASecretManagerIsReadFromTheSettingsSent()
    {
        var read = SecretManagerSettings.Read(Type, Json("""{"Prefix":"app-"}"""), null, NullLogger.Instance);

        read.IsSuccess.ShouldBeTrue();
        read.Value.ShouldBeOfType<SyntheticSecretManagerConfiguration>().Prefix.ShouldBe("app-");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AnUpdateChangesTheSettingsSentAndKeepsTheStoredIdentity()
    {
        var id = Guid.NewGuid();
        var stored = new SyntheticSecretManagerConfiguration { Id = id, Name = "vault", Domain = "SecretManager", Implementation = Type, Prefix = "old-" };

        var read = SecretManagerSettings.Read(Type, Json("""{"prefix":"new-"}"""), stored, NullLogger.Instance);

        read.IsSuccess.ShouldBeTrue();
        var changed = read.Value.ShouldBeOfType<SyntheticSecretManagerConfiguration>();
        changed.Prefix.ShouldBe("new-");
        changed.Id.ShouldBe(id);
        changed.Name.ShouldBe("vault");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AnUpdateThatSendsNothingForAMemberKeepsItsStoredValue()
    {
        var stored = new SyntheticSecretManagerConfiguration { Name = "vault", Prefix = "keep-" };

        var read = SecretManagerSettings.Read(Type, Json("{}"), stored, NullLogger.Instance);

        read.Value.ShouldBeOfType<SyntheticSecretManagerConfiguration>().Prefix.ShouldBe("keep-");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ASettingTheTypeDoesNotTakeIsRefusedByNameWithThoseItTakes()
    {
        var read = SecretManagerSettings.Read(Type, Json("""{"Prefx":"typo"}"""), null, NullLogger.Instance);

        read.IsFailure.ShouldBeTrue();
        var reason = read.Messages.Single().Message;
        reason.ShouldContain("Prefx");
        reason.ShouldContain("Prefix");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AnUnknownTypeIsRefusedByItsNameAndTheKnownTypesNotByAClassName()
    {
        var read = SecretManagerSettings.Read("Nope", Json("{}"), null, NullLogger.Instance);

        read.IsFailure.ShouldBeTrue();
        var reason = read.Messages.Single().Message;
        reason.ShouldContain("'Nope'");
        reason.ShouldContain(Type);
        reason.ShouldNotContain("Configuration");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("[]")]
    [InlineData("\"text\"")]
    [Trait("Priority", "P1")]
    public void SettingsThatAreNotAJsonObjectAreRefusedWithTheReason(string? sent)
    {
        var read = SecretManagerSettings.Read(Type, sent is null ? null : Json(sent), null, NullLogger.Instance);

        read.IsFailure.ShouldBeTrue();
        read.Messages.Single().Message.ShouldContain("JSON object");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void TheTypeListNamesEachTypesOwnDescriptionAndTheContainerHoldingItsSettings()
    {
        var type = SecretManagerTypes.ByName(Type);

        var described = new Types(NullLogger<ListSecretManagerTypesEndpointBase>.Instance).Describe(type);

        described.Name.ShouldBe(Type);
        described.DisplayName.ShouldBe(type.DisplayName);
        described.Description.ShouldBe(type.Description);
        described.SettingsContainer.ShouldBe(type.Container);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ANullForASettingThatAlwaysHasAValueIsRefusedNamingIt()
    {
        var read = SecretManagerSettings.Read(Type, Json("{\"Prefix\":null}"), Stored(), NullLogger.Instance);

        read.IsFailure.ShouldBeTrue();
        var reason = read.Messages.Single().Message;
        reason.ShouldContain("Prefix");
        reason.ShouldContain("always has a value");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ANullForASettingThatCanBeEmptyClearsIt()
    {
        var read = SecretManagerSettings.Read(Type, Json("{\"ClientSecret\":null}"), Stored(), NullLogger.Instance);

        read.IsSuccess.ShouldBeTrue();
        read.Value.ShouldBeOfType<SyntheticSecretManagerConfiguration>().ClientSecret.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void TheTypeListDescribesExactlyTheSettingsTheReaderTakes()
    {
        var type = SecretManagerTypes.ByName(Type);

        var described = new Types(NullLogger<ListSecretManagerTypesEndpointBase>.Instance).Describe(type);

        described.Settings.Select(setting => setting.Name).OrderBy(name => name, StringComparer.Ordinal)
            .ShouldBe(SecretManagerSettings.Takes(type.ConfigurationType));
        described.Settings.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void EverySettingTheTypeListDescribesIsAcceptedByTheReader()
    {
        var type = SecretManagerTypes.ByName(Type);
        var described = new Types(NullLogger<ListSecretManagerTypesEndpointBase>.Instance).Describe(type);

        foreach (var setting in described.Settings)
        {
            var read = SecretManagerSettings.Read(Type, Json($$"""{"{{setting.Name}}":null}"""), null, NullLogger.Instance);

            read.Messages.Any(message => message.Message.Contains("is not one a", StringComparison.Ordinal)).ShouldBeFalse(setting.Name);
        }
    }

    private static SyntheticSecretManagerConfiguration Stored() => new()
    {
        Id = Guid.NewGuid(), Name = "vault", Domain = "SecretManager", Implementation = Type, Prefix = "p-", ClientSecret = "s3cr3t"
    };

    [Fact]
    [Trait("Priority", "P0")]
    public void ASecretIsReadBackAsTheMaskAndNeverItsValue()
    {
        var shown = SecretManagerSettings.Shown(SecretManagerTypes.ByName(Type), Stored(), NullLogger.Instance);

        shown.IsSuccess.ShouldBeTrue();
        shown.Value!["ClientSecret"]!.GetValue<string>().ShouldBe(ConnectionSecretMask.Value);
        shown.Value["Prefix"]!.GetValue<string>().ShouldBe("p-");
        shown.Value.ToJsonString().ShouldNotContain("s3cr3t");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ASecretThatHoldsNothingIsReadBackAsNullAndTheSystemMembersAreLeftOut()
    {
        var stored = Stored();
        stored.ClientSecret = null;

        var shown = SecretManagerSettings.Shown(SecretManagerTypes.ByName(Type), stored, NullLogger.Instance);

        shown.Value!.ContainsKey("ClientSecret").ShouldBeTrue();
        shown.Value["ClientSecret"].ShouldBeNull();
        shown.Value.ContainsKey("Name").ShouldBeFalse();
        shown.Value.ContainsKey("Implementation").ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AnUpdateThatSendsTheMaskKeepsTheStoredSecret()
    {
        var read = SecretManagerSettings.Read(
            Type, Json("{\"ClientSecret\":\"" + ConnectionSecretMask.Value + "\",\"Prefix\":\"new-\"}"), Stored(), NullLogger.Instance);

        read.IsSuccess.ShouldBeTrue();
        var changed = read.Value.ShouldBeOfType<SyntheticSecretManagerConfiguration>();
        changed.ClientSecret.ShouldBe("s3cr3t");
        changed.Prefix.ShouldBe("new-");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AnUpdateThatSendsANewSecretReplacesIt()
    {
        var read = SecretManagerSettings.Read(Type, Json("{\"ClientSecret\":\"fresh\"}"), Stored(), NullLogger.Instance);

        read.Value.ShouldBeOfType<SyntheticSecretManagerConfiguration>().ClientSecret.ShouldBe("fresh");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void TheTypeListAsksThePlatformAdministratorPolicyLikeEveryOtherSecretManagerRoute()
    {
        new Types(NullLogger<ListSecretManagerTypesEndpointBase>.Instance).Policy.ShouldBe(AuthConstants.PlatformAdminPolicy);
    }
}
