using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Claims.Abstractions;
using Fdw.Messages;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Settings;
using Fdw.Services.Settings.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Settings.Tests;

/// <summary>
/// Tests for the endpoints over inherited and locked settings (FDW-1579): who may act at each level, that every set and lift is recorded
/// in the audit at the level it was made at, and that a change which cannot be recorded answers 500.
/// </summary>
[Trait("Category", "Security")]
public sealed class InheritedSettingEndpointTests
{
    private static readonly Guid Tenant = new("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid OtherTenant = new("aaaaaaaa-0000-0000-0000-000000000002");
    private static readonly Guid Finance = new("44444444-0000-0000-0000-000000000001");

    private sealed class SetTenant(IInheritedSettings settings, IAdministrativeAudit audit, ISystemRoleConfiguration roles)
        : SetTenantInheritedSettingEndpointBase(NullLogger.Instance, settings, audit, roles)
    {
        protected override void ConfigureEndpoint() => AllowAnonymous();
    }

    private sealed class LiftTenant(IInheritedSettings settings, IAdministrativeAudit audit, ISystemRoleConfiguration roles)
        : LiftTenantSettingLockEndpointBase(NullLogger.Instance, settings, audit, roles)
    {
        protected override void ConfigureEndpoint() => AllowAnonymous();
    }

    private sealed class ListTenant(IInheritedSettings settings, ISystemRoleConfiguration roles)
        : ListTenantInheritedSettingsEndpointBase(NullLogger.Instance, settings, roles)
    {
        protected override void ConfigureEndpoint() => AllowAnonymous();
    }

    private sealed class SetOrganization(IInheritedSettings settings, IAdministrativeAudit audit)
        : SetOrganizationInheritedSettingEndpointBase(NullLogger.Instance, settings, audit)
    {
        protected override void ConfigureEndpoint() => AllowAnonymous();
    }

    private sealed class SetPlatform(IInheritedSettings settings, IAdministrativeAudit audit, ISystemRoleConfiguration roles)
        : SetPlatformInheritedSettingEndpointBase(NullLogger.Instance, settings, audit, roles)
    {
        protected override void ConfigureEndpoint() => AllowAnonymous();
    }

    private static Action<DefaultHttpContext> ActingIn(Guid tenantId)
        => ctx => ctx.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimDefinitions.tenantId.Name, tenantId.ToString())], "test"));

    private static ISystemRoleConfiguration Roles(bool platformAdmin)
    {
        var roles = new Mock<ISystemRoleConfiguration>();
        roles.Setup(r => r.IsPlatformAdmin(It.IsAny<ClaimsPrincipal>())).Returns(platformAdmin);
        return roles.Object;
    }

    private static Mock<IAdministrativeAudit> Audit(List<AdministrativeChangeEntry>? recorded = null, bool succeeds = true)
    {
        var audit = new Mock<IAdministrativeAudit>();
        audit.Setup(a => a.Record(It.IsAny<AdministrativeChangeEntry>(), It.IsAny<CancellationToken>()))
            .Callback((AdministrativeChangeEntry entry, CancellationToken _) => recorded?.Add(entry))
            .ReturnsAsync(succeeds ? GenericResult.Success() : GenericResult.Failure(new GenericMessage("the audit store is down")));
        return audit;
    }

    private static EffectiveSetting Setting(string value, string setAt, string? lockedAt = null)
        => new("MaxRows", "Int32", null, value, setAt, lockedAt);

    private static Mock<IInheritedSettings> Settings(EffectiveSetting? after = null, bool setFails = false)
    {
        var settings = new Mock<IInheritedSettings>();
        settings.Setup(s => s.Get(It.IsAny<SettingScope>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<EffectiveSetting>.Success(Setting("500", SettingScope.PlatformLevel)));
        var result = setFails
            ? GenericResult<EffectiveSetting>.Failure(new GenericMessage("the setting is locked at the Platform level"))
            : GenericResult<EffectiveSetting>.Success(after!);
        settings.Setup(s => s.Set(It.IsAny<SettingScope>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(result);
        settings.Setup(s => s.Lift(It.IsAny<SettingScope>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(result);
        return settings;
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ATenantSetAnsweredWith200AndRecordedAtTheTenantLevelNamingTheTenant()
    {
        var recorded = new List<AdministrativeChangeEntry>();
        var endpoint = Factory.Create<SetTenant>(ActingIn(Tenant), Settings(after: Setting("300", SettingScope.TenantLevel, SettingScope.TenantLevel)).Object, Audit(recorded).Object, Roles(false));

        await endpoint.HandleAsync(new SetTenantInheritedSettingRequest { TenantId = Tenant, SettingName = "MaxRows", Value = "300", Locked = true }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        endpoint.Response.IsLocked.ShouldBeTrue();
        var entry = recorded.ShouldHaveSingleItem();
        entry.ChangeKind.ShouldBe("SettingSet");
        entry.ItemId.ShouldBe($"{Tenant}:MaxRows");
        entry.Level.Name.ShouldBe("Tenant");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ATenantSetByAnotherTenantsCallerIsRefusedAndNothingIsSet()
    {
        var settings = Settings(after: Setting("300", SettingScope.TenantLevel));
        var endpoint = Factory.Create<SetTenant>(ActingIn(OtherTenant), settings.Object, Audit().Object, Roles(false));

        await endpoint.HandleAsync(new SetTenantInheritedSettingRequest { TenantId = Tenant, SettingName = "MaxRows", Value = "300" }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(403);
        settings.Verify(s => s.Set(It.IsAny<SettingScope>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task APlatformAdministratorSetsAnyTenantsValue()
    {
        var endpoint = Factory.Create<SetTenant>(ActingIn(OtherTenant), Settings(after: Setting("300", SettingScope.TenantLevel)).Object, Audit().Object, Roles(true));

        await endpoint.HandleAsync(new SetTenantInheritedSettingRequest { TenantId = Tenant, SettingName = "MaxRows", Value = "300" }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ASetTheServiceRefusesIsAnsweredFromItsFailureAndRecordsNothing()
    {
        var recorded = new List<AdministrativeChangeEntry>();
        var endpoint = Factory.Create<SetTenant>(ActingIn(Tenant), Settings(setFails: true).Object, Audit(recorded).Object, Roles(false));

        await endpoint.HandleAsync(new SetTenantInheritedSettingRequest { TenantId = Tenant, SettingName = "MaxRows", Value = "300" }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBeGreaterThanOrEqualTo(400);
        recorded.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ASetMadeButNotRecordedIsAnswered500()
    {
        var endpoint = Factory.Create<SetTenant>(ActingIn(Tenant), Settings(after: Setting("300", SettingScope.TenantLevel)).Object, Audit(succeeds: false).Object, Roles(false));

        await endpoint.HandleAsync(new SetTenantInheritedSettingRequest { TenantId = Tenant, SettingName = "MaxRows", Value = "300" }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(500);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task OnlyAPlatformAdministratorLiftsATenantsLock()
    {
        var settings = Settings(after: Setting("300", SettingScope.TenantLevel));
        var endpoint = Factory.Create<LiftTenant>(ActingIn(Tenant), settings.Object, Audit().Object, Roles(false));

        await endpoint.HandleAsync(new TenantInheritedSettingKeyRequest { TenantId = Tenant, SettingName = "MaxRows" }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(403);
        settings.Verify(s => s.Lift(It.IsAny<SettingScope>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ALiftIsRecordedAtThePlatformLevelWithTheOldValue()
    {
        var recorded = new List<AdministrativeChangeEntry>();
        var endpoint = Factory.Create<LiftTenant>(ActingIn(OtherTenant), Settings(after: Setting("300", SettingScope.TenantLevel)).Object, Audit(recorded).Object, Roles(true));

        await endpoint.HandleAsync(new TenantInheritedSettingKeyRequest { TenantId = Tenant, SettingName = "MaxRows" }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        var entry = recorded.ShouldHaveSingleItem();
        entry.ChangeKind.ShouldBe("SettingLockLifted");
        entry.ItemId.ShouldBe($"{Tenant}:MaxRows");
        entry.Level.Name.ShouldBe("Platform");
        entry.OldValue.ShouldNotBeNull();
        entry.NewValue.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ATenantReadsItsOwnSettingsButNotAnotherTenants()
    {
        var settings = new Mock<IInheritedSettings>();
        settings.Setup(s => s.List(It.IsAny<SettingScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<EffectiveSetting>>.Success([Setting("500", SettingScope.PlatformLevel)]));
        var own = Factory.Create<ListTenant>(ActingIn(Tenant), settings.Object, Roles(false));
        var other = Factory.Create<ListTenant>(ActingIn(OtherTenant), settings.Object, Roles(false));

        await own.HandleAsync(new TenantInheritedSettingsRequest { TenantId = Tenant }, CancellationToken.None);
        await other.HandleAsync(new TenantInheritedSettingsRequest { TenantId = Tenant }, CancellationToken.None);

        own.HttpContext.Response.StatusCode.ShouldBe(200);
        own.Response.Settings.ShouldHaveSingleItem().Name.ShouldBe("MaxRows");
        other.HttpContext.Response.StatusCode.ShouldBe(403);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AnOrganizationSetIsRecordedAtTheOrganizationLevelNamingTheOrganization()
    {
        var recorded = new List<AdministrativeChangeEntry>();
        var endpoint = Factory.Create<SetOrganization>(ActingIn(Tenant), Settings(after: Setting("100", SettingScope.OrganizationLevel)).Object, Audit(recorded).Object);

        await endpoint.HandleAsync(new SetOrganizationInheritedSettingRequest { OrganizationId = Finance, SettingName = "MaxRows", Value = "100" }, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        var entry = recorded.ShouldHaveSingleItem();
        entry.Level.Name.ShouldBe("Organization");
        entry.OrganizationId.ShouldBe(Finance);
        entry.ItemId.ShouldBe($"{Finance}:MaxRows");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task APlatformSetIsForPlatformAdministratorsOnly()
    {
        var settings = Settings(after: Setting("400", SettingScope.PlatformLevel, SettingScope.PlatformLevel));
        var refused = Factory.Create<SetPlatform>(ActingIn(Tenant), settings.Object, Audit().Object, Roles(false));
        var recorded = new List<AdministrativeChangeEntry>();
        var allowed = Factory.Create<SetPlatform>(ActingIn(Tenant), settings.Object, Audit(recorded).Object, Roles(true));

        await refused.HandleAsync(new SetPlatformInheritedSettingRequest { SettingName = "MaxRows", Value = "400", Locked = true }, CancellationToken.None);
        await allowed.HandleAsync(new SetPlatformInheritedSettingRequest { SettingName = "MaxRows", Value = "400", Locked = true }, CancellationToken.None);

        refused.HttpContext.Response.StatusCode.ShouldBe(403);
        allowed.HttpContext.Response.StatusCode.ShouldBe(200);
        recorded.ShouldHaveSingleItem().Level.Name.ShouldBe("Platform");
    }
}
