using System;
using System.Collections.Generic;
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
using Fdw.Services.Users;
using Fdw.Services.Users.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Authorization.Tests;

/// <summary>
/// FDW-5655: revoking a role from a user ends their sessions with a cut-off at the moment of the change, at full clock
/// precision and never rounded to the second, so a sign-in that follows it in the same second reads as after it.
/// </summary>
public sealed class RevokeUserRoleCutOffTests
{
    private static readonly Guid RoleId = Guid.Parse("bbbbbbbb-0000-0000-0000-0000000056a5");
    private static readonly Guid UserId = Guid.Parse("0a1b2c3d-0000-0000-0000-0000000056a5");
    private static readonly Guid TenantId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private sealed class RevokeUserRole(
        IRoleConfigurationProvider roles,
        IUserRoleConfigurationProvider userRoles,
        IUserConfigurationProvider users,
        ITokenRevocationStore revocations)
        : RevokeUserRoleEndpointBase(NullLogger.Instance, roles, userRoles, users, revocations)
    {
        protected override void ConfigureEndpoint()
        {
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task TheUsersCutOffIsTheMomentOfTheRevocationUnrounded()
    {
        var role = new Mock<IRoleImplementationConfiguration>();
        role.SetupGet(r => r.Id).Returns(RoleId);
        role.SetupGet(r => r.Name).Returns("Analyst");
        var roles = new Mock<IRoleConfigurationProvider>();
        roles.Setup(r => r.Get("Analyst", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IRoleImplementationConfiguration>.Success(role.Object));

        var user = new Mock<IUserImplementationConfiguration>();
        user.SetupGet(u => u.Id).Returns(UserId);
        var users = new Mock<IUserConfigurationProvider>();
        users.Setup(u => u.Get(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IUserImplementationConfiguration>.Success(user.Object));

        var assignment = new UserRoleImplementationConfiguration
        {
            Id = Guid.NewGuid(), UserId = UserId.ToString("D"), RoleId = RoleId, TenantId = TenantId,
        };
        var userRoles = new Mock<IUserRoleConfigurationProvider>();
        userRoles
            .Setup(p => p.Find(It.IsAny<Func<UserRoleImplementationConfiguration, bool>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<UserRoleImplementationConfiguration>>.Success([assignment]));
        userRoles.Setup(p => p.Delete(assignment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());

        var cutOffs = new List<DateTimeOffset>();
        var revocations = new Mock<ITokenRevocationStore>();
        revocations
            .Setup(r => r.RevokeAll(UserId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .Callback((Guid _, DateTimeOffset revokedAt, CancellationToken _) => cutOffs.Add(revokedAt))
            .ReturnsAsync(GenericResult.Success());

        var endpoint = Factory.Create<RevokeUserRole>(Caller(), roles.Object, userRoles.Object, users.Object, revocations.Object);

        var before = DateTimeOffset.UtcNow;
        await endpoint.HandleAsync(new RevokeRoleRequest { IdOrName = UserId.ToString("D"), RoleName = "Analyst" }, CancellationToken.None);
        var after = DateTimeOffset.UtcNow;

        cutOffs.Count.ShouldBe(1);
        cutOffs[0].ShouldBeInRange(before, after);
    }

    private static Action<DefaultHttpContext> Caller()
        => ctx =>
        {
            ctx.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("sub", Guid.NewGuid().ToString()),
                new Claim("tenantId", TenantId.ToString()),
            ], "test"));
            var grants = new Mock<IGrantRule>();
            grants.Setup(g => g.CanGive(It.IsAny<IReadOnlyList<string>>(), TenantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult.Success());
            var guard = new Mock<IPlatformAdministratorGuard>();
            guard.Setup(g => g.CanRemove(UserId, "Analyst", TenantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult.Success());
            var audit = new Mock<IAdministrativeAudit>();
            audit.Setup(a => a.Record(It.IsAny<AdministrativeChangeEntry>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult.Success());
            ctx.AddTestServices(s =>
            {
                s.AddSingleton(grants.Object);
                s.AddSingleton(guard.Object);
                s.AddSingleton(audit.Object);
            });
        };
}
