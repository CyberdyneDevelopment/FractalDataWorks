using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Results;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Authorization.Endpoints;
using Fdw.Services.TokenManagers.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Authorization.Tests;

/// <summary>
/// FDW-5222: deleting a role removes its assignments with it, so the endpoint reads who held the role before the delete. The holders'
/// sessions still carry the role's grants, and they are ended from that list after the delete, as they were before.
/// </summary>
public sealed class DeleteRoleEndpointTests
{
    private static readonly Guid RoleId = Guid.Parse("bbbbbbbb-0000-0000-0000-0000000000a1");
    private static readonly Guid Holder = Guid.Parse("0a1b2c3d-0000-0000-0000-00000000000a");

    private sealed class DeleteRole(IRoleConfigurationProvider roles, IUserRoleConfigurationProvider userRoles, ITokenRevocationStore revocations)
        : DeleteRoleEndpointBase(NullLogger.Instance, roles, userRoles, revocations)
    {
        protected override void ConfigureEndpoint()
        {
        }
    }

    private static Action<DefaultHttpContext> Caller()
        => ctx =>
        {
            ctx.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", Guid.NewGuid().ToString())], "test"));
            var guard = new Mock<IPlatformAdministratorGuard>();
            guard.Setup(g => g.CanDeleteRole(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
            var audit = new Mock<IAdministrativeAudit>();
            audit.Setup(a => a.Record(It.IsAny<AdministrativeChangeEntry>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
            ctx.AddTestServices(s =>
            {
                s.AddSingleton(guard.Object);
                s.AddSingleton(audit.Object);
            });
        };

    private static Mock<IRoleConfigurationProvider> Roles(List<string> steps, Action onDelete)
    {
        var role = new Mock<IRoleImplementationConfiguration>();
        role.SetupGet(r => r.Id).Returns(RoleId);
        role.SetupGet(r => r.Name).Returns("Analyst");
        var roles = new Mock<IRoleConfigurationProvider>();
        roles.Setup(r => r.Get("Analyst", It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IRoleImplementationConfiguration>.Success(role.Object));
        roles.Setup(r => r.Delete(RoleId, It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                steps.Add("delete");
                onDelete();
            })
            .ReturnsAsync(GenericResult.Success());
        return roles;
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task TheHoldersAreReadBeforeTheDeleteAndTheirSessionsEndAfterIt()
    {
        var steps = new List<string>();
        var held = new List<IUserRoleImplementationConfiguration>
        {
            new UserRoleImplementationConfiguration { Id = Guid.NewGuid(), UserId = Holder.ToString("D"), RoleId = RoleId },
        };
        var userRoles = new Mock<IUserRoleConfigurationProvider>();
        userRoles
            .Setup(p => p.Find(It.IsAny<Func<IUserRoleImplementationConfiguration, bool>>(), It.IsAny<CancellationToken>()))
            .Callback(() => steps.Add("holders"))
            .ReturnsAsync((Func<IUserRoleImplementationConfiguration, bool> match, CancellationToken _) =>
                GenericResult<IReadOnlyList<IUserRoleImplementationConfiguration>>.Success([.. held.Where(match)]));

        // The delete removes the assignments with the role, as the role provider does: read afterwards, nobody holds it.
        var roles = Roles(steps, held.Clear);
        var revocations = new Mock<ITokenRevocationStore>();
        revocations
            .Setup(r => r.RevokeAll(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .Callback((Guid _, DateTimeOffset _, CancellationToken _) => steps.Add("sessions"))
            .ReturnsAsync(GenericResult.Success());
        var endpoint = Factory.Create<DeleteRole>(Caller(), roles.Object, userRoles.Object, revocations.Object);

        await endpoint.HandleAsync(new GetRoleRequest { Name = "Analyst" }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(204);
        steps.ToArray().ShouldBe(new[] { "holders", "delete", "sessions" });
        revocations.Verify(r => r.RevokeAll(Holder, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task WhenTheHoldersCannotBeReadTheRoleIsNotDeleted()
    {
        var steps = new List<string>();
        var userRoles = new Mock<IUserRoleConfigurationProvider>();
        userRoles
            .Setup(p => p.Find(It.IsAny<Func<IUserRoleImplementationConfiguration, bool>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<IUserRoleImplementationConfiguration>>.Failure(new Fdw.Messages.GenericMessage("the assignments could not be read")));
        var roles = Roles(steps, () => { });
        var endpoint = Factory.Create<DeleteRole>(Caller(), roles.Object, userRoles.Object, Mock.Of<ITokenRevocationStore>());

        await endpoint.HandleAsync(new GetRoleRequest { Name = "Analyst" }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBeGreaterThanOrEqualTo(400);
        steps.ShouldBeEmpty();
    }
}
