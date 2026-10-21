using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Configuration;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;

namespace Fdw.Services.Connections.Tests;

/// <summary>
/// Tests for <see cref="ConnectionGrants"/> and <see cref="ConnectionAssignments"/> (FDW-1581): a tenant cannot assign a connection it was
/// not granted, a base folder cannot climb out of the grant, a grant in use is kept, and a refusal writes nothing.
/// </summary>
public sealed class ConnectionGrantsAndAssignmentsTests
{
    private static readonly Guid Rayburn = new("11111111-0000-0000-0000-000000000003");
    private static readonly Guid Acme = new("11111111-0000-0000-0000-000000000004");
    private static readonly Guid Finance = new("44444444-0000-0000-0000-000000000001");
    private static readonly Guid GrantId = new("55555555-0000-0000-0000-000000000001");

    private static GenericMessage Fail(string reason) => new(reason);

    private sealed class Rig
    {
        public Mock<IOrganizationScope> Scope { get; } = new();
        public Mock<IConnectionGrantConfigurationProvider> Grants { get; } = new();
        public Mock<IConnectionAssignmentConfigurationProvider> Assignments { get; } = new();
        public Mock<IDocumentConnectionProvider> Connections { get; } = new();
        public Mock<ITenantProvider> Tenants { get; } = new();
        public List<ConnectionGrantImplementationConfiguration> Granted { get; } = [];
        public List<ConnectionAssignmentImplementationConfiguration> Assigned { get; } = [];

        public Rig()
        {
            Scope.Setup(s => s.TenantOf(Finance, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<Guid>.Success(Rayburn));
            Grants.Setup(g => g.Get(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => GenericResult<IReadOnlyList<IConnectionGrantImplementationConfiguration>>.Success([.. Granted]));
            Grants.Setup(g => g.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns((Guid id, CancellationToken _) => Task.FromResult(Granted.FirstOrDefault(grant => grant.Id == id) is { } found
                    ? GenericResult<IConnectionGrantImplementationConfiguration>.Success(found)
                    : GenericResult<IConnectionGrantImplementationConfiguration>.Failure(Fail("not found"))));
            Grants.Setup(g => g.Save(It.IsAny<ConnectionGrantImplementationConfiguration>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback((ConnectionGrantImplementationConfiguration saved, string _, string _, string _, CancellationToken _) =>
                {
                    saved.Id = GrantId;
                    Granted.Add(saved);
                })
                .ReturnsAsync(GenericResult.Success());
            Grants.Setup(g => g.Delete(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
            Assignments.Setup(a => a.Get(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => GenericResult<IReadOnlyList<IConnectionAssignmentImplementationConfiguration>>.Success([.. Assigned]));
            Assignments.Setup(a => a.Find(It.IsAny<Func<ConnectionAssignmentImplementationConfiguration, bool>>(), It.IsAny<CancellationToken>()))
                .Returns((Func<ConnectionAssignmentImplementationConfiguration, bool> predicate, CancellationToken _) =>
                    Task.FromResult(GenericResult<IReadOnlyList<ConnectionAssignmentImplementationConfiguration>>.Success([.. Assigned.Where(predicate)])));
            Assignments.Setup(a => a.Save(It.IsAny<ConnectionAssignmentImplementationConfiguration>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult.Success());
            Assignments.Setup(a => a.Delete(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
            Connections.Setup(c => c.Get("rayburn-files", It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IDocumentConnection>.Success(Mock.Of<IDocumentConnection>()));
            Connections.Setup(c => c.Get("elsewhere", It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IDocumentConnection>.Failure(Fail("not a document connection")));
            Tenants.Setup(t => t.GetTenant(Rayburn, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<ITenant>.Success(Mock.Of<ITenant>()));
            Tenants.Setup(t => t.GetTenant(Acme, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<ITenant>.Failure(Fail("no such tenant")));
        }

        public ConnectionGrants GrantsService() =>
            new(Grants.Object, Assignments.Object, Connections.Object, Tenants.Object, NullLogger<ConnectionGrants>.Instance);

        public ConnectionAssignments AssignmentsService() =>
            new(Scope.Object, Assignments.Object, Grants.Object, NullLogger<ConnectionAssignments>.Instance);

        public Rig GrantHeld(Guid tenant, string purpose = "Documents")
        {
            Granted.Add(new ConnectionGrantImplementationConfiguration
            {
                Id = GrantId, Name = $"{tenant}:{purpose}:rayburn-files", GranteeTenantId = tenant, ConnectionName = "rayburn-files", Purpose = purpose, FolderPath = "rayburn",
            });
            return this;
        }

        public void NothingGranted() =>
            Grants.Verify(g => g.Save(It.IsAny<ConnectionGrantImplementationConfiguration>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

        public void NothingAssigned() =>
            Assignments.Verify(a => a.Save(It.IsAny<ConnectionAssignmentImplementationConfiguration>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AConnectionIsGrantedToATenantForAPurposeWithAFolder()
    {
        var rig = new Rig();

        var result = await rig.GrantsService().Grant(Rayburn, "rayburn-files", "Documents", "rayburn", true, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.FolderPath.ShouldBe("rayburn");
        result.Value.MayRegisterOwn.ShouldBeTrue();
        result.Value.Id.ShouldBe(GrantId);
        rig.Grants.Verify(g => g.Save(
            It.Is<ConnectionGrantImplementationConfiguration>(c => c.GranteeTenantId == Rayburn && c.Purpose == "Documents" && c.FolderPath == "rayburn"),
            "ConnectionGrant", "ConnectionGrant", $"{Rayburn}:Documents:rayburn-files", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("rayburn-files", "Documents", "../legal")]
    [InlineData("", "Documents", null)]
    [InlineData("rayburn-files", "  ", null)]
    [InlineData("elsewhere", "Documents", null)]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AGrantWithABadFolderNoNameNoPurposeOrNoDocumentConnectionIsRefused(string connection, string purpose, string? folder)
    {
        var rig = new Rig();

        var result = await rig.GrantsService().Grant(Rayburn, connection, purpose, folder, false, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        rig.NothingGranted();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ATenantThatDoesNotExistAndADuplicateGrantAreRefused()
    {
        var rig = new Rig().GrantHeld(Rayburn);

        var noTenant = await rig.GrantsService().Grant(Acme, "rayburn-files", "Documents", null, false, TestContext.Current.CancellationToken);
        var duplicate = await rig.GrantsService().Grant(Rayburn, "rayburn-files", "DOCUMENTS", null, false, TestContext.Current.CancellationToken);

        noTenant.IsSuccess.ShouldBeFalse();
        duplicate.IsSuccess.ShouldBeFalse();
        rig.NothingGranted();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AGrantInUseIsKeptAndAFreeOneIsTakenBack()
    {
        var rig = new Rig().GrantHeld(Rayburn);
        rig.Assigned.Add(new ConnectionAssignmentImplementationConfiguration { OrganizationId = Finance, Purpose = "Documents", GrantId = GrantId });

        var kept = await rig.GrantsService().Revoke(Rayburn, "rayburn-files", "Documents", TestContext.Current.CancellationToken);
        rig.Assigned.Clear();
        var taken = await rig.GrantsService().Revoke(Rayburn, "rayburn-files", "Documents", TestContext.Current.CancellationToken);

        kept.IsSuccess.ShouldBeFalse();
        taken.IsSuccess.ShouldBeTrue();
        rig.Grants.Verify(g => g.Delete($"{Rayburn}:Documents:rayburn-files", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ATenantsOrganizationIsAssignedItsTenantsGrantWithABaseFolder()
    {
        var rig = new Rig().GrantHeld(Rayburn);

        var result = await rig.AssignmentsService().Assign(Finance, "Documents", GrantId, "finance", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.ConnectionName.ShouldBe("rayburn-files");
        result.Value.FolderPath.ShouldBe("rayburn");
        result.Value.BaseFolder.ShouldBe("finance");
        rig.Assignments.Verify(a => a.Save(
            It.Is<ConnectionAssignmentImplementationConfiguration>(c => c.OrganizationId == Finance && c.GrantId == GrantId && c.BaseFolder == "finance"),
            "ConnectionAssignment", "ConnectionAssignment", $"{Finance}:Documents", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ATenantCannotAssignAConnectionItWasNotGranted()
    {
        var rig = new Rig().GrantHeld(Acme);

        var result = await rig.AssignmentsService().Assign(Finance, "Documents", GrantId, null, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        rig.NothingAssigned();
    }

    [Theory]
    [InlineData("../legal")]
    [InlineData("finance/../../legal")]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ABaseFolderThatClimbsOutOfTheGrantIsRefused(string baseFolder)
    {
        var rig = new Rig().GrantHeld(Rayburn);

        var result = await rig.AssignmentsService().Assign(Finance, "Documents", GrantId, baseFolder, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        rig.NothingAssigned();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AGrantForAnotherPurposeAnUnknownGrantAndAnOrganizationTheCallerMayNotAdministerAreRefused()
    {
        var rig = new Rig().GrantHeld(Rayburn, "Images");
        var legal = new Guid("44444444-0000-0000-0000-000000000002");
        rig.Scope.Setup(s => s.TenantOf(legal, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<Guid>.Failure(Fail("not acting in Legal")));

        var purpose = await rig.AssignmentsService().Assign(Finance, "Documents", GrantId, null, TestContext.Current.CancellationToken);
        var unknown = await rig.AssignmentsService().Assign(Finance, "Images", Guid.NewGuid(), null, TestContext.Current.CancellationToken);
        var notYours = await rig.AssignmentsService().Assign(legal, "Images", GrantId, null, TestContext.Current.CancellationToken);

        purpose.IsSuccess.ShouldBeFalse();
        unknown.IsSuccess.ShouldBeFalse();
        notYours.IsSuccess.ShouldBeFalse();
        rig.NothingAssigned();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task ASecondAssignmentForThePurposeReplacesTheFirstUnderItsStoredName()
    {
        var rig = new Rig().GrantHeld(Rayburn);
        rig.Assigned.Add(new ConnectionAssignmentImplementationConfiguration { Name = $"{Finance}:documents", OrganizationId = Finance, Purpose = "documents", GrantId = GrantId, BaseFolder = "old" });

        await rig.AssignmentsService().Assign(Finance, "Documents", GrantId, "finance", TestContext.Current.CancellationToken);

        rig.Assignments.Verify(a => a.Save(It.IsAny<ConnectionAssignmentImplementationConfiguration>(), "ConnectionAssignment", "ConnectionAssignment", $"{Finance}:documents", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task AnAssignmentIsTakenAwayAndOneThatIsNotThereIsReported()
    {
        var rig = new Rig().GrantHeld(Rayburn);
        rig.Assigned.Add(new ConnectionAssignmentImplementationConfiguration { Name = $"{Finance}:Documents", OrganizationId = Finance, Purpose = "Documents", GrantId = GrantId, BaseFolder = "finance" });

        var removed = await rig.AssignmentsService().Unassign(Finance, "Documents", TestContext.Current.CancellationToken);
        rig.Assigned.Clear();
        var missing = await rig.AssignmentsService().Unassign(Finance, "Documents", TestContext.Current.CancellationToken);

        removed.IsSuccess.ShouldBeTrue();
        removed.Value!.BaseFolder.ShouldBe("finance");
        missing.IsSuccess.ShouldBeFalse();
    }
}
