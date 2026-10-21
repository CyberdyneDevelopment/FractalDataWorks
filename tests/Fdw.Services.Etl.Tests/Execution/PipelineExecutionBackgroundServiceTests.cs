using System;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// Tests for <see cref="PipelineExecutionBackgroundService.EstablishSystemAuthenticationContext"/> —
/// the seam that stamps a background execution's per-run DI scope with an authentication context,
/// so connections created within that scope resolve a SESSION_CONTEXT rather than the
/// deny-everywhere principal. Elevated rather than tenant-scoped by decision (FDW-767).
/// </summary>
[Collection(EtlOperationalConnectionCollection.Name)]
public sealed class PipelineExecutionBackgroundServiceTests
{
    private static PipelineExecutionBackgroundService CreateSut() =>
        new(new PipelineExecutionQueue(), new PipelineRunSlot(), Mock.Of<IServiceScopeFactory>(),
            NullLogger<PipelineExecutionBackgroundService>.Instance);

    private static PipelineExecutionRequest CreateRequest(Guid? tenantId) => new()
    {
        ExecutionId = Guid.NewGuid(),
        PipelineId = Guid.NewGuid(),
        PipelineName = "TestPipeline",
        TriggerSource = "Test",
        RunRoles = ["DataWriter"],
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
        var existing = new SystemAuthenticationContext(existingTenantId);
        accessor.Current = existing;
        var request = CreateRequest(Guid.NewGuid());
        var sut = CreateSut();

        // Act
        sut.EstablishSystemAuthenticationContext(provider, request);

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
        Should.NotThrow(() => sut.EstablishSystemAuthenticationContext(provider, request));
    }

    private static PipelineExecutionRequest RequestWithCap(int? rowsPerSource) => new()
    {
        ExecutionId = Guid.NewGuid(),
        PipelineId = Guid.NewGuid(),
        PipelineName = "TestPipeline",
        TriggerSource = "Test",
        RunRoles = ["DataWriter"],
        TestRowsPerSource = rowsPerSource
    };

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void RequestWithARowCapRunsAsATestRunThatWritesNothing()
    {
        var options = PipelineExecutionBackgroundService.OptionsOf(RequestWithCap(5));

        options.IsTestMode.ShouldBeTrue();
        options.MaxRowsPerSource.ShouldBe(5);
        options.SkipDestinationWrites.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void RequestWithoutARowCapRunsForReal()
    {
        var options = PipelineExecutionBackgroundService.OptionsOf(RequestWithCap(null));

        options.IsTestMode.ShouldBeFalse();
        options.SkipDestinationWrites.ShouldBe(PipelineExecutionOptions.Production.SkipDestinationWrites);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void TestRunKeepsTheTenantExecutionAndRolesOfTheRequest()
    {
        var request = RequestWithCap(3);

        var options = PipelineExecutionBackgroundService.OptionsOf(request);

        options.ExecutionId.ShouldBe(request.ExecutionId);
        options.RunRoles.ShouldBe(request.RunRoles);
    }
}
