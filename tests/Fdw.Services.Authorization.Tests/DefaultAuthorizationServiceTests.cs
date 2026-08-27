using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Commands;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using Fdw.Services.Data;
using Fdw.Results;

namespace Fdw.Services.Authorization.Tests;

/// <summary>
/// Tests for <see cref="DefaultAuthorizationService"/> claim-based enforcement.
/// </summary>
// Why: Enforcement evaluates against the baked "perm" claims carried by the token
// (IAuthenticationContext.Permissions). The 3-tier union is resolved once at token-issue time
// by EffectivePermissionResolver, not re-queried per request, so these tests assert grant/deny
// purely from the permission set on the context.
public sealed class DefaultAuthorizationServiceTests
{
    private readonly DefaultAuthorizationService _sut;

    public DefaultAuthorizationServiceTests()
    {
        // Why: the providers/tenant/org dependencies remain on the ctor for DI-shape stability but
        // are unused by enforcement. Loose mocks satisfy the null checks without behavior setup.
        _sut = new DefaultAuthorizationService(
            CreateProviderMock<RoleConfiguration, RoleConfigurationCommand>(),
            CreateProviderMock<PermissionConfiguration, PermissionConfigurationCommand>(),
            CreateProviderMock<RolePermissionConfiguration, RolePermissionConfigurationCommand>(),
            NullLogger<DefaultAuthorizationService>.Instance);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task Authorize_Grants_WhenBakedPermissionMatches()
    {
        var context = CreateAuthenticatedContext("user-1", ["users:read", "users:write"]);

        var result = await _sut.Authorize(context, "users", "read", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task Authorize_Denies_WhenBakedPermissionAbsent()
    {
        var context = CreateAuthenticatedContext("user-1", ["connections:read"]);

        var result = await _sut.Authorize(context, "users", "write", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task Authorize_Denies_WhenNoBakedPermissions()
    {
        var context = CreateAuthenticatedContext("user-1", []);

        var result = await _sut.Authorize(context, "users", "read", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task Authorize_Grants_WhenDomainWildcardPresent()
    {
        var context = CreateAuthenticatedContext("user-1", ["users:*"]);

        var result = await _sut.Authorize(context, "users", "delete", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task Authorize_Grants_WhenGlobalWildcardPresent()
    {
        var context = CreateAuthenticatedContext("user-1", ["*:*"]);

        var result = await _sut.Authorize(context, "anything", "read", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task Authorize_Denies_WhenNotAuthenticated()
    {
        var mock = new Mock<IAuthenticationContext>();
        mock.Setup(c => c.IsAuthenticated).Returns(false);
        mock.Setup(c => c.UserId).Returns("user-1");
        mock.Setup(c => c.Permissions).Returns(new[] { "users:read" });

        var result = await _sut.Authorize(mock.Object, "users", "read", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task HasPermission_Grants_WhenBakedPermissionMatches()
    {
        var context = CreateAuthenticatedContext("user-1", ["users:read"]);

        var result = await _sut.HasPermission(context, "users:read", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task HasPermission_Denies_WhenBakedPermissionAbsent()
    {
        var context = CreateAuthenticatedContext("user-1", ["users:read"]);

        var result = await _sut.HasPermission(context, "users:write", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task HasPermission_Denies_WhenNotAuthenticated()
    {
        var mock = new Mock<IAuthenticationContext>();
        mock.Setup(c => c.IsAuthenticated).Returns(false);

        var result = await _sut.HasPermission(mock.Object, "users:read", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeFalse();
    }

    // ──────────────────────────────────────────────────────────────────────────────
    // Fail-loud guard clauses — null/blank input. Each guard returns a non-success
    // result carrying the AuthorizationLog ResultCode for that condition (never a
    // thrown exception or a silently-defaulted value).
    // ──────────────────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task Authorize_Fails_WhenContextNull()
    {
        var result = await _sut.Authorize(null!, "users", "read", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
        result.Messages[0].Code.ShouldBe("AUTHORIZATION-21000");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Authorize_Fails_WhenResourceBlank(string? resource)
    {
        var context = CreateAuthenticatedContext("user-1", ["users:read"]);

        var result = await _sut.Authorize(context, resource!, "read", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages[0].Code.ShouldBe("AUTHORIZATION-21001");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Authorize_Fails_WhenActionBlank(string? action)
    {
        var context = CreateAuthenticatedContext("user-1", ["users:read"]);

        var result = await _sut.Authorize(context, "users", action!, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages[0].Code.ShouldBe("AUTHORIZATION-21002");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task HasPermission_Fails_WhenContextNull()
    {
        var result = await _sut.HasPermission(null!, "users:read", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages[0].Code.ShouldBe("AUTHORIZATION-21000");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HasPermission_Fails_WhenPermissionBlank(string? permission)
    {
        var context = CreateAuthenticatedContext("user-1", ["users:read"]);

        var result = await _sut.HasPermission(context, permission!, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages[0].Code.ShouldBe("AUTHORIZATION-21004");
    }

    // ──────────────────────────────────────────────────────────────────────────────
    // HasRole — fail-loud guards, not-authenticated deny, happy/negative match paths.
    // ──────────────────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task HasRole_Fails_WhenContextNull()
    {
        var result = await _sut.HasRole(null!, "Admin", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages[0].Code.ShouldBe("AUTHORIZATION-21000");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HasRole_Fails_WhenRoleBlank(string? role)
    {
        var context = CreateAuthenticatedContextWithRoles("user-1", ["Admin"]);

        var result = await _sut.HasRole(context, role!, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages[0].Code.ShouldBe("AUTHORIZATION-21003");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task HasRole_Denies_WhenNotAuthenticated()
    {
        var mock = new Mock<IAuthenticationContext>();
        mock.Setup(c => c.IsAuthenticated).Returns(false);

        var result = await _sut.HasRole(mock.Object, "Admin", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task HasRole_Grants_WhenUserHasRole_CaseInsensitive()
    {
        var context = CreateAuthenticatedContextWithRoles("user-1", ["Admin", "Viewer"]);

        var result = await _sut.HasRole(context, "admin", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task HasRole_Denies_WhenUserLacksRole()
    {
        var context = CreateAuthenticatedContextWithRoles("user-1", ["Viewer"]);

        var result = await _sut.HasRole(context, "Admin", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeFalse();
    }

    // ──────────────────────────────────────────────────────────────────────────────
    // GetRoles — fail-loud guard, not-authenticated deny, happy/negative paths.
    // ──────────────────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task GetRoles_Fails_WhenContextNull()
    {
        var result = await _sut.GetRoles(null!, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages[0].Code.ShouldBe("AUTHORIZATION-21000");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task GetRoles_ReturnsEmpty_WhenNotAuthenticated()
    {
        var mock = new Mock<IAuthenticationContext>();
        mock.Setup(c => c.IsAuthenticated).Returns(false);

        var result = await _sut.GetRoles(mock.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task GetRoles_ReturnsRoles_WhenAuthenticated()
    {
        var context = CreateAuthenticatedContextWithRoles("user-1", ["Admin", "Viewer"]);

        var result = await _sut.GetRoles(context, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.ShouldContain("Admin");
        result.Value!.ShouldContain("Viewer");
        result.Value!.Count().ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task GetRoles_ReturnsEmpty_WhenUserHasNoRoles()
    {
        var context = CreateAuthenticatedContextWithRoles("user-1", []);

        var result = await _sut.GetRoles(context, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    private static IAuthenticationContext CreateAuthenticatedContext(string userId, IEnumerable<string> permissions)
    {
        var mock = new Mock<IAuthenticationContext>();
        mock.Setup(c => c.IsAuthenticated).Returns(true);
        mock.Setup(c => c.UserId).Returns(userId);
        mock.Setup(c => c.Roles).Returns(Array.Empty<string>());
        mock.Setup(c => c.Permissions).Returns(permissions);
        return mock.Object;
    }

    private static IAuthenticationContext CreateAuthenticatedContextWithRoles(string userId, IEnumerable<string> roles)
    {
        var mock = new Mock<IAuthenticationContext>();
        mock.Setup(c => c.IsAuthenticated).Returns(true);
        mock.Setup(c => c.UserId).Returns(userId);
        mock.Setup(c => c.Roles).Returns(roles);
        mock.Setup(c => c.Permissions).Returns(Array.Empty<string>());
        return mock.Object;
    }

    // Why: ImplementationConfigurationProviderBase<T, TCommand> has 10 constructor params (params 7-10 are optional).
    // All must be passed explicitly to Moq — Castle DynamicProxy uses reflection-based instantiation
    // and cannot resolve C# optional parameter defaults.
    private static ImplementationConfigurationProviderBase<T, TCommand> CreateProviderMock<T, TCommand>()
        where T : class, Fdw.Configuration.IGenericConfiguration
        where TCommand : Fdw.Services.Configuration.ConfigurationCommandBase<T>
    {
        return new Mock<ImplementationConfigurationProviderBase<T, TCommand>>(
            MockBehavior.Loose,
            NullLogger<ImplementationConfigurationProviderBase<T, TCommand>>.Instance,
            GatewayProviderFor(Mock.Of<IConfigurationGateway>(
                g => g.DataStores == (System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>())),
            "TestStore",
            "cfg") // invalidator
            .Object;
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
