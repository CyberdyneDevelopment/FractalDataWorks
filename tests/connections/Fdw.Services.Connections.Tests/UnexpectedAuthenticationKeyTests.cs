using System.Collections.Generic;
using Fdw.Services.Connections.MsSql.Authentication;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Tests;

/// <summary>
/// FDW-4607: a connection's authentication may hold only the keys its authentication type declares. A key outside them
/// (a hand-placed Password) is named so a save can refuse it, and is never shown by a read.
/// </summary>
public sealed class UnexpectedAuthenticationKeyTests
{
    [Fact]
    public void TheKeysAMethodDeclaresAreExpected()
    {
        var values = new Dictionary<string, string?>
        {
            ["Username"] = "app",
            ["SecretManagerName"] = "CompanyVault",
            ["SecretKeyName"] = "prod-sql-password",
        };

        MsSqlAuthenticationTypes.ByName("SqlAuth").UnexpectedProperties(values.Keys).ShouldBeEmpty();
    }

    [Fact]
    public void AKeyTheMethodDoesNotDeclareIsNamedAndOnlyThatKey()
    {
        var values = new Dictionary<string, string?>
        {
            ["Username"] = "app",
            ["SecretKeyName"] = "prod-sql-password",
            ["Password"] = "not-a-real-password",
        };

        MsSqlAuthenticationTypes.ByName("SqlAuth").UnexpectedProperties(values.Keys).ShouldBe(["Password"]);
    }

    [Fact]
    public void KeysAreComparedWithoutRegardToCase()
    {
        var values = new Dictionary<string, string?> { ["USERNAME"] = "app", ["secretkeyname"] = "prod-sql-password" };

        MsSqlAuthenticationTypes.ByName("SqlAuth").UnexpectedProperties(values.Keys).ShouldBeEmpty();
    }

    [Fact]
    public void AnUnrecognisedMethodExpectsNoKey()
    {
        var values = new Dictionary<string, string?> { ["Username"] = "app" };

        MsSqlAuthenticationTypes.ByName("NoSuchMethod").UnexpectedProperties(values.Keys).ShouldBe(["Username"]);
    }

    [Fact]
    public void AMethodWithNoKeysExpectsNoKey()
    {
        var values = new Dictionary<string, string?> { ["Password"] = "not-a-real-password" };

        MsSqlAuthenticationTypes.ByName("WindowsAuth").UnexpectedProperties(values.Keys).ShouldBe(["Password"]);
    }

    [Fact]
    public void AStoredKeyTheMethodDoesNotDeclareIsMaskedWhenShown()
    {
        var values = new Dictionary<string, string?>
        {
            ["Username"] = "app",
            ["SecretManagerName"] = "CompanyVault",
            ["SecretKeyName"] = "prod-sql-password",
            ["Password"] = "not-a-real-password",
        };

        var masked = MsSqlAuthenticationTypes.ByName("SqlAuth").MaskSecrets(values, "********");

        masked["Password"].ShouldBe("********");
        masked["Username"].ShouldBe("app");
        masked["SecretManagerName"].ShouldBe("CompanyVault");
        masked["SecretKeyName"].ShouldBe("********");
    }

    [Fact]
    public void AnEmptyUnexpectedValueIsShownEmpty()
    {
        var values = new Dictionary<string, string?> { ["Username"] = "app", ["Password"] = string.Empty };

        var masked = MsSqlAuthenticationTypes.ByName("SqlAuth").MaskSecrets(values, "********");

        masked["Password"].ShouldBe(string.Empty);
    }
}
