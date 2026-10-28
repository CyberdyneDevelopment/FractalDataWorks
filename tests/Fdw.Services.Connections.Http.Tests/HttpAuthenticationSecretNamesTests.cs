using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Connections.Http.Security;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Http.Tests;

/// <summary>
/// FDW-2844: every authentication method states the names of the secrets it reads, by the slot each travels in, so the connection factory
/// keeps no list of methods. A method that requires a secret and names none would be built with no credential.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class HttpAuthenticationSecretNamesTests
{
    // A method with every value it requires set, each to a name of its own so a slot that is read from the wrong value shows.
    private static HttpAuthenticationConfiguration Bound(string type)
    {
        var instance = HttpAuthenticationTypes.ByName(type).CreateInstance();
        instance.BindFromValues(instance.RequiredProperties.ToDictionary(property => property, property => (string?)$"S_{property}", StringComparer.OrdinalIgnoreCase));
        return instance;
    }

    [Theory]
    [InlineData("WsSecurity", "S_CertificateSecretName", null, null)]
    [InlineData("UsernameToken", null, "S_PasswordSecretName", null)]
    [InlineData("Basic", null, "S_PasswordSecretName", null)]
    [InlineData("ApiKey", null, null, "S_ApiKeySecretName")]
    [InlineData("Bearer", null, null, "S_TokenSecretName")]
    public void EachMethodNamesTheSecretsItReadsInTheSlotTheyTravelIn(string type, string? certificate, string? password, string? apiKey)
    {
        var names = Bound(type).SecretNames;

        names.CertificateSecretName.ShouldBe(certificate);
        names.PasswordSecretName.ShouldBe(password);
        names.ApiKeySecretName.ShouldBe(apiKey);
    }

    [Fact]
    public void NoSecurityReadsNoSecret()
    {
        Bound("None").SecretNames.ShouldBe(HttpSecretNames.None);
    }

    [Fact]
    public void EveryMethodThatRequiresASecretNamesAtLeastOneWhenItsRequiredValuesAreSet()
    {
        foreach (var method in HttpAuthenticationTypes.All())
        {
            var names = Bound(method.Name).SecretNames;

            if (method.RequiresSecret)
            {
                names.ShouldNotBe(HttpSecretNames.None, method.Name);
            }
            else
            {
                names.ShouldBe(HttpSecretNames.None, method.Name);
            }
        }
    }
}
