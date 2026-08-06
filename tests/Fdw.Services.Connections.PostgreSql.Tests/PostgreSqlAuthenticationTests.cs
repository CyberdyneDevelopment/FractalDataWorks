using System.Collections.Generic;
using Fdw.Services.Connections.PostgreSql.Authentication;
using Fdw.Services.Connections.PostgreSql.Authentication.Types;
using Shouldly;

namespace Fdw.Services.Connections.PostgreSql.Tests;

/// <summary>
/// Tests for <see cref="PostgreSqlAuthenticationTypes"/>, <see cref="NonePostgreSqlAuthentication"/>,
/// and <see cref="PasswordPostgreSqlAuthentication"/>.
/// </summary>
public class PostgreSqlAuthenticationTests
{
    private static IReadOnlyDictionary<string, string?> Kvp(params (string Key, string? Value)[] pairs)
    {
        var dict = new Dictionary<string, string?>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var p in pairs) dict[p.Key] = p.Value;
        return dict;
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void NoneIsDiscoverableByName()
    {
        var type = PostgreSqlAuthenticationTypes.ByName("None");

        type.ShouldNotBeNull();
        type.Name.ShouldBe("None");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void PasswordIsDiscoverableByName()
    {
        var type = PostgreSqlAuthenticationTypes.ByName("Password");

        type.ShouldNotBeNull();
        type.Name.ShouldBe("Password");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void NoneBuildAuthFragmentReturnsEmptyString()
    {
        var auth = new NonePostgreSqlAuthentication();
        var result = auth.BuildAuthFragment(Kvp(), null);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(string.Empty);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void PasswordBuildAuthFragmentAppendsUsernameAndPassword()
    {
        var auth = new PasswordPostgreSqlAuthentication();
        var result = auth.BuildAuthFragment(Kvp(("Username", "testuser"), ("SecretKeyName", "db-password")), "test-password-123");

        result.IsSuccess.ShouldBeTrue();
        result.Value!.ShouldContain("Username=testuser;");
        result.Value!.ShouldContain("Password=test-password-123;");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void PasswordBuildAuthFragmentOmitsPasswordWhenNotResolved()
    {
        var auth = new PasswordPostgreSqlAuthentication();
        var result = auth.BuildAuthFragment(Kvp(("Username", "testuser"), ("SecretKeyName", "db-password")), null);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.ShouldContain("Username=testuser;");
        result.Value!.ShouldNotContain("Password");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void PasswordValidateFailsWhenUsernameIsMissing()
    {
        var auth = new PasswordPostgreSqlAuthentication();
        var result = auth.Validate(Kvp(("SecretKeyName", "db-password")));

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage!.ShouldContain("Username is required");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void PasswordValidateFailsWhenSecretKeyNameIsMissing()
    {
        var auth = new PasswordPostgreSqlAuthentication();
        var result = auth.Validate(Kvp(("Username", "testuser")));

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage!.ShouldContain("SecretKeyName is required");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void PasswordValidateSucceedsWhenAllRequiredPropertiesPresent()
    {
        var auth = new PasswordPostgreSqlAuthentication();
        var result = auth.Validate(Kvp(("Username", "testuser"), ("SecretKeyName", "db-password")));

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void PasswordRequiredPropertiesContainsUsernameAndSecretKeyName()
    {
        var type = PostgreSqlAuthenticationTypes.ByName("Password");

        type.RequiredProperties.ShouldContain("Username");
        type.RequiredProperties.ShouldContain("SecretKeyName");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void NoneRequiredPropertiesIsEmpty()
    {
        var type = PostgreSqlAuthenticationTypes.ByName("None");

        type.RequiredProperties.ShouldBeEmpty();
    }
}
