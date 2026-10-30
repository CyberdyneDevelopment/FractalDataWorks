using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Context;
using Fdw.Services.Authentication.Abstractions.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Flow;
using Fdw.Services.Authorization;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Multitenancy.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Authentication.Tests.Flow;

/// <summary>
/// Administering who may use a sign-in flow (FDW-912): a gate is placed where permissions are first
/// baked, named as the platform spells the permission, and refused rather than guessed at when the flow
/// cannot take one, already carries several, or carries one this host cannot configure. A limit no role
/// can meet, and a restrictive change that locks out the administrator making it, are refused.
/// </summary>
public class AuthenticationFlowAdministrationTests
{
    private const string Password = "signin:password";
    private const string Entra = "signin:entra";
    private static readonly Guid AdminId = new("22222222-0000-0000-0000-000000000001");
    private static readonly Guid TenantId = new("22222222-0000-0000-0000-000000000002");
    private static readonly Guid PasswordId = new("22222222-0000-0000-0000-000000000003");
    private static readonly Guid EntraId = new("22222222-0000-0000-0000-000000000004");

    private static readonly AuthenticationFlowAdministrator SignedInWithPassword = new() { UserId = AdminId, AuthenticationMethods = ["pwd"] };
    private static readonly AuthenticationFlowAdministrator SignedInWithKey = new() { UserId = AdminId, AuthenticationMethods = [] };

    [Fact]
    public async Task GatingAnOpenFlowPlacesTheGateStraightAfterPermissionsAreBaked()
    {
        var harness = new Harness([Password], Row("PasswordLogin", ("password", null), ("authorizes", null)));

        var result = await harness.Administration.RequirePermission("PasswordLogin", Password, SignedInWithPassword, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        harness.Saved.ShouldNotBeNull();
        harness.Saved.Steps.Select(s => (s.StepOrder, s.StepName)).ShouldBe([(1, "password"), (2, "gate"), (3, "authorizes")]);
        harness.Saved.Steps[1].Configuration.ShouldBe(Gate(Password));
        result.Value!.RequiredPermissions.ShouldBe([Password]);
        harness.Flows.Verify(f => f.LoadAndValidate(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ThePermissionIsWrittenAsThePlatformSpellsIt()
    {
        var harness = new Harness([Password], Row("PasswordLogin", ("password", null), ("authorizes", null)));

        await harness.Administration.RequirePermission("PasswordLogin", "SIGNIN:PASSWORD", SignedInWithPassword, TestContext.Current.CancellationToken);

        harness.Saved!.Steps[1].Configuration.ShouldBe(Gate(Password));
    }

    [Fact]
    public async Task GatingAnAlreadyGatedFlowRewritesItsGateInPlace()
    {
        var harness = new Harness([Password], Row("PasswordLogin", ("password", null), ("gate", Gate(Entra)), ("authorizes", null)));

        var result = await harness.Administration.RequirePermission("PasswordLogin", Password, SignedInWithPassword, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        harness.Saved!.Steps.Count.ShouldBe(3);
        harness.Saved.Steps[1].Configuration.ShouldBe(Gate(Password));
    }

    [Fact]
    public async Task APermissionNobodyDefinesIsRefusedAndNothingIsSaved()
    {
        var harness = new Harness([Password], Row("PasswordLogin", ("password", null), ("authorizes", null)));

        var result = await harness.Administration.RequirePermission("PasswordLogin", "signin:typo", SignedInWithPassword, TestContext.Current.CancellationToken);

        result.CodeChain.ShouldContain(AuthenticationResultCodes.ByName("PermissionNotDefined"));
        harness.Saved.ShouldBeNull();
    }

    [Fact]
    public async Task APermissionNoRoleCarriesIsRefusedBecauseTheFlowWouldAdmitNobody()
    {
        var harness = new Harness([Password], Row("PasswordLogin", ("password", null), ("authorizes", null))) { CarriedByARole = [PasswordId] };

        var result = await harness.Administration.RequirePermission("PasswordLogin", Entra, SignedInWithKey, TestContext.Current.CancellationToken);

        result.CodeChain.ShouldContain(AuthenticationResultCodes.ByName("PermissionHeldByNoRole"));
        harness.Saved.ShouldBeNull();
    }

    [Fact]
    public async Task ALimitTheAdministratorDoesNotMeetOnTheirOwnWayInIsRefused()
    {
        var harness = new Harness([], Row("PasswordLogin", ("password", null), ("authorizes", null)));

        var result = await harness.Administration.RequirePermission("PasswordLogin", Password, SignedInWithPassword, TestContext.Current.CancellationToken);

        result.CodeChain.ShouldContain(AuthenticationResultCodes.ByName("ChangeLocksOutAdministrator"));
        harness.Saved.ShouldBeNull();
    }

    [Fact]
    public async Task SwitchingOffTheLastFlowTheAdministratorSignsInThroughIsRefused()
    {
        var harness = new Harness([Password], Row("PasswordLogin", ("password", null), ("authorizes", null)));

        var result = await harness.Administration.SetEnabled("PasswordLogin", isEnabled: false, SignedInWithPassword, TestContext.Current.CancellationToken);

        result.CodeChain.ShouldContain(AuthenticationResultCodes.ByName("ChangeLocksOutAdministrator"));
        harness.Saved.ShouldBeNull();
    }

    [Fact]
    public async Task SwitchingOffOneOfTwoFlowsTheAdministratorCanUseIsAllowed()
    {
        var harness = new Harness([Password],
            Row("PasswordLogin", ("password", null), ("authorizes", null)),
            Row("PasswordSession", ("password", null), ("authorizes", null)));

        var result = await harness.Administration.SetEnabled("PasswordLogin", isEnabled: false, SignedInWithPassword, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        harness.Saved!.IsEnabled.ShouldBeFalse();
        result.Value!.IsServing.ShouldBeFalse();
        result.Value.Problem.ShouldBeNull();
    }

    [Fact]
    public async Task AnAdministratorWhoDidNotSignInThroughAFlowCannotBeLockedOutByOne()
    {
        var harness = new Harness([], Row("PasswordLogin", ("password", null), ("authorizes", null)));

        var result = await harness.Administration.SetEnabled("PasswordLogin", isEnabled: false, SignedInWithKey, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        harness.Saved!.IsEnabled.ShouldBeFalse();
    }

    [Fact]
    public async Task AFlowThatNeverBakesPermissionsCannotBeGated()
    {
        var harness = new Harness([Password], Row("TenantSwitch", ("authorizes", null)));

        var result = await harness.Administration.RequirePermission("TenantSwitch", Password, SignedInWithKey, TestContext.Current.CancellationToken);

        result.CodeChain.ShouldContain(AuthenticationResultCodes.ByName("FlowCannotBeGated"));
        harness.Saved.ShouldBeNull();
    }

    [Fact]
    public async Task AFlowRequiringSeveralPermissionsIsNeitherRewrittenNorOpened()
    {
        var harness = new Harness([Password, Entra], Row("PasswordLogin",
            ("password", null), ("gate", Gate(Password)), ("gate", Gate(Entra)), ("authorizes", null)));

        var gated = await harness.Administration.RequirePermission("PasswordLogin", Password, SignedInWithPassword, TestContext.Current.CancellationToken);
        var opened = await harness.Administration.Open("PasswordLogin", SignedInWithPassword, TestContext.Current.CancellationToken);

        gated.CodeChain.ShouldContain(AuthenticationResultCodes.ByName("FlowGateAmbiguous"));
        opened.CodeChain.ShouldContain(AuthenticationResultCodes.ByName("FlowGateAmbiguous"));
        harness.Saved.ShouldBeNull();
    }

    [Fact]
    public async Task AGateThisHostCannotConfigureIsNotRewrittenButCanBeOpened()
    {
        var harness = new Harness([Password], Row("PasswordLogin", ("password", null), ("readonly-gate", Gate(Entra)), ("authorizes", null)));

        var gated = await harness.Administration.RequirePermission("PasswordLogin", Password, SignedInWithPassword, TestContext.Current.CancellationToken);

        gated.CodeChain.ShouldContain(AuthenticationResultCodes.ByName("FlowGateNotAdministrable"));
        harness.Saved.ShouldBeNull();

        var opened = await harness.Administration.Open("PasswordLogin", SignedInWithPassword, TestContext.Current.CancellationToken);

        opened.IsSuccess.ShouldBeTrue();
        harness.Saved!.Steps.Select(s => s.StepName).ShouldBe(["password", "authorizes"]);
    }

    [Fact]
    public async Task OpeningAFlowRemovesItsGateAndRenumbersTheRest()
    {
        var harness = new Harness([], Row("PasswordLogin", ("password", null), ("gate", Gate(Password)), ("authorizes", null)));

        var result = await harness.Administration.Open("PasswordLogin", SignedInWithPassword, TestContext.Current.CancellationToken);

        result.Value!.RequiredPermissions.ShouldBeEmpty();
        harness.Saved!.Steps.Select(s => (s.StepOrder, s.StepName)).ShouldBe([(1, "password"), (2, "authorizes")]);
    }

    [Fact]
    public async Task AChangeToHowTheFlowAlreadyStandsWritesNothing()
    {
        var harness = new Harness([Password], Row("PasswordLogin", ("password", null), ("authorizes", null)));

        var opened = await harness.Administration.Open("PasswordLogin", SignedInWithPassword, TestContext.Current.CancellationToken);
        var switchedOn = await harness.Administration.SetEnabled("PasswordLogin", isEnabled: true, SignedInWithPassword, TestContext.Current.CancellationToken);

        opened.IsSuccess.ShouldBeTrue();
        switchedOn.IsSuccess.ShouldBeTrue();
        harness.Saved.ShouldBeNull();
        harness.Flows.Verify(f => f.LoadAndValidate(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AFlowNobodyConfiguredIsCodedFlowNotFound()
    {
        var harness = new Harness([Password], Row("PasswordLogin", ("password", null), ("authorizes", null)));

        var result = await harness.Administration.SetEnabled("NoSuchFlow", isEnabled: false, SignedInWithPassword, TestContext.Current.CancellationToken);

        result.CodeChain.ShouldContain(AuthenticationResultCodes.ByName("FlowNotFound"));
    }

    [Fact]
    public async Task TheRowTheProviderHandedBackIsNeverEdited()
    {
        var row = Row("PasswordLogin", ("password", null), ("authorizes", null));
        var harness = new Harness([Password], row);

        await harness.Administration.RequirePermission("PasswordLogin", Password, SignedInWithPassword, TestContext.Current.CancellationToken);

        row.Steps.Count.ShouldBe(2);
        harness.Saved.ShouldNotBeSameAs(row);
    }

    [Fact]
    public async Task ListingStatesWhatEachFlowProvesAndWhoItAdmits()
    {
        var harness = new Harness([Password],
            Row("PasswordLogin", ("password", null), ("gate", Gate(Password)), ("authorizes", null)),
            Row("PasswordSession", ("password", null), ("readonly-gate", Gate(Password)), ("authorizes", null)),
            Row("TenantSwitch", ("authorizes", null)));

        var result = await harness.Administration.Get(TestContext.Current.CancellationToken);

        var flows = result.Value!;
        flows.Select(f => f.Name).ShouldBe(["PasswordLogin", "PasswordSession", "TenantSwitch"]);
        flows[0].AuthenticationMethods.ShouldBe(["pwd"]);
        flows[0].RequiredPermissions.ShouldBe([Password]);
        flows[0].CanChangeRequiredPermission.ShouldBeTrue();
        flows[1].RequiredPermissions.ShouldBe([Password]);
        flows[1].CanChangeRequiredPermission.ShouldBeFalse();
        flows[2].CanChangeRequiredPermission.ShouldBeFalse();
        flows[2].CanChangeRequiredPermissionReason.ShouldNotBeNull();
    }

    // ---------------------------------------------------------------- doubles and builders

    private static string Gate(string permission) => JsonSerializer.Serialize(new { RequiredPermission = permission });

    private static AuthenticationFlowImplementationConfiguration Row(
        string name, params (string Step, string? Configuration)[] steps) => new()
    {
        Name = name,
        Implementation = "AuthenticationFlow",
        Audience = "test-audience",
        ExecutionLifetime = "PT5M",
        IsEnabled = true,
        Steps = [.. steps.Select((s, i) => new AuthenticationFlowStepConfiguration
        {
            Name = s.Step, StepName = s.Step, StepOrder = i + 1, Configuration = s.Configuration,
        })],
    };

    private static IPermissionImplementationConfiguration Permission(Guid id, string name)
    {
        var permission = new Mock<IPermissionImplementationConfiguration>();
        permission.SetupGet(p => p.Id).Returns(id);
        permission.SetupGet(p => p.Name).Returns(name);
        return permission.Object;
    }

    private static IRolePermissionImplementationConfiguration Grant(Guid permissionId)
    {
        var grant = new Mock<IRolePermissionImplementationConfiguration>();
        grant.SetupGet(g => g.PermissionId).Returns(permissionId);
        return grant.Object;
    }

    /// <summary>The administration service over the given rows, with the administrator holding <c>held</c>.</summary>
    private sealed class Harness
    {
        private readonly AuthenticationFlowImplementationConfiguration[] _rows;
        private readonly string[] _held;
        private AuthenticationFlowAdministration? _administration;

        public Harness(string[] held, params AuthenticationFlowImplementationConfiguration[] rows)
        {
            _held = held;
            _rows = rows;
        }

        /// <summary>Which permissions some role carries. Both, unless a test says otherwise.</summary>
        public Guid[] CarriedByARole { get; init; } = [PasswordId, EntraId];

        public Mock<IAuthenticationFlowProvider> Flows { get; } = new();

        public AuthenticationFlowImplementationConfiguration? Saved { get; private set; }

        public AuthenticationFlowAdministration Administration => _administration ??= Build();

        private AuthenticationFlowAdministration Build()
        {
            var configuration = new Mock<IAuthenticationFlowConfigurationProvider>();
            configuration.Setup(c => c.Get(It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<IReadOnlyList<IAuthenticationFlowImplementationConfiguration>>.Success(_rows));
            configuration.Setup(c => c.Update(
                    It.IsAny<AuthenticationFlowImplementationConfiguration>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<AuthenticationFlowImplementationConfiguration, string, string, string, CancellationToken>((saved, _, _, _, _) => Saved = saved)
                .ReturnsAsync(GenericResult.Success());

            Flows.Setup(f => f.LoadAndValidate(It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
            Flows.Setup(f => f.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string name, CancellationToken _) => GenericResult<AuthenticationFlow>.Success(new AuthenticationFlow
                {
                    Name = name,
                    Steps = [],
                    RequiredPermissions = [],
                    Audience = "test-audience",
                    ExecutionLifetime = TimeSpan.FromMinutes(5),
                }));

            var permissions = new Mock<IPermissionConfigurationProvider>();
            permissions.Setup(p => p.Get(It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<IReadOnlyList<IPermissionImplementationConfiguration>>.Success(
                    [Permission(PasswordId, Password), Permission(EntraId, Entra)]));

            var grants = new Mock<IRolePermissionConfigurationProvider>();
            grants.Setup(g => g.Get(It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<IReadOnlyList<IRolePermissionImplementationConfiguration>>.Success(
                    [.. CarriedByARole.Select(Grant)]));

            var tenants = new Mock<ITenantResolver>();
            tenants.Setup(t => t.TenantFor(AdminId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<Guid>.Success(TenantId));
            var held = new Mock<IUserPermissions>();
            held.Setup(p => p.Get(AdminId.ToString(), TenantId, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<EffectiveAuthorization>.Success(new EffectiveAuthorization(_held, [], validUntil: null)));

            var steps = new NamedSteps()
                .Add("password", new HostileStep
                {
                    Name = "password",
                    Contributes = [ContextElements.Subject, ContextElements.Principal, ContextElements.Claims],
                    AuthenticationMethods = ["pwd"],
                })
                .Add("gate", new AdministrableGate())
                .Add("readonly-gate", new ReadOnlyGate())
                .Add("authorizes", new HostileStep
                {
                    Name = "authorizes",
                    Requires = [ContextElements.Principal],
                    Contributes = [ContextElements.Decision],
                });

            return new AuthenticationFlowAdministration(
                configuration.Object, Flows.Object, permissions.Object, grants.Object, tenants.Object, held.Object,
                new AuthenticationContextAccessor(), steps.Lookup,
                () => new[] { "password", "gate", "readonly-gate", "authorizes" }.Select(steps.Lookup).OfType<IAuthenticationStep>());
        }
    }

    /// <summary>A gate reading <c>RequiredPermission</c> from its settings, as the shipped one does.</summary>
    private class ReadOnlyGate : IPermissionGatedAuthenticationStep
    {
        public virtual string Name => "readonly-gate";

        public IReadOnlyList<IContextElement> Requires => [ContextElements.Principal, ContextElements.Claims];

        public IReadOnlyList<IContextElement> Contributes => [ContextElements.Decision];

        public IReadOnlyList<string> AuthenticationMethods => [];

        public IGenericResult ValidateConfiguration(string? configuration)
            => RequiredPermission(configuration) is { IsSuccess: true } ? GenericResult.Success() : GenericResult.Failure(new GenericMessage("bad settings"));

        public IGenericResult<string> RequiredPermission(string? configuration)
            => configuration is not null
                && JsonDocument.Parse(configuration).RootElement.TryGetProperty("RequiredPermission", out var p)
                && p.GetString() is { Length: > 0 } name
                ? GenericResult<string>.Success(name)
                : GenericResult<string>.Failure(new GenericMessage("no RequiredPermission"));

        public Task<IGenericResult<StepOutcome>> Execute(
            AuthenticationContext context, ITenantProvider tenants, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("never run here");

        public Task<IGenericResult<StepOutcome>> Execute(
            AuthenticationContext context, string? configuration, ITenantProvider tenants, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("never run here");
    }

    /// <summary>The same gate, opted in to being placed and pointed from a console.</summary>
    private sealed class AdministrableGate : ReadOnlyGate, IAdministrablePermissionGate
    {
        public override string Name => "gate";

        public string ConfigurationRequiring(string permission) => Gate(permission);
    }
}
