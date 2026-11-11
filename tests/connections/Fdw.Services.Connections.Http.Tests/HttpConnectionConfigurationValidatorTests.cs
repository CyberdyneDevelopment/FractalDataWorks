using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Connections.Http.Validation;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Http.Tests;

/// <summary>
/// FDW-5139: a stored HTTP connection comes back with no Lifetime (it has no column), so the write validator does not require one; it
/// used to refuse every update of every HTTP connection.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class HttpConnectionConfigurationValidatorTests
{
    [Fact]
    public void AConfigurationAsItIsStoredWithNoLifetimeIsValid()
    {
        var stored = new HttpConnectionConfiguration
        {
            Name = "Remote",
            BaseUrl = "https://remote.test/",
            Protocol = "Rest",
            AuthenticationType = "None",
            PagingStyle = "OffsetLimit",
        };

        var validated = new HttpConnectionConfigurationValidator().Validate(stored);

        validated.IsValid.ShouldBeTrue(string.Join("; ", validated.Errors.Select(error => error.ErrorMessage)));
    }

    [Fact]
    public void AConfigurationWithNoBaseUrlIsStillRefused()
    {
        var validated = new HttpConnectionConfigurationValidator().Validate(
            new HttpConnectionConfiguration { Protocol = "Rest", AuthenticationType = "None" });

        validated.IsValid.ShouldBeFalse();
    }
}
