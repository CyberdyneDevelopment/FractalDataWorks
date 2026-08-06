using System;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Etl.Projects.Execution;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Fdw.Services.Etl.Projects.Tests.Execution;

/// <summary>
/// Tests for
/// <see cref="OrchestrationNodeOrchestratorBackgroundService.EstablishWorkAuthenticationContext"/> —
/// mirrors <c>PipelineExecutionBackgroundServiceTests</c> for the node-orchestration execution path.
/// </summary>
public sealed class OrchestrationNodeOrchestratorBackgroundServiceTests
{
    private static OrchestrationNodeOrchestratorBackgroundService CreateSut() =>
        new(new OrchestrationNodeExecutionQueue(), Mock.Of<IServiceScopeFactory>());

    private static OrchestrationNodeExecutionRequest CreateRequest(Guid? tenantId) => new()
    {
        ExecutionId = Guid.NewGuid(),
        RootNodeId = Guid.NewGuid(),
        TriggerSource = "Test",
        TenantId = tenantId
    };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ScopeExposesAuthenticationContextWithMatchingTenantId()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IAuthenticationContextAccessor, AuthenticationContextAccessor>();
        var provider = services.BuildServiceProvider();
        var tenantId = Guid.NewGuid();
        var request = CreateRequest(tenantId);
        var sut = CreateSut();

        // Act
        sut.EstablishWorkAuthenticationContext(provider, request);

        // Assert
        var accessor = provider.GetRequiredService<IAuthenticationContextAccessor>();
        accessor.Current.ShouldNotBeNull();
        accessor.Current!.ActiveTenantId.ShouldBe(tenantId);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void DoesNothingWhenRequestHasNoTenantId()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IAuthenticationContextAccessor, AuthenticationContextAccessor>();
        var provider = services.BuildServiceProvider();
        var request = CreateRequest(tenantId: null);
        var sut = CreateSut();

        // Act
        sut.EstablishWorkAuthenticationContext(provider, request);

        // Assert
        provider.GetRequiredService<IAuthenticationContextAccessor>().Current.ShouldBeNull();
    }
}
