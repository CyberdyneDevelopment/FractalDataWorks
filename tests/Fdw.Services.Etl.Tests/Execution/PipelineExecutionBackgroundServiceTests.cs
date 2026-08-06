using System;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// Tests for <see cref="PipelineExecutionBackgroundService.EstablishWorkAuthenticationContext"/> —
/// the tenant-isolation seam that stamps a background execution's per-run DI scope with a
/// <see cref="WorkAuthenticationContext"/> carrying the execution's TenantId, so RLS SESSION_CONTEXT is
/// set for connections created within that scope.
/// </summary>
public sealed class PipelineExecutionBackgroundServiceTests
{
    private static PipelineExecutionBackgroundService CreateSut() =>
        new(new PipelineExecutionQueue(), Mock.Of<IServiceScopeFactory>(),
            NullLogger<PipelineExecutionBackgroundService>.Instance);

    private static PipelineExecutionRequest CreateRequest(Guid? tenantId) => new()
    {
        ExecutionId = Guid.NewGuid(),
        PipelineName = "TestPipeline",
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

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void DoesNotOverwriteAnAlreadyEstablishedContext()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IAuthenticationContextAccessor, AuthenticationContextAccessor>();
        var provider = services.BuildServiceProvider();
        var accessor = provider.GetRequiredService<IAuthenticationContextAccessor>();
        var existingTenantId = Guid.NewGuid();
        var existing = new WorkAuthenticationContext(existingTenantId);
        accessor.Current = existing;
        var request = CreateRequest(Guid.NewGuid());
        var sut = CreateSut();

        // Act
        sut.EstablishWorkAuthenticationContext(provider, request);

        // Assert
        accessor.Current.ShouldBeSameAs(existing);
        accessor.Current!.ActiveTenantId.ShouldBe(existingTenantId);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Security")]
    public void DoesNotThrowWhenAccessorIsNotRegistered()
    {
        // Arrange
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var request = CreateRequest(Guid.NewGuid());
        var sut = CreateSut();

        // Act / Assert — no accessor registered (Connections.MsSql not loaded) must be a safe no-op.
        Should.NotThrow(() => sut.EstablishWorkAuthenticationContext(provider, request));
    }
}
