using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Workspaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Workspaces;

/// <summary>
/// FDW-4556: the workspaces package registers the default workspace maker from its own service type, so a host that references it and serves no workspace endpoint founds a
/// tenant's default workspace. A host without the package keeps the refusing stub (FDW-4178) that Fdw.Services.Users registers with TryAdd; the maker replaces it whichever of
/// the two registers first.
/// </summary>
public sealed class DefaultWorkspaceServiceTypeTests
{
    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    [InlineData(true)]
    [InlineData(false)]
    public void TheMakerReplacesTheRefusingStubWhicheverRegistersFirst(bool stubFirst)
    {
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        Doubles(builder.Services);
        if (stubFirst) builder.Services.TryAddScoped<IDefaultWorkspaceFounding, RefusingFounding>();

        new DefaultWorkspaceServiceType().Register(builder, NullLoggerFactory.Instance).IsSuccess.ShouldBeTrue();

        if (!stubFirst) builder.Services.TryAddScoped<IDefaultWorkspaceFounding, RefusingFounding>();
        using var provider = builder.Services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IDefaultWorkspaceFounding>().ShouldBeOfType<DefaultWorkspaceFounding>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task AHostWithThePackageAndNoWorkspaceEndpointFoundsATenantsDefaultWorkspace()
    {
        // The host holds the Users stub and the workspaces package's service type and nothing of the endpoint group; what founding resolves is what a tenant founding gets.
        var workspaces = new Mock<IWorkspaceConfigurationProvider>();
        var permissions = new Mock<IWorkspacePermissions>();
        WorkspaceImplementationConfiguration? saved = null;
        workspaces
            .Setup(w => w.Save(It.IsAny<WorkspaceImplementationConfiguration>(), "Workspace", "Workspace", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((WorkspaceImplementationConfiguration workspace, string _, string _, string _, CancellationToken _) =>
            {
                saved = workspace;
                return Task.FromResult(GenericResult.Success());
            });
        permissions.Setup(p => p.Create(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        Doubles(builder.Services);
        builder.Services.AddSingleton(workspaces.Object);
        builder.Services.AddScoped(_ => permissions.Object);
        builder.Services.TryAddScoped<IDefaultWorkspaceFounding, RefusingFounding>();
        new DefaultWorkspaceServiceType().Register(builder, NullLoggerFactory.Instance).IsSuccess.ShouldBeTrue();
        var request = new NewDefaultWorkspace(WorkspaceId: Guid.NewGuid(), TenantId: Guid.NewGuid(), OwnerName: "Beta", OrganizationId: Guid.NewGuid(), OwnerUserId: Guid.NewGuid());

        using var provider = builder.Services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var made = await scope.ServiceProvider.GetRequiredService<IDefaultWorkspaceFounding>().Found(request, TestContext.Current.CancellationToken);

        made.IsSuccess.ShouldBeTrue();
        made.Value.ShouldBe(request.WorkspaceId);
        saved.ShouldNotBeNull().Name.ShouldBe("Beta");
        permissions.Verify(p => p.Create(request.WorkspaceId, "Beta", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void ARegistrationRunTwiceLeavesOneProviderAndOneMaker()
    {
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        Doubles(builder.Services);
        var option = new DefaultWorkspaceServiceType();

        option.Register(builder, NullLoggerFactory.Instance).IsSuccess.ShouldBeTrue();
        option.Register(builder, NullLoggerFactory.Instance).IsSuccess.ShouldBeTrue();

        using var provider = builder.Services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IWorkspaceConfigurationProvider>()
            .ShouldBeSameAs(scope.ServiceProvider.GetRequiredService<WorkspaceConfigurationProvider>());
        scope.ServiceProvider.GetRequiredService<IWorkspacePermissions>().ShouldBeOfType<WorkspacePermissions>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void AHostWithoutThePackageRegistrationKeepsTheRefusingStub()
    {
        var services = new ServiceCollection();
        services.TryAddScoped<IDefaultWorkspaceFounding, RefusingFounding>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IDefaultWorkspaceFounding>().ShouldBeOfType<RefusingFounding>();
    }

    // What a founding host holds besides the workspaces package: the gateway provider, the authorization providers and the ambient authentication context.
    private static void Doubles(IServiceCollection services)
    {
        services.AddLogging();
        services.AddSingleton(Mock.Of<IConfigurationGatewayProvider>());
        services.AddSingleton(Mock.Of<IPermissionConfigurationProvider>());
        services.AddSingleton(Mock.Of<IRolePermissionConfigurationProvider>());
        services.AddSingleton(Mock.Of<IAuthenticationContextAccessor>());
    }

    // Stands for Fdw.Services.Users' UnavailableDefaultWorkspaceFounding, which this project does not reference.
    private sealed class RefusingFounding : IDefaultWorkspaceFounding
    {
        public Task<IGenericResult<Guid>> Found(NewDefaultWorkspace workspace, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IGenericResult> Remove(NewDefaultWorkspace workspace, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
