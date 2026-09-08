using System;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Etl.Projects.Execution;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Fdw.Services.Etl.Projects.Tests.Execution;

/// <summary>
/// Tests for
/// <see cref="OrchestrationNodeOrchestratorBackgroundService.EstablishSystemAuthenticationContext"/> —
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
    public void ScopeIsElevatedToTheSystemContext()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IAuthenticationContextAccessor, AuthenticationContextAccessor>();
        var provider = services.BuildServiceProvider();
        var request = CreateRequest(Guid.NewGuid());
        var sut = CreateSut();

        // Act
        sut.EstablishSystemAuthenticationContext(provider, request);

        // Assert
        var accessor = provider.GetRequiredService<IAuthenticationContextAccessor>();
        accessor.Current.ShouldNotBeNull();
        accessor.Current!.IsSystemContext.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ElevatesEvenWhenRequestHasNoTenantId()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IAuthenticationContextAccessor, AuthenticationContextAccessor>();
        var provider = services.BuildServiceProvider();
        var request = CreateRequest(tenantId: null);
        var sut = CreateSut();

        // Act
        sut.EstablishSystemAuthenticationContext(provider, request);

        // Assert — a tenant-less execution must NOT be left on the deny principal: system
        // elevation does not consult TenantId, so its absence is not a reason to skip elevating.
        var accessor = provider.GetRequiredService<IAuthenticationContextAccessor>();
        accessor.Current.ShouldNotBeNull();
        accessor.Current!.IsSystemContext.ShouldBeTrue();
    }
}
