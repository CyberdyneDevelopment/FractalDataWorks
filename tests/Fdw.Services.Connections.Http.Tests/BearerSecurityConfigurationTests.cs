using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Connections.Http.Security;
using Fdw.Services.Connections.Http.Security.Types;
using Microsoft.Extensions.Configuration;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Http.Tests;

/// <summary>
/// TC-11: Bearer authentication was a placeholder that named no secret, so a connection had no way to
/// send a token at all. These tests pin the option's contract: it requires a secret, reads the name of
/// the secret holding its token from the connection's authentication values, and writes it back out.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class BearerSecurityConfigurationTests
{
    [Fact]
    public void BearerRequiresASecretManagerAndATokenSecret()
    {
        var bearer = HttpAuthenticationTypes.ByName("Bearer");

        bearer.RequiresSecret.ShouldBeTrue();
        string.Join(",", bearer.RequiredProperties).ShouldBe("SecretManagerName,TokenSecretName");
    }

    [Fact]
    public void BindFromValuesReadsTheTokenSecretName()
    {
        var bearer = new BearerSecurityConfiguration();

        bearer.BindFromValues(new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["SecretManagerName"] = "MsSqlSecrets",
            ["TokenSecretName"] = "YOUTRACK_TOKEN",
        });

        bearer.TokenSecretName.ShouldBe("YOUTRACK_TOKEN");
    }

    [Fact]
    public void BindReadsTheTokenSecretNameFromItsSection()
    {
        var section = new Mock<IConfigurationSection>();
        section.Setup(s => s["TokenSecretName"]).Returns("YOUTRACK_TOKEN");
        var bearer = new BearerSecurityConfiguration();

        bearer.Bind(section.Object);

        bearer.TokenSecretName.ShouldBe("YOUTRACK_TOKEN");
    }

    [Fact]
    public void AsKvpWritesTheTypeAndTheTokenSecretName()
    {
        var bearer = new BearerSecurityConfiguration { TokenSecretName = "YOUTRACK_TOKEN" };

        string.Join(";", bearer.AsKvp().Select(kv => $"{kv.Key}={kv.Value}"))
            .ShouldBe("Type=Bearer;TokenSecretName=YOUTRACK_TOKEN");
    }
}
