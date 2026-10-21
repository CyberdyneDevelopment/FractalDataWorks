using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Users.Configuration;
using Fdw.Services.Users.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;

namespace Fdw.Services.Users.Tests;

/// <summary>
/// Tests for <see cref="OrganizationFounding"/> (FDW-1758): an organization is made with a copy of another's roles and a first
/// administrator, nothing is made when the person, their membership or the organization to copy is missing, and what was made is removed
/// again when the roles or the administrator cannot be set.
/// </summary>
public sealed class OrganizationFoundingTests
{
    private static readonly Guid Tenant = new("11111111-0000-0000-0000-000000000001");
    private static readonly Guid OtherTenant = new("11111111-0000-0000-0000-000000000002");
    private static readonly Guid Administrator = new("22222222-0000-0000-0000-000000000001");
    private static readonly Guid Finance = new("44444444-0000-0000-0000-000000000001");
    private static readonly Guid Legal = new("44444444-0000-0000-0000-000000000009");
    private static readonly NewOrganization NewLegal = new(Tenant, "Legal", "legal");

    private static IGenericResult Failed(string reason) => GenericResult.Failure(new GenericMessage(reason));

    private sealed class Rig
    {
        public Mock<IOrganizationWriter> Writer { get; } = new();
        public Mock<IOrganizationProvider> Provider { get; } = new();
        public Mock<IOrganizationBootstrap> Bootstrap { get; } = new();
        public Mock<IUserConfigurationProvider> Users { get; } = new();
        public Mock<ITenantProvider> Tenants { get; } = new();

        public Rig()
        {
            Users.Setup(u => u.Get(Administrator, It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<IUserImplementationConfiguration>.Success(Mock.Of<IUserImplementationConfiguration>()));
            Tenants.Setup(t => t.ValidateTenantAccess(Tenant, Administrator.ToString(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<bool>.Success(true));
            Provider.Setup(p => p.Get(Finance, It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<OrganizationConfiguration>.Success(new OrganizationConfiguration { Id = Finance, TenantId = Tenant }));
            Writer.Setup(w => w.Create(It.IsAny<NewOrganization>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<Guid>.Success(Legal));
            Writer.Setup(w => w.Delete(Legal, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
            Bootstrap.Setup(b => b.CopyRoles(Tenant, Finance, Legal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<IReadOnlyList<OrganizationRoleView>>.Success([new OrganizationRoleView("Administrator", null, true, ["organizations:write"])]));
            Bootstrap.Setup(b => b.GiveRole(Tenant, Legal, Administrator, "Administrator", It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<OrganizationMemberChange>.Success(new OrganizationMemberChange(null, "Administrator")));
            Bootstrap.Setup(b => b.Clear(Tenant, Legal, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
        }

        public OrganizationFounding Founding()
            => new(Writer.Object, Provider.Object, Bootstrap.Object, Users.Object, Tenants.Object, NullLogger<OrganizationFounding>.Instance);

        public void NothingMade() => Writer.Verify(w => w.Create(It.IsAny<NewOrganization>(), It.IsAny<CancellationToken>()), Times.Never);

        public void MadeThenRemoved()
        {
            Bootstrap.Verify(b => b.Clear(Tenant, Legal, It.IsAny<CancellationToken>()), Times.Once);
            Writer.Verify(w => w.Delete(Legal, It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AnOrganizationIsMadeWithACopyOfTheRolesAndItsFirstAdministrator()
    {
        var rig = new Rig();

        var result = await rig.Founding().Found(NewLegal, Finance, Administrator, "Administrator", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(Legal);
        rig.Bootstrap.Verify(b => b.CopyRoles(Tenant, Finance, Legal, It.IsAny<CancellationToken>()), Times.Once);
        rig.Bootstrap.Verify(b => b.GiveRole(Tenant, Legal, Administrator, "Administrator", It.IsAny<CancellationToken>()), Times.Once);
        rig.Writer.Verify(w => w.Delete(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task APersonWithNoAccountMakesNothing()
    {
        var rig = new Rig();
        rig.Users.Setup(u => u.Get(Administrator, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IUserImplementationConfiguration>.Failure(new GenericMessage("not found")));

        var result = await rig.Founding().Found(NewLegal, Finance, Administrator, "Administrator", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        rig.NothingMade();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task APersonWhoMayNotActInTheTenantMakesNothing()
    {
        var rig = new Rig();
        rig.Tenants.Setup(t => t.ValidateTenantAccess(Tenant, Administrator.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<bool>.Success(false));

        var result = await rig.Founding().Found(NewLegal, Finance, Administrator, "Administrator", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        rig.NothingMade();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AnOrganizationToCopyThatDoesNotExistMakesNothing()
    {
        var rig = new Rig();
        rig.Provider.Setup(p => p.Get(Finance, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<OrganizationConfiguration>.Failure(new GenericMessage("not found")));

        var result = await rig.Founding().Found(NewLegal, Finance, Administrator, "Administrator", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        rig.NothingMade();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AnOrganizationToCopyFromAnotherTenantMakesNothing()
    {
        var rig = new Rig();
        rig.Provider.Setup(p => p.Get(Finance, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<OrganizationConfiguration>.Success(new OrganizationConfiguration { Id = Finance, TenantId = OtherTenant }));

        var result = await rig.Founding().Found(NewLegal, Finance, Administrator, "Administrator", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        rig.NothingMade();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AWriterThatRefusesMakesNothingElse()
    {
        var rig = new Rig();
        rig.Writer.Setup(w => w.Create(It.IsAny<NewOrganization>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<Guid>.Failure(new GenericMessage("the slug is taken")));

        var result = await rig.Founding().Found(NewLegal, Finance, Administrator, "Administrator", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        rig.Bootstrap.Verify(b => b.CopyRoles(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task RolesThatCannotBeCopiedRemoveTheOrganizationAgain()
    {
        var rig = new Rig();
        rig.Bootstrap.Setup(b => b.CopyRoles(Tenant, Finance, Legal, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<OrganizationRoleView>>.Failure(new GenericMessage("the caller does not hold organizations:write")));

        var result = await rig.Founding().Found(NewLegal, Finance, Administrator, "Administrator", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        rig.MadeThenRemoved();
        rig.Bootstrap.Verify(b => b.GiveRole(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ARoleThatCannotBeGivenRemovesTheOrganizationAgain()
    {
        var rig = new Rig();
        rig.Bootstrap.Setup(b => b.GiveRole(Tenant, Legal, Administrator, "Administrator", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<OrganizationMemberChange>.Failure(new GenericMessage("no such role")));

        var result = await rig.Founding().Found(NewLegal, Finance, Administrator, "Administrator", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        rig.MadeThenRemoved();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task AFailedRemovalDoesNotStopTheNextOne()
    {
        var rig = new Rig();
        rig.Bootstrap.Setup(b => b.GiveRole(Tenant, Legal, Administrator, "Administrator", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<OrganizationMemberChange>.Failure(new GenericMessage("no such role")));
        rig.Bootstrap.Setup(b => b.Clear(Tenant, Legal, It.IsAny<CancellationToken>())).ReturnsAsync(Failed("the store is down"));

        var result = await rig.Founding().Found(NewLegal, Finance, Administrator, "Administrator", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        rig.Writer.Verify(w => w.Delete(Legal, It.IsAny<CancellationToken>()), Times.Once);
    }
}
