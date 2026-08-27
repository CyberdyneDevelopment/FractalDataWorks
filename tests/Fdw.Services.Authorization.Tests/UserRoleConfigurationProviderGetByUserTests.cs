using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using Xunit;
using Fdw.Services.Data;

namespace Fdw.Services.Authorization.Tests;

/// <summary>
/// Regression tests for <see cref="UserRoleConfigurationProvider.GetByUser"/> (FDW-532 follow-up).
///
/// Root cause guarded here: <c>authz.UserRole.UserId</c> is stored UPPERCASE in the DB, while the
/// token subject GUID arrives lowercase (Guid.ToString() emits lowercase). The original
/// case-SENSITIVE Ordinal compare matched nobody, so EVERY user (admin included) got ZERO roles →
/// 0 permissions → a 401/403 cascade.
///
/// These tests exercise the REAL provider — only the DB transport (IConfigurationGateway) is faked.
/// GetByUser calls the base Get() (which hits the gateway) and then runs the real
/// OrdinalIgnoreCase filter under test. The prior FDW-532 adversarial tests mocked the provider
/// itself and so never executed this comparison; this suite closes that gap.
/// </summary>
public class UserRoleConfigurationProviderGetByUserTests
{
    // The live admin Id from the verified production row, shown in both cases.
    private const string AdminIdUpper = "CA520AE5-1234-4ABC-9DEF-0123456789AB";
    private static readonly string AdminIdLower = AdminIdUpper.ToLowerInvariant();

    // Why: Build the REAL UserRoleConfigurationProvider with a faked gateway that returns the
    // supplied stored rows for the no-arg Get() (List) command. The provider's GetByUser filter
    // is the code under test and runs unmocked.
    private static UserRoleConfigurationProvider MakeProvider(params UserRoleConfiguration[] storedRows)
    {

        var gateway = new Mock<IConfigurationGateway>();
        // Why: IConfigurationGateway.DataStores is contractually non-null; ResolveParentJoin reads it.
        gateway.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());
        gateway
            .Setup(g => g.Execute<IEnumerable<UserRoleConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<UserRoleConfiguration>>.Success(storedRows));

        return new UserRoleConfigurationProvider(
            NullLogger<UserRoleConfigurationProvider>.Instance,
            GatewayProviderFor(gateway.Object),
            "PlatformConfiguration", "authz");
    }

    private static UserRoleConfiguration Assignment(string userId)
        => new()
        {
            Id = Guid.NewGuid(),
            Name = $"{userId}:role",
            UserId = userId,
            RoleId = Guid.NewGuid(),
        };

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Authorization")]
    [Trait("Issue", "FDW-532")]
    public async Task GetByUserMatchesWhenStoredUpperAndQueriedLower()
    {
        // Arrange: stored row is UPPERCASE (as the DB stores it); query is lowercase (token subject).
        var provider = MakeProvider(Assignment(AdminIdUpper));

        // Act
        var result = await provider.GetByUser(AdminIdLower, TestContext.Current.CancellationToken);

        // Assert: the assignment IS returned despite the case difference.
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Count.ShouldBe(1);
        result.Value[0].UserId.ShouldBe(AdminIdUpper);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Authorization")]
    [Trait("Issue", "FDW-532")]
    public async Task GetByUserMatchesWhenStoredLowerAndQueriedUpper()
    {
        // Arrange: reverse direction — stored lowercase, queried uppercase.
        var provider = MakeProvider(Assignment(AdminIdLower));

        // Act
        var result = await provider.GetByUser(AdminIdUpper, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Count.ShouldBe(1);
        result.Value[0].UserId.ShouldBe(AdminIdLower);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Authorization")]
    [Trait("Issue", "FDW-532")]
    public async Task GetByUserReturnsAllAssignmentsForUserAcrossCase()
    {
        // Arrange: a user with two role assignments stored UPPERCASE, plus an unrelated user's row.
        var other = Guid.NewGuid().ToString().ToUpperInvariant();
        var provider = MakeProvider(
            Assignment(AdminIdUpper),
            Assignment(AdminIdUpper),
            Assignment(other));

        // Act: query lowercase.
        var result = await provider.GetByUser(AdminIdLower, TestContext.Current.CancellationToken);

        // Assert: exactly the two admin assignments, none of the other user's.
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Count.ShouldBe(2);
        result.Value.ShouldAllBe(ur => string.Equals(ur.UserId, AdminIdUpper, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Authorization")]
    [Trait("Issue", "FDW-532")]
    public async Task GetByUserReturnsEmptyForGenuinelyDifferentUser()
    {
        // Why: case-insensitivity must NOT collapse distinct GUIDs — a different user still gets none.
        var provider = MakeProvider(Assignment(AdminIdUpper));

        var result = await provider.GetByUser(
            Guid.NewGuid().ToString(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldBeEmpty();
    }

    // Why the gateway is registered rather than handed over: a provider asks for the gateway on the
    // connection it was told its rows live on, so the fake has to answer to that name to be found.
    // Why a double rather than the real provider: these tests exercise what a configuration provider
    // does with its gateway, not which gateway it selects, so the double answers for whatever
    // connection is asked. Selection itself is covered where the real provider is under test.
    private static IConfigurationGatewayProvider GatewayProviderFor(IConfigurationGateway gateway)
        => new AnyConnectionGateways(gateway);

    private sealed class AnyConnectionGateways : IConfigurationGatewayProvider
    {
        private readonly IConfigurationGateway _gateway;

        public AnyConnectionGateways(IConfigurationGateway gateway) => _gateway = gateway;

        public IGenericResult<IConfigurationGateway> Get(string connectionName)
            => GenericResult<IConfigurationGateway>.Success(_gateway);

        public IGenericResult Register(IConfigurationGateway gateway) => GenericResult.Success();
    }

}
