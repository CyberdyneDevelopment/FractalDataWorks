using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authorization;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Workspaces;
using Fdw.Services.Workspaces.Results;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Workspaces;

/// <summary>
/// FDW-937: a workspace's own permissions are written in the seeded shape, only once, and removed with
/// their grants before the permission itself.
/// </summary>
[Collection(nameof(ServicesTestCollection))]
public class WorkspacePermissionsTests
{
    private static readonly Guid SalesId = Guid.Parse("0197a3c4-5b6d-7e8f-9a0b-1c2d3e4f5a6b");
    [Fact]
    public async Task CreateWritesViewAndQueryInTheSeededShape()
    {
        var permissions = new Mock<IPermissionConfigurationProvider>();
        permissions.Setup(p => p.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IPermissionImplementationConfiguration>.Success(null!));
        var saved = new List<PermissionImplementationConfiguration>();
        permissions.Setup(p => p.Save(Capture.In(saved), "Permission", "Permission", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        var result = await Service(permissions).Create(SalesId, "sales", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        saved.Select(p => p.Name).ShouldBe(new[] { "workspaces/0197a3c4-5b6d-7e8f-9a0b-1c2d3e4f5a6b:view", "workspaces/0197a3c4-5b6d-7e8f-9a0b-1c2d3e4f5a6b:query" });
        saved.ShouldAllBe(p => p.PermissionDomain == "workspaces" && p.Resource == "workspaces/0197a3c4-5b6d-7e8f-9a0b-1c2d3e4f5a6b"
                               && p.Scope == "tenant");
        saved.Select(p => p.Action).ShouldBe(new[] { "view", "query" });
        saved[0].DisplayName.ShouldBe("View workspace sales");
    }

    [Fact]
    public async Task CreateLeavesAnExistingPermissionAlone()
    {
        var permissions = new Mock<IPermissionConfigurationProvider>();
        permissions.Setup(p => p.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IPermissionImplementationConfiguration>.Success(Permission("workspaces/0197a3c4-5b6d-7e8f-9a0b-1c2d3e4f5a6b:view")));

        (await Service(permissions).Create(SalesId, "sales", TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();

        permissions.Verify(p => p.Save(It.IsAny<PermissionImplementationConfiguration>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateStopsAtTheFirstFailedWrite()
    {
        var permissions = new Mock<IPermissionConfigurationProvider>();
        permissions.Setup(p => p.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IPermissionImplementationConfiguration>.Success(null!));
        permissions.Setup(p => p.Save(It.IsAny<PermissionImplementationConfiguration>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(WorkspacesResultCodes.ByName("WorkspaceLoadReturnedNoValue")));

        (await Service(permissions).Create(SalesId, "sales", TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();

        permissions.Verify(p => p.Save(It.IsAny<PermissionImplementationConfiguration>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveDeletesEveryGrantOfThePermissionBeforeThePermission()
    {
        var view = Permission("workspaces/0197a3c4-5b6d-7e8f-9a0b-1c2d3e4f5a6b:view");
        var query = Permission("workspaces/0197a3c4-5b6d-7e8f-9a0b-1c2d3e4f5a6b:query");
        var permissions = new Mock<IPermissionConfigurationProvider>();
        permissions.Setup(p => p.Get("workspaces/0197a3c4-5b6d-7e8f-9a0b-1c2d3e4f5a6b:view", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IPermissionImplementationConfiguration>.Success(view));
        permissions.Setup(p => p.Get("workspaces/0197a3c4-5b6d-7e8f-9a0b-1c2d3e4f5a6b:query", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IPermissionImplementationConfiguration>.Success(query));

        var order = new List<string>();
        permissions.Setup(p => p.Delete(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Callback((Guid id, CancellationToken _) => order.Add($"permission:{id}"))
            .ReturnsAsync(GenericResult.Success());

        var grantOfView = Grant(view.Id);
        var grantOfSomethingElse = Grant(Guid.NewGuid());
        var grants = new Mock<IRolePermissionConfigurationProvider>();
        grants.Setup(g => g.Find(It.IsAny<Func<RolePermissionImplementationConfiguration, bool>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Func<RolePermissionImplementationConfiguration, bool> keep, CancellationToken _) =>
                GenericResult<IReadOnlyList<RolePermissionImplementationConfiguration>>.Success(
                    [.. new[] { grantOfView, grantOfSomethingElse }.Where(keep)]));
        grants.Setup(g => g.Delete(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Callback((Guid id, CancellationToken _) => order.Add($"grant:{id}"))
            .ReturnsAsync(GenericResult.Success());

        var result = await new WorkspacePermissions(permissions.Object, grants.Object).Remove(SalesId, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        order.ShouldBe(new[] { $"grant:{grantOfView.Id}", $"permission:{view.Id}", $"permission:{query.Id}" });
    }

    [Fact]
    public async Task RemoveOfPermissionsThatDoNotExistDeletesNothing()
    {
        var permissions = new Mock<IPermissionConfigurationProvider>();
        permissions.Setup(p => p.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IPermissionImplementationConfiguration>.Success(null!));

        (await Service(permissions).Remove(SalesId, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();

        permissions.Verify(p => p.Delete(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static WorkspacePermissions Service(Mock<IPermissionConfigurationProvider> permissions)
        => new(permissions.Object, new Mock<IRolePermissionConfigurationProvider>().Object);

    private static PermissionImplementationConfiguration Permission(string name)
        => new() { Id = Guid.NewGuid(), Name = name };

    private static RolePermissionImplementationConfiguration Grant(Guid permissionId)
        => new() { Id = Guid.NewGuid(), RoleId = Guid.NewGuid(), PermissionId = permissionId };
}
