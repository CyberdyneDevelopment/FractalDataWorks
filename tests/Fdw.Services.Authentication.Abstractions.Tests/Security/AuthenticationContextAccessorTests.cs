using System;
using System.Threading.Tasks;
using Fdw.Services.Authentication.Abstractions.Security;

namespace Fdw.Services.Authentication.Abstractions.Tests.Security;

/// <summary>
/// Tests for <see cref="AuthenticationContextAccessor"/> — the AsyncLocal-backed
/// <see cref="IAuthenticationContextAccessor"/> that lets a DI Singleton (e.g. the connection factory)
/// see per-logical-call-flow ambient authentication context without a captive scoped dependency.
/// </summary>
public class AuthenticationContextAccessorTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void CurrentIsNullByDefault()
    {
        // Arrange
        var accessor = new AuthenticationContextAccessor();

        // Assert
        accessor.Current.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void CurrentReturnsWhatWasSet()
    {
        // Arrange
        var accessor = new AuthenticationContextAccessor();
        var context = new WorkAuthenticationContext(Guid.NewGuid());

        // Act
        accessor.Current = context;

        // Assert
        accessor.Current.ShouldBeSameAs(context);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ValueSetInOneAsyncFlowDoesNotLeakIntoASiblingFlow()
    {
        // Arrange
        var accessor = new AuthenticationContextAccessor();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        IAuthenticationContext? seenInFlowB = null;

        var flowA = Task.Run(async () =>
        {
            accessor.Current = new WorkAuthenticationContext(tenantA);
            await Task.Delay(50).ConfigureAwait(false);
        }, TestContext.Current.CancellationToken);

        var flowB = Task.Run(async () =>
        {
            await Task.Delay(10).ConfigureAwait(false);
            accessor.Current = new WorkAuthenticationContext(tenantB);
            await Task.Delay(50).ConfigureAwait(false);
            seenInFlowB = accessor.Current;
        }, TestContext.Current.CancellationToken);

        await Task.WhenAll(flowA, flowB);

        // Assert
        seenInFlowB.ShouldNotBeNull();
        seenInFlowB!.ActiveTenantId.ShouldBe(tenantB);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task ValueSetInsideAnAwaitedMethodDoesNotLeakBackToTheCallerAfterItReturns()
    {
        // Arrange
        var accessor = new AuthenticationContextAccessor();
        var tenantId = Guid.NewGuid();

        async Task SetInChildFlow()
        {
            accessor.Current = new WorkAuthenticationContext(tenantId);
            await Task.Yield();
        }

        await SetInChildFlow();

        // Assert
        accessor.Current.ShouldBeNull();
    }
}
