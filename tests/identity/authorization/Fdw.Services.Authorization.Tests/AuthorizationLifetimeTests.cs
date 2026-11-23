using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Authorization.Tests;

public sealed class AuthorizationLifetimeTests
{
    [Fact]
    public void SingletonAuthorizationConsumersResolveWithScopeValidation()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton(Mock.Of<IConfigurationGatewayProvider>());
        // The role, permission and organization role providers write and read each description (FDW-6208); the Descriptions service type is not under test here.
        builder.Services.AddSingleton(Mock.Of<Fdw.Services.Descriptions.IDescriptionWriter>());
        builder.Services.AddSingleton(Mock.Of<Fdw.Services.Descriptions.Abstractions.IDescriptionReader>());
        new DefaultAuthorizationServiceType().Register(builder, null);
        using var services = builder.Services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
        });

        var orgAccess = services.GetRequiredService<IOrgAccessProvider>();
        services.GetRequiredService<IFrameworkAuthorizationService>().ShouldNotBeNull();
        services.GetRequiredService<IUserPermissions>().ShouldNotBeNull();
        using var scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IOrgAccessProvider>().ShouldBeSameAs(orgAccess);
    }
}
