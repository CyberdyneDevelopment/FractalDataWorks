using System;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Workspaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Workspaces;

/// <summary>
/// FDW-3668: writes for a tenant's default workspace land in that tenant. A caller acting in another tenant is swapped for a tenant run
/// for the length of the block and put back after it; a caller already acting in the tenant is left alone.
/// </summary>
[Collection(nameof(ServicesTestCollection))]
public sealed class TenantActingTests
{
    private static readonly Guid CallerTenant = Guid.NewGuid();
    private static readonly Guid NewTenant = Guid.NewGuid();
    private static readonly Guid Owner = Guid.NewGuid();

    private sealed class Accessor : IAuthenticationContextAccessor
    {
        public IAuthenticationContext? Current { get; set; }
    }

    private static IAuthenticationContext ActingIn(Guid tenantId)
    {
        var caller = new Mock<IAuthenticationContext>();
        caller.SetupGet(c => c.ActiveTenantId).Returns(tenantId);
        return caller.Object;
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Workspaces")]
    public void ACallerInAnotherTenantIsSwappedForATenantRunAndPutBack()
    {
        var caller = ActingIn(CallerTenant);
        var accessor = new Accessor { Current = caller };

        using (TenantActing.In(accessor, NewTenant, Owner, NullLogger.Instance))
        {
            accessor.Current.ShouldBeOfType<TenantRunAuthenticationContext>();
            accessor.Current!.ActiveTenantId.ShouldBe(NewTenant);
        }

        accessor.Current.ShouldBeSameAs(caller);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Workspaces")]
    public void ACallerAlreadyInTheTenantIsLeftAlone()
    {
        var caller = ActingIn(NewTenant);
        var accessor = new Accessor { Current = caller };

        using (TenantActing.In(accessor, NewTenant, Owner, NullLogger.Instance))
        {
            accessor.Current.ShouldBeSameAs(caller);
        }

        accessor.Current.ShouldBeSameAs(caller);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Workspaces")]
    public void ACallerWithNoContextIsSwappedAndPutBackAsNone()
    {
        var accessor = new Accessor();

        using (TenantActing.In(accessor, NewTenant, Owner, NullLogger.Instance))
        {
            accessor.Current!.ActiveTenantId.ShouldBe(NewTenant);
        }

        accessor.Current.ShouldBeNull();
    }
}
