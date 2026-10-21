using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Messages;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Results;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Authorization.Endpoints;
using Fdw.Services.Users;
using Fdw.Services.Users.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;

namespace Fdw.Services.Authorization.Tests;

/// <summary>
/// Tests for the endpoints over an organization's roles and members (FDW-1593): each change is answered from the service's result,
/// recorded in the audit at the Organization level with the organization named, and a change that cannot be recorded answers 500.
/// </summary>
[Trait("Category", "Security")]
public sealed class OrganizationEndpointTests
{
    private static readonly Guid Finance = new("44444444-0000-0000-0000-000000000001");
    private static readonly Guid Jo = new("22222222-0000-0000-0000-000000000001");

    private sealed class CreateRole(IOrganizationRoles roles, IAdministrativeAudit audit) : CreateOrganizationRoleEndpointBase(NullLogger.Instance, roles, audit)
    {
        protected override void ConfigureEndpoint() => AllowAnonymous();
    }

    private sealed class DeleteRole(IOrganizationRoles roles, IAdministrativeAudit audit) : DeleteOrganizationRoleEndpointBase(NullLogger.Instance, roles, audit)
    {
        protected override void ConfigureEndpoint() => AllowAnonymous();
    }

    private sealed class GiveRole(IOrganizationMembers members, IUserConfigurationProvider users, IAdministrativeAudit audit)
        : GiveOrganizationMemberRoleEndpointBase(NullLogger.Instance, members, users, audit)
    {
        protected override void ConfigureEndpoint() => AllowAnonymous();
    }

    private sealed class RemoveMember(IOrganizationMembers members, IAdministrativeAudit audit) : RemoveOrganizationMemberEndpointBase(NullLogger.Instance, members, audit)
    {
        protected override void ConfigureEndpoint() => AllowAnonymous();
    }

    private static Action<DefaultHttpContext> Caller()
        => ctx => ctx.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", Guid.NewGuid().ToString())], "test"));

    private static Mock<IAdministrativeAudit> Audit(List<AdministrativeChangeEntry>? recorded = null, bool succeeds = true)
    {
        var audit = new Mock<IAdministrativeAudit>();
        audit.Setup(a => a.Record(It.IsAny<AdministrativeChangeEntry>(), It.IsAny<CancellationToken>()))
            .Callback((AdministrativeChangeEntry entry, CancellationToken _) => recorded?.Add(entry))
            .ReturnsAsync(succeeds ? GenericResult.Success() : GenericResult.Failure(new GenericMessage("the audit store is down")));
        return audit;
    }

    private static Mock<IUserConfigurationProvider> Users(bool exists)
    {
        var users = new Mock<IUserConfigurationProvider>();
        users.Setup(u => u.Get(Jo, It.IsAny<CancellationToken>())).ReturnsAsync(exists
            ? GenericResult<IUserImplementationConfiguration>.Success(Mock.Of<IUserImplementationConfiguration>())
            : GenericResult<IUserImplementationConfiguration>.Failure(new GenericMessage("not found")));
        return users;
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ADefinedRoleIsAnswered201AndRecordedAtTheOrganizationLevel()
    {
        var recorded = new List<AdministrativeChangeEntry>();
        var roles = new Mock<IOrganizationRoles>();
        roles.Setup(r => r.Define(Finance, "Clerk", "Uploads", It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<OrganizationRoleView>.Success(new OrganizationRoleView("Clerk", "Uploads", false, ["datasets:write"])));
        var endpoint = Factory.Create<CreateRole>(Caller(), roles.Object, Audit(recorded).Object);

        await endpoint.HandleAsync(new OrganizationRoleRequest { OrganizationId = Finance, RoleName = "Clerk", Description = "Uploads", Permissions = ["datasets:write"] }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(201);
        var entry = recorded.ShouldHaveSingleItem();
        entry.ChangeKind.ShouldBe("OrganizationRoleDefined");
        entry.OrganizationId.ShouldBe(Finance);
        entry.ItemId.ShouldBe($"{Finance}:Clerk");
        entry.Level.Name.ShouldBe("Organization");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ARoleTheServiceRefusesIsAnsweredFromItsFailureAndRecordsNothing()
    {
        var recorded = new List<AdministrativeChangeEntry>();
        var roles = new Mock<IOrganizationRoles>();
        roles.Setup(r => r.Define(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<OrganizationRoleView>.Failure(new GenericMessage("the caller does not hold datasets:delete")));
        var endpoint = Factory.Create<CreateRole>(Caller(), roles.Object, Audit(recorded).Object);

        await endpoint.HandleAsync(new OrganizationRoleRequest { OrganizationId = Finance, RoleName = "Clerk", Permissions = ["datasets:delete"] }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBeGreaterThanOrEqualTo(400);
        recorded.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ARoleDefinedButNotRecordedIsAnswered500()
    {
        var roles = new Mock<IOrganizationRoles>();
        roles.Setup(r => r.Define(Finance, "Clerk", null, It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<OrganizationRoleView>.Success(new OrganizationRoleView("Clerk", null, false, ["datasets:write"])));
        var endpoint = Factory.Create<CreateRole>(Caller(), roles.Object, Audit(succeeds: false).Object);

        await endpoint.HandleAsync(new OrganizationRoleRequest { OrganizationId = Finance, RoleName = "Clerk", Permissions = ["datasets:write"] }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(500);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ADeletedRoleIsAnswered204AndRecordedWithItsOldPermissions()
    {
        var recorded = new List<AdministrativeChangeEntry>();
        var roles = new Mock<IOrganizationRoles>();
        roles.Setup(r => r.Get(Finance, "Clerk", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<OrganizationRoleView>.Success(new OrganizationRoleView("Clerk", "Uploads", false, ["datasets:write"])));
        roles.Setup(r => r.Delete(Finance, "Clerk", It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
        var endpoint = Factory.Create<DeleteRole>(Caller(), roles.Object, Audit(recorded).Object);

        await endpoint.HandleAsync(new OrganizationRoleKeyRequest { OrganizationId = Finance, RoleName = "Clerk" }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(204);
        var entry = recorded.ShouldHaveSingleItem();
        entry.ChangeKind.ShouldBe("OrganizationRoleDeleted");
        entry.OldValue.ShouldNotBeNull().ShouldContain("datasets:write");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task APersonWithNoAccountIsNotFoundAndGivenNothing()
    {
        var members = new Mock<IOrganizationMembers>();
        var endpoint = Factory.Create<GiveRole>(Caller(), members.Object, Users(false).Object, Audit().Object);

        await endpoint.HandleAsync(new OrganizationMemberRequest { OrganizationId = Finance, UserId = Jo, RoleName = "Clerk" }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(404);
        members.Verify(m => m.Give(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(null, "OrganizationMemberAdded")]
    [InlineData("Observer", "OrganizationMemberRoleChanged")]
    [Trait("Priority", "P0")]
    public async Task APersonGivenARoleIsRecordedAsAddedOrChangedWithTheOldRole(string? previous, string expectedKind)
    {
        var recorded = new List<AdministrativeChangeEntry>();
        var members = new Mock<IOrganizationMembers>();
        members.Setup(m => m.Give(Finance, Jo, "Clerk", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<OrganizationMemberChange>.Success(new OrganizationMemberChange(previous, "Clerk")));
        var endpoint = Factory.Create<GiveRole>(Caller(), members.Object, Users(true).Object, Audit(recorded).Object);

        await endpoint.HandleAsync(new OrganizationMemberRequest { OrganizationId = Finance, UserId = Jo, RoleName = "Clerk" }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        var entry = recorded.ShouldHaveSingleItem();
        entry.ChangeKind.ShouldBe(expectedKind);
        entry.OldValue.ShouldBe(previous);
        entry.NewValue.ShouldBe("Clerk");
        entry.OrganizationId.ShouldBe(Finance);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ARemovedPersonIsAnswered204AndRecordedWithTheirOldRole()
    {
        var recorded = new List<AdministrativeChangeEntry>();
        var members = new Mock<IOrganizationMembers>();
        members.Setup(m => m.Remove(Finance, Jo, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<string>.Success("Clerk"));
        var endpoint = Factory.Create<RemoveMember>(Caller(), members.Object, Audit(recorded).Object);

        await endpoint.HandleAsync(new OrganizationMemberKeyRequest { OrganizationId = Finance, UserId = Jo }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(204);
        var entry = recorded.ShouldHaveSingleItem();
        entry.ChangeKind.ShouldBe("OrganizationMemberRemoved");
        entry.OldValue.ShouldBe("Clerk");
    }
}
