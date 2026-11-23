using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Configuration;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Settings;
using Fdw.Services.Settings.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Authorization.Tests;

/// <summary>
/// A role's description is filled on every read that asks for the whole record (FDW-6208). The platform reads roles with nobody authenticated to ask for (<c>RoleGrants</c> and
/// <c>PermissionHolders</c> do, in a tenant-less system context), and since FDW-7341 they state <c>WithoutFilledMembers</c>, so only a reader that asks for the whole record reaches
/// this path. With no tenant <c>DescriptionService.Language()</c> asks for the <c>DescriptionLanguage</c> setting, which the real
/// <c>DefaultEffectiveSettingsProvider</c> answers from the SERVER setting alone. These tests run the real description service and the real settings provider (only the tables are
/// stand-ins) to prove both sides for a reader that asks for the whole record: it answers when the server setting is stated and fails with <c>DescriptionLanguageNotStated</c> when it is not.
/// </summary>
[Trait("Category", "Descriptions")]
public sealed class RoleLoadWithoutACallerTests
{
    private static readonly Guid AdminId = Guid.Parse("00000000-0000-0000-0000-0000000000a1");

    private readonly Mock<IServerSettingConfigurationProvider> _server = new(MockBehavior.Strict);
    private readonly Mock<ITenantSettingConfigurationProvider> _tenant = new(MockBehavior.Strict);
    private readonly Mock<IRoleSettingConfigurationProvider> _role = new(MockBehavior.Strict);
    private readonly Mock<IDescriptionConfigurationProvider> _descriptions = new(MockBehavior.Strict);

    // The roler's load: the role provider over the real description service, the real settings provider and nobody authenticated.
    private RoleConfigurationProvider Provider()
    {
        var accessor = new Mock<IAuthenticationContextAccessor>();
        accessor.SetupGet(a => a.Current).Returns((IAuthenticationContext?)null);
        var gateways = new Mock<IConfigurationGatewayProvider>();
        gateways.SetupGet(g => g.KeyChunkSize).Returns(100);
        var settings = new DefaultEffectiveSettingsProvider(_server.Object, _tenant.Object, _role.Object, NullLogger<DefaultEffectiveSettingsProvider>.Instance);
        var descriptions = new DescriptionService(_descriptions.Object, settings, gateways.Object, accessor.Object, 4000, NullLogger<DescriptionService>.Instance);
        return new RoleConfigurationProvider(
            NullLogger<RoleConfigurationProvider>.Instance, gateways.Object, "PlatformConfiguration", "authz", "Role",
            Mock.Of<IUserRoleConfigurationProvider>(), Mock.Of<IRolePermissionConfigurationProvider>(), descriptions, descriptions);
    }

    private void TheServerSettingIs(ServerSettingImplementationConfiguration? setting)
        => _server
            .Setup(s => s.Get(SettingDefinitions.DescriptionLanguage, It.IsAny<CancellationToken>()))
            .ReturnsAsync(setting is null
                ? GenericResult<IServerSettingImplementationConfiguration>.Failure(new Fdw.Messages.GenericMessage("no such setting"))
                : GenericResult<IServerSettingImplementationConfiguration>.Success(setting));

    private static ServerSettingImplementationConfiguration Stated(string language)
        => new() { Name = SettingDefinitions.DescriptionLanguage, SettingValue = language, DataType = "String", IsActive = true };

    private static async IAsyncEnumerable<DescriptionDomainConfiguration> Stream(IEnumerable<DescriptionDomainConfiguration> rows)
    {
        foreach (var row in rows)
        {
            yield return row;
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ALoadWithNoCallerAnswersWhenTheServerSettingIsStatedAndNeverReadsATenantOrRoleSetting()
    {
        TheServerSettingIs(Stated("en-US"));
        _descriptions
            .Setup(d => d.Get(It.IsAny<DomainQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => GenericResult<ConfigurationPage<DescriptionDomainConfiguration>>.Success(
                new ConfigurationPage<DescriptionDomainConfiguration>(0, int.MaxValue - 1, false, _ => Stream(
                [
                    new DescriptionDomainConfiguration
                    {
                        Name = "Role/Text/00000000-0000-0000-0000-0000000000a1/en-US/1", DescriptionType = "Role", DescriptionSubType = "Text",
                        ThingId = AdminId, Language = "en-US", Ordinal = 1, ThingName = "admin", Text = "Full system access",
                    },
                ]))));
        var roles = new List<IRoleImplementationConfiguration> { new RoleImplementationConfiguration { Id = AdminId, Name = "admin" } };

        var read = await Provider().Read(roles, null, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue();
        roles[0].Description.ShouldBe("Full system access");
        _tenant.VerifyNoOtherCalls();
        _role.VerifyNoOtherCalls();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ALoadWithNoCallerFailsWithLanguageNotStatedWhenTheServerSettingIsMissingAndReadsNoDescription()
    {
        TheServerSettingIs(null);
        var roles = new List<IRoleImplementationConfiguration> { new RoleImplementationConfiguration { Id = AdminId, Name = "admin" } };

        var read = await Provider().Read(roles, null, TestContext.Current.CancellationToken);

        read.IsFailure.ShouldBeTrue();
        read.Code!.Name.ShouldBe("DescriptionLanguageNotStated");
        roles[0].Description.ShouldBeNull();
        _descriptions.VerifyNoOtherCalls();
        _tenant.VerifyNoOtherCalls();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ALoadWithNoCallerFailsWithLanguageNotStatedWhenTheServerSettingIsInactive()
    {
        var inactive = Stated("en-US");
        inactive.IsActive = false;
        TheServerSettingIs(inactive);
        var roles = new List<IRoleImplementationConfiguration> { new RoleImplementationConfiguration { Id = AdminId, Name = "admin" } };

        var read = await Provider().Read(roles, null, TestContext.Current.CancellationToken);

        read.IsFailure.ShouldBeTrue();
        read.Code!.Name.ShouldBe("DescriptionLanguageNotStated");
        _descriptions.VerifyNoOtherCalls();
    }
}
