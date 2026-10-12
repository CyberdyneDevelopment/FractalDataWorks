using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Connections.Abstractions.Authentication;
using Fdw.Services.Connections.Endpoints;
using Fdw.Services.Connections.MsSql.Authentication;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Tests;

/// <summary>
/// FDW-1090: every authentication method declares the credentials it asks for with the label a person is asked for,
/// and the capabilities and connection detail carry those declarations, never a label derived from the field name.
/// </summary>
public sealed class CredentialFieldDeclarationTests
{
    [Fact]
    public void SqlAuthAsksForAHiddenPassword()
    {
        var credential = MsSqlAuthenticationTypes.ByName("SqlAuth").Credentials.ShouldHaveSingleItem();

        credential.Field.ShouldBe("SecretKeyName");
        credential.Label.ShouldBe("Password");
        credential.Secret.ShouldBeTrue();
    }

    [Fact]
    public void CredentialFieldsAreTheDeclaredFields()
    {
        foreach (var method in MsSqlAuthenticationTypes.All())
            method.CredentialFields.ShouldBe(method.Credentials.Select(credential => credential.Field).ToList());
    }

    [Fact]
    public void EveryDeclaredCredentialHasALabelThatIsNotItsFieldName()
    {
        foreach (var credential in MsSqlAuthenticationTypes.All().SelectMany(method => method.Credentials))
        {
            credential.Label.ShouldNotBeNullOrWhiteSpace();
            credential.Label.ShouldNotBe(credential.Field);
        }
    }

    [Fact]
    public void AMethodIsDescribedFromItsOwnDeclarations()
    {
        var sqlAuth = ConnectionAuthenticationMethod.Of(MsSqlAuthenticationTypes.ByName("SqlAuth"));
        var windows = ConnectionAuthenticationMethod.Of(MsSqlAuthenticationTypes.ByName("WindowsAuth"));

        sqlAuth.Name.ShouldBe("SqlAuth");
        sqlAuth.UsesHostIdentity.ShouldBeFalse();
        sqlAuth.Credentials.ShouldHaveSingleItem().Label.ShouldBe("Password");
        windows.UsesHostIdentity.ShouldBeTrue();
        windows.Credentials.ShouldBeEmpty();
    }

    [Fact]
    public void TheCapabilityShapeCarriesLabelsAndMarksEveryCredentialRequired()
    {
        var info = ConnectionAuthenticationTypeInfo.Of(new ConnectionAuthenticationMethod(
            "Basic", "HTTP Basic", usesHostIdentity: false,
            [new ConnectionCredentialField("UsernameSecretName", "User name", secret: false),
             new ConnectionCredentialField("PasswordSecretName", "Password", secret: true)]));

        info.Name.ShouldBe("Basic");
        info.DisplayName.ShouldBe("HTTP Basic");
        info.UsesHostIdentity.ShouldBeFalse();
        info.CredentialFields.Select(field => (field.Field, field.Label, field.Required, field.Secret)).ShouldBe(
        [
            ("UsernameSecretName", "User name", true, false),
            ("PasswordSecretName", "Password", true, true),
        ]);
    }

    [Fact]
    public void SlotsCarryTheDeclaredCredentials()
    {
        var slots = ConnectionCredentialSlots.From(
            "SqlAuth",
            MsSqlAuthenticationTypes.ByName("SqlAuth").Credentials,
            new Dictionary<string, string?>());

        slots.Credentials.ShouldHaveSingleItem().Label.ShouldBe("Password");
        slots.Fields.ShouldBe(["SecretKeyName"]);
    }

    [Theory]
    [InlineData("", "Password")]
    [InlineData("SecretKeyName", "")]
    public void ACredentialWithoutAFieldOrLabelIsRefused(string field, string label)
        => Should.Throw<System.ArgumentException>(() => new ConnectionCredentialField(field, label, secret: true));
}
