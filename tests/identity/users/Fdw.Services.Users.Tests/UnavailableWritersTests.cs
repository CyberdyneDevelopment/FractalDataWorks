using System;
using System.Threading.Tasks;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Users.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Users.Tests;

/// <summary>
/// Tests for <see cref="UnavailableTenantWriter"/> and <see cref="UnavailableOrganizationWriter"/> (FDW-4180): a host without the SQL multitenancy
/// type starts and every write fails with a message, and that type's registration replaces them whichever registers first.
/// </summary>
public sealed class UnavailableWritersTests
{
    private static readonly Guid Id = new("11111111-0000-0000-0000-000000000001");

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Users")]
    public async Task EveryTenantWriteFails()
    {
        var writer = new UnavailableTenantWriter();

        (await writer.Create(new NewTenant("Beta", "beta", null, null, true, "Main", "main"))).IsFailure.ShouldBeTrue();
        (await writer.Update(Id, new TenantChange("Beta", true, null))).IsFailure.ShouldBeTrue();
        (await writer.Delete(Id)).IsFailure.ShouldBeTrue();
        (await writer.SetDeleted(Id, true)).IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Users")]
    public async Task EveryOrganizationWriteFails()
    {
        var writer = new UnavailableOrganizationWriter();

        (await writer.Create(new NewOrganization(Id, "Main", "main"))).IsFailure.ShouldBeTrue();
        (await writer.Update(Id, new OrganizationChange("Main", true))).IsFailure.ShouldBeTrue();
        (await writer.Delete(Id)).IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Users")]
    public void TheRefusingWritersResolveWhenNothingElseRegistersThem()
    {
        var services = new ServiceCollection();
        services.TryAddScoped<ITenantWriter, UnavailableTenantWriter>();
        services.TryAddScoped<IOrganizationWriter, UnavailableOrganizationWriter>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<ITenantWriter>().ShouldBeOfType<UnavailableTenantWriter>();
        scope.ServiceProvider.GetRequiredService<IOrganizationWriter>().ShouldBeOfType<UnavailableOrganizationWriter>();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Trait("Priority", "P0")]
    [Trait("Category", "Users")]
    public void TheRealWritersWinInEitherRegistrationOrder(bool usersRegistersFirst)
    {
        var tenants = new Mock<ITenantWriter>().Object;
        var organizations = new Mock<IOrganizationWriter>().Object;
        var services = new ServiceCollection();
        if (usersRegistersFirst)
        {
            services.TryAddScoped<ITenantWriter, UnavailableTenantWriter>();
            services.TryAddScoped<IOrganizationWriter, UnavailableOrganizationWriter>();
        }

        services.AddScoped<ITenantWriter>(_ => tenants);
        services.AddScoped<IOrganizationWriter>(_ => organizations);
        if (!usersRegistersFirst)
        {
            services.TryAddScoped<ITenantWriter, UnavailableTenantWriter>();
            services.TryAddScoped<IOrganizationWriter, UnavailableOrganizationWriter>();
        }

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<ITenantWriter>().ShouldBeSameAs(tenants);
        scope.ServiceProvider.GetRequiredService<IOrganizationWriter>().ShouldBeSameAs(organizations);
    }
}
