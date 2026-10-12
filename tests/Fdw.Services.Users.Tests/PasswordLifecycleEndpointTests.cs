using System.Collections.Generic;
using System.Linq;
using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Users.Abstractions;
using Fdw.Services.Users.Clients.Models;
using Fdw.Services.Users.Configuration;
using Fdw.Services.Users.Endpoints;
using Fdw.Services.Users.Tests.TestSupport;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Users.Tests;

/// <summary>
/// The password routes after FDW-910: a password is issued only to an account some enabled password
/// flow admits, it can be taken away, and a new account states its roles.
/// </summary>
[Trait("Category", "Security")]
public class PasswordLifecycleEndpointTests
{
    private static readonly Guid VendorId = new("7e7e7e7e-0000-0000-0000-000000000001");

    // ---------------------------------------------------------------- SetPasswordLogin

    [Fact]
    public async Task SetPasswordLoginRefusesAnAccountNoPasswordFlowAdmits()
    {
        var credentials = new Mock<IUserCredentialService>(MockBehavior.Strict);
        var endpoint = Factory.Create<SetPasswordLogin>(
            Context(admits: GenericResult<bool>.Success(false)), Store().Provider, credentials.Object);

        await endpoint.HandleAsync(new SetPasswordLoginRequest { IdOrName = VendorId.ToString(), InitialPassword = "Initial#Pass1" }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(409);
        credentials.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SetPasswordLoginIssuesAPasswordToAnAdmittedAccount()
    {
        var credentials = new Mock<IUserCredentialService>();
        credentials.Setup(c => c.HasCredential(VendorId, "Password", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<bool>.Success(false));
        credentials.Setup(c => c.Store(VendorId, "Password", "Initial#Pass1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());
        credentials.Setup(c => c.ForcePasswordChange(VendorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());
        var endpoint = Factory.Create<SetPasswordLogin>(
            Context(admits: GenericResult<bool>.Success(true)), Store().Provider, credentials.Object);

        await endpoint.HandleAsync(new SetPasswordLoginRequest { IdOrName = VendorId.ToString(), InitialPassword = "Initial#Pass1" }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(204);
        credentials.Verify(c => c.Store(VendorId, "Password", "Initial#Pass1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetPasswordLoginIssuesNothingWhenAdmissionCannotBeDecided()
    {
        var credentials = new Mock<IUserCredentialService>(MockBehavior.Strict);
        var endpoint = Factory.Create<SetPasswordLogin>(
            Context(admits: GenericResult<bool>.Failure(new GenericMessage("flows unreadable"))), Store().Provider, credentials.Object);

        await endpoint.HandleAsync(new SetPasswordLoginRequest { IdOrName = VendorId.ToString(), InitialPassword = "Initial#Pass1" }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(500);
        credentials.VerifyNoOtherCalls();
    }

    // ---------------------------------------------------------------- ResetPassword

    [Fact]
    public async Task ResetPasswordRefusesAnAccountNoPasswordFlowAdmits()
    {
        var credentials = new Mock<IUserCredentialService>(MockBehavior.Strict);
        var endpoint = Factory.Create<ResetPassword>(
            Context(admits: GenericResult<bool>.Success(false)), Store().Provider, credentials.Object);

        await endpoint.HandleAsync(new ResetPasswordRequest { IdOrName = VendorId.ToString(), NewPassword = "Reset#Pass123" }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(409);
        credentials.VerifyNoOtherCalls();
    }

    // ---------------------------------------------------------------- RemovePasswordLogin

    [Fact]
    public async Task RemovePasswordLoginDisablesTheAccountsPassword()
    {
        var credentials = new Mock<IUserCredentialService>();
        credentials.Setup(c => c.HasCredential(VendorId, "Password", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<bool>.Success(true));
        credentials.Setup(c => c.Remove(VendorId, "Password", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());
        var endpoint = Factory.Create<RemovePasswordLogin>(Context(admits: null), Store().Provider, credentials.Object);

        await endpoint.HandleAsync(new UserScopedRequest { IdOrName = VendorId.ToString() }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(204);
        credentials.Verify(c => c.Remove(VendorId, "Password", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemovePasswordLoginAnswersNotFoundWhenThereIsNone()
    {
        var credentials = new Mock<IUserCredentialService>();
        credentials.Setup(c => c.HasCredential(VendorId, "Password", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<bool>.Success(false));
        var endpoint = Factory.Create<RemovePasswordLogin>(Context(admits: null), Store().Provider, credentials.Object);

        await endpoint.HandleAsync(new UserScopedRequest { IdOrName = VendorId.ToString() }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(404);
        credentials.Verify(c => c.Remove(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemovePasswordLoginIsRefusedToACallerWhoDoesNotAdministerTheAccount()
    {
        var credentials = new Mock<IUserCredentialService>(MockBehavior.Strict);
        var endpoint = Factory.Create<RemovePasswordLogin>(
            Context(admits: null, platformAdmin: false), Store().Provider, credentials.Object);

        await endpoint.HandleAsync(new UserScopedRequest { IdOrName = VendorId.ToString() }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(403);
        credentials.VerifyNoOtherCalls();
    }

    // ---------------------------------------------------------------- CreateUser roles

    [Fact]
    public void ACreateUserRequestWithoutRolesIsRefused()
    {
        var request = new CreateUserRequest
        {
            Username = "vendor",
            SignInMethods = new SignInMethodsRequest { ApiKey = new ApiKeySignInMethod { Label = "ingest", Kind = "agent" } },
        };

        new CreateUserValidator().Validate(request).Errors.ShouldContain(e => e.PropertyName == "Roles");
    }

    [Fact]
    public void ACreateUserRequestWithAnEmptyRoleListIsAccepted()
    {
        var request = new CreateUserRequest
        {
            Username = "vendor",
            SignInMethods = new SignInMethodsRequest { ApiKey = new ApiKeySignInMethod { Label = "ingest", Kind = "agent" } },
            Roles = [],
        };

        new CreateUserValidator().Validate(request).Errors.ShouldNotContain(e => e.PropertyName == "Roles");
    }

    // ---------------------------------------------------------------- CreateUser with a password

    [Fact]
    public async Task CreatingAPasswordAccountWhoseRolesNoPasswordFlowAdmitsIsRefusedBeforeAnythingIsWritten()
    {
        var credentials = new Mock<IUserCredentialService>(MockBehavior.Strict);
        var endpoint = Factory.Create<CreateUser>(CreateContext(admits: false), Store().Provider, credentials.Object);

        await endpoint.HandleAsync(PasswordAccount(["Viewer"]), CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(409);
        // Named on Roles, so a client can tell it from the route's other 409 (a taken username).
        endpoint.ValidationFailures.ShouldContain(f => f.PropertyName == "Roles");
        credentials.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreatingAPasswordAccountAsksWithThePermissionsItsRolesGrant()
    {
        IReadOnlyCollection<string>? asked = null;
        var endpoint = Factory.Create<CreateUser>(
            CreateContext(admits: false, onAsked: held => asked = held), Store().Provider, new Mock<IUserCredentialService>().Object);

        await endpoint.HandleAsync(PasswordAccount(["Viewer", "PasswordSignIn"]), CancellationToken.None);

        asked.ShouldNotBeNull().ShouldBe(["datasets:read", "signin:password"], ignoreOrder: true);
    }

    // ---------------------------------------------------------------- CreateUser with a taken username (FDW-1056)

    [Fact]
    public async Task CreatingAnAccountWhoseUsernameIsTakenIsRefusedBeforeAnythingIsWritten()
    {
        // Save is an upsert by name: without this refusal the create would write a new version of the
        // existing account and attach the new password to it. Case-insensitivity is the store's
        // collation, so it is not asserted against the fake store here.
        var credentials = new Mock<IUserCredentialService>(MockBehavior.Strict);
        var store = Store();
        var endpoint = Factory.Create<CreateUser>(CreateContext(admits: true), store.Provider, credentials.Object);

        await endpoint.HandleAsync(PasswordAccount(["Viewer", "PasswordSignIn"], username: "vendor"), CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(409);
        endpoint.ValidationFailures.ShouldContain(f => f.PropertyName == "Username");
        store.Writes.ShouldBeEmpty();
        credentials.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AnEmailAddressIsAFreeUsernameLikeAnyOther()
    {
        var credentials = new Mock<IUserCredentialService>(MockBehavior.Strict);
        var endpoint = Factory.Create<CreateUser>(CreateContext(admits: false), Store().Provider, credentials.Object);

        await endpoint.HandleAsync(PasswordAccount(["Viewer"], username: "jane.doe@example.com"), CancellationToken.None);

        // Past the username check, so refused on the roles instead (no password flow admits "Viewer").
        endpoint.ValidationFailures.ShouldNotContain(f => f.PropertyName == "Username");
        endpoint.ValidationFailures.ShouldContain(f => f.PropertyName == "Roles");
    }

    private static CreateUserRequest PasswordAccount(string[] roles, string username = "newhire") => new()
    {
        Username = username,
        SignInMethods = new SignInMethodsRequest { Password = new PasswordSignInMethod { InitialPassword = "Initial#Pass1" } },
        Roles = roles,
    };

    private static Action<DefaultHttpContext> CreateContext(bool admits, Action<IReadOnlyCollection<string>>? onAsked = null)
        => ctx =>
        {
            ctx.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(Fdw.Claims.Abstractions.ClaimDefinitions.tenantId.Name, Guid.NewGuid().ToString())], "test"));
            var grants = new Mock<Fdw.Services.Authorization.Abstractions.IRoleGrants>();
            grants.Setup(g => g.Get(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IReadOnlyList<string> roles, CancellationToken _) => GenericResult<IReadOnlyCollection<string>>.Success(
                    [.. roles.Select(r => r == "PasswordSignIn" ? "signin:password" : "datasets:read")]));
            var admission = new Mock<ISignInMethodAdmission>();
            admission.Setup(a => a.MayUse(It.IsAny<IReadOnlyCollection<string>>(), "pwd", It.IsAny<CancellationToken>()))
                .Callback((IReadOnlyCollection<string> held, string _, CancellationToken _) => onAsked?.Invoke(held))
                .ReturnsAsync(GenericResult<bool>.Success(admits));
            ctx.AddTestServices(s =>
            {
                s.AddSingleton(grants.Object);
                s.AddSingleton(admission.Object);
            });
        };

    private static Fdw.Services.Data.Abstractions.IConfigurationGatewayProvider NoStore()
        => Mock.Of<Fdw.Services.Data.Abstractions.IConfigurationGatewayProvider>();

    // Nothing is written on the refused path; only the user provider reads, to find out whether the name is taken.
    private sealed class CreateUser(UserConfigurationProvider users, IUserCredentialService credentials) : CreateUserEndpointBase<CreateUserRequest>(
        NullLogger.Instance,
        users,
        new UserTenantConfigurationProvider(NullLogger<UserTenantConfigurationProvider>.Instance, NoStore(), "PlatformConfiguration", "tenant", "UserTenants"),
        new Fdw.Services.Authorization.UserRoleConfigurationProvider(NullLogger<Fdw.Services.Authorization.UserRoleConfigurationProvider>.Instance, NoStore(), "PlatformConfiguration", "authz", "UserRole"),
        new Fdw.Services.Authorization.RoleConfigurationProvider(NullLogger<Fdw.Services.Authorization.RoleConfigurationProvider>.Instance, NoStore(), "PlatformConfiguration", "authz", "Role"),
        credentials,
        Mock.Of<Fdw.Services.Authentication.Abstractions.Methods.IPersonalAccessTokenService>(),
        Mock.Of<Fdw.Services.Authentication.Abstractions.Methods.IAgentKeyService>(),
        Mock.Of<Fdw.Services.ExternalIdentityProviders.Binding.IExternalIdentityConfigurationProvider>())
    {
        protected override void ConfigureEndpoint()
        {
        }
    }

    // ---------------------------------------------------------------- builders

    private static ConfigurationStore<UserConfigurationProvider, IUserImplementationConfiguration, UserDomainConfiguration> Store()
        => ConfigurationStore.Users(new UserImplementationConfiguration
        {
            Id = VendorId,
            Username = "vendor",
            IsActive = true,
            IsCurrent = true,
            CreatedAt = DateTimeOffset.UtcNow,
        });

    private static Action<DefaultHttpContext> Context(IGenericResult<bool>? admits, bool platformAdmin = true)
        => ctx =>
        {
            ctx.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", Guid.NewGuid().ToString())], "test"));
            var roles = new Mock<ISystemRoleConfiguration>();
            roles.Setup(r => r.IsPlatformAdmin(It.IsAny<ClaimsPrincipal>())).Returns(platformAdmin);
            roles.SetupGet(r => r.AdminRoleName).Returns("Admin");
            var admission = new Mock<ISignInMethodAdmission>(MockBehavior.Strict);
            if (admits is not null)
                admission.Setup(a => a.MayUse(VendorId, "pwd", It.IsAny<CancellationToken>())).ReturnsAsync(admits);
            ctx.AddTestServices(s =>
            {
                s.AddSingleton(roles.Object);
                s.AddSingleton(admission.Object);
            });
        };

    private sealed class SetPasswordLogin(UserConfigurationProvider users, IUserCredentialService credentials)
        : SetPasswordLoginEndpointBase(NullLogger.Instance, users, credentials);

    private sealed class ResetPassword(UserConfigurationProvider users, IUserCredentialService credentials)
        : ResetPasswordEndpointBase(users, credentials, NullLogger.Instance);

    private sealed class RemovePasswordLogin(UserConfigurationProvider users, IUserCredentialService credentials)
        : RemovePasswordLoginEndpointBase(NullLogger.Instance, users, credentials);

    private sealed class CreateUserValidator : Endpoints.Validators.CreateUserRequestValidator<CreateUserRequest>;
}
