using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions.Configuration;
using Fdw.Services.Settings;
using Fdw.Services.Settings.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Descriptions.Tests;

/// <summary>
/// The language a description is read in, when nobody is signed in (FDW-7203): a read made by background code (the notification rule notifier, a threshold, a timer) has no
/// authentication context, so the setting <c>DescriptionLanguage</c> is resolved with no tenant, and the only scope such a read reaches is the server's. A host that states
/// the language only for tenants fails every such read with <c>DescriptionLanguageNotStated</c>, and nothing here supplies a language to get round it.
/// </summary>
/// <remarks>
/// The real <see cref="DefaultEffectiveSettingsProvider"/> and the real <see cref="DescriptionService.Language"/>, over stand-ins for the three setting providers; no description is read.
/// </remarks>
[Trait("Category", "Descriptions")]
public sealed class DescriptionLanguageWithoutACallerTests
{
    private static readonly Guid TenantA = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");

    private readonly Mock<IServerSettingConfigurationProvider> _server = new(MockBehavior.Loose);
    private readonly Mock<ITenantSettingConfigurationProvider> _tenant = new(MockBehavior.Loose);
    private readonly Mock<IRoleSettingConfigurationProvider> _role = new(MockBehavior.Loose);
    private readonly List<TenantSettingImplementationConfiguration> _tenantSettings = [];

    public DescriptionLanguageWithoutACallerTests()
    {
        _tenant
            .Setup(p => p.Get(It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult<IGenericResult<IReadOnlyList<ITenantSettingImplementationConfiguration>>>(
                GenericResult<IReadOnlyList<ITenantSettingImplementationConfiguration>>.Success([.. _tenantSettings])));
        _role
            .Setup(p => p.Get(It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult<IGenericResult<IReadOnlyList<IRoleSettingImplementationConfiguration>>>(
                GenericResult<IReadOnlyList<IRoleSettingImplementationConfiguration>>.Success([])));
    }

    // The server states the setting as given (a blank value is a server row that states no language), or has no row for it at all when none is given.
    private void TheServerStates(string? value)
        => _server
            .Setup(p => p.Get(SettingDefinitions.DescriptionLanguage, It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult<IGenericResult<IServerSettingImplementationConfiguration>>(
                value is null
                    ? GenericResult<IServerSettingImplementationConfiguration>.Failure(new Fdw.Messages.GenericMessage("no such setting"))
                    : GenericResult<IServerSettingImplementationConfiguration>.Success(
                        new ServerSettingImplementationConfiguration { Name = SettingDefinitions.DescriptionLanguage, SettingValue = value, DataType = "String", IsActive = true })));

    private DescriptionService Service(Guid? signedInTenant)
    {
        var accessor = new Mock<IAuthenticationContextAccessor>();
        accessor.SetupGet(a => a.Current).Returns(signedInTenant is null ? null : Mock.Of<IAuthenticationContext>(context => context.ActiveTenantId == signedInTenant));
        var settings = new DefaultEffectiveSettingsProvider(_server.Object, _tenant.Object, _role.Object, NullLogger<DefaultEffectiveSettingsProvider>.Instance);

        return new DescriptionService(
            new Mock<IDescriptionConfigurationProvider>(MockBehavior.Strict).Object, settings, new Mock<IConfigurationGatewayProvider>().Object, accessor.Object, 4000,
            NullLogger<DescriptionService>.Instance);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ACallerlessReadAnswersTheLanguageTheServerStates()
    {
        TheServerStates("en-US");

        var language = Service(signedInTenant: null).Language();

        language.IsSuccess.ShouldBeTrue();
        language.Value.ShouldBe("en-US");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ACallerlessReadFailsWhenTheLanguageIsStatedOnlyForATenantAndTheServerStatesNone()
    {
        TheServerStates(string.Empty);
        _tenantSettings.Add(new() { TenantId = TenantA, SettingName = SettingDefinitions.DescriptionLanguage, SettingValue = "en-US", IsActive = true });

        var language = Service(signedInTenant: null).Language();

        language.IsFailure.ShouldBeTrue();
        language.Code!.Name.ShouldBe("DescriptionLanguageNotStated");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ACallerlessReadFailsWhenTheServerHasNoRowForTheSetting()
    {
        TheServerStates(null);

        var language = Service(signedInTenant: null).Language();

        language.IsFailure.ShouldBeTrue();
        language.Code!.Name.ShouldBe("DescriptionLanguageNotStated");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ACallerInATenantReadsTheLanguageThatTenantStatesWhereTheServerStatesNone()
    {
        TheServerStates(string.Empty);
        _tenantSettings.Add(new() { TenantId = TenantA, SettingName = SettingDefinitions.DescriptionLanguage, SettingValue = "fr-FR", IsActive = true });

        var language = Service(signedInTenant: TenantA).Language();

        language.IsSuccess.ShouldBeTrue();
        language.Value.ShouldBe("fr-FR");
    }
}
