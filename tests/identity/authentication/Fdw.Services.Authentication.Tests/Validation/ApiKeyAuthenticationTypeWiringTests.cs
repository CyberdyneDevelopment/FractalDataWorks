using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Validation;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Shouldly;

namespace Fdw.Services.Authentication.Tests.Validation;

/// <summary>
/// The ApiKey option has to finish wiring itself into the domain dispatcher the same way its LocalKey
/// and JwtBearer siblings do. Before this, <c>ApiKeyAuthenticationType</c> declared a Registration
/// phase but no Initialization phase, so nothing ever called
/// <see cref="IAuthenticationServiceConfigurationProvider"/>.Register("ApiKey", ...) — a domain row
/// naming the ApiKey kind could never be dispatched to, regardless of what the row itself carried.
/// </summary>
public sealed class ApiKeyAuthenticationTypeWiringTests
{
    [Fact]
    public void Registers_its_implementation_provider_with_the_domain_provider_on_initialize()
    {
        var domainProvider = new Mock<IAuthenticationServiceConfigurationProvider>();

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton(Mock.Of<IConfigurationGatewayProvider>());
        builder.Services.AddSingleton(domainProvider.Object);

        var sut = new ApiKeyAuthenticationType();
        sut.Register(builder, null);

        using var host = builder.Build();

        sut.Initialize(host, null);

        // The regression this guards against: before the fix, ApiKeyAuthenticationType declared no
        // Initialization phase at all, so this call never happened -- a domain row naming "ApiKey"
        // had no provider to dispatch to, regardless of what the row itself carried.
        domainProvider.Verify(
            p => p.Register("ApiKey", It.IsAny<IApiKeyAuthenticationConfigurationProvider>()),
            Times.Once);
    }

    [Fact]
    public void Resolves_its_own_configuration_provider_from_the_container()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton(Mock.Of<IConfigurationGatewayProvider>());

        new ApiKeyAuthenticationType().Register(builder, null);

        using var host = builder.Build();

        host.Services.GetService<IApiKeyAuthenticationConfigurationProvider>().ShouldNotBeNull();
    }
}
