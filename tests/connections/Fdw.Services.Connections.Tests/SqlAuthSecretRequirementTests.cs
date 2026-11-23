using System.Collections.Generic;
using Fdw.Services.Connections.MsSql.Authentication.Types;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Tests;

/// <summary>SqlAuth requires a secret, so a connection that names no key for it is refused with a coded result and never read as one that needs none (FDW-6360).</summary>
[Trait("Category", "SqlAuthSecret")]
public sealed class SqlAuthSecretRequirementTests
{
    private static Dictionary<string, string?> Values(string? key, string? manager)
        => new(System.StringComparer.OrdinalIgnoreCase) { ["Username"] = "login", ["SecretKeyName"] = key, ["SecretManagerName"] = manager };

    [Theory]
    [Trait("Priority", "P0")]
    [InlineData(null)]
    [InlineData("")]
    public void ASqlAuthConnectionWithNoSecretKeyIsRefusedAndNeverAnsweredAsNeedingNoSecret(string? key)
    {
        var requirement = new SqlAuthConfiguration().RequiredSecret(Values(key, "Bootstrap"));

        requirement.IsFailure.ShouldBeTrue();
        requirement.Code!.Name.ShouldBe("AuthenticationValidationFailed");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ASqlAuthConnectionThatNamesAKeyAndAManagerNamesBothInItsRequirement()
    {
        var requirement = new SqlAuthConfiguration().RequiredSecret(Values("PlatformPassword", "Bootstrap"));

        requirement.IsSuccess.ShouldBeTrue(requirement.CurrentMessage);
        (requirement.Value.ManagerName, requirement.Value.KeyName).ShouldBe(("Bootstrap", "PlatformPassword"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ASqlAuthConnectionThatNamesAKeyAndNoManagerIsRefused()
    {
        new SqlAuthConfiguration().RequiredSecret(Values("PlatformPassword", null)).IsFailure.ShouldBeTrue();
    }
}
