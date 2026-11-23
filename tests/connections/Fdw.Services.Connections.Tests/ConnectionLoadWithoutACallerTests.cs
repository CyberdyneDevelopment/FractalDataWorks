using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions;
using Fdw.Services.Descriptions.Configuration;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.MsSql;
using Fdw.Services.Settings;
using Fdw.Services.Settings.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Tests;

/// <summary>
/// The data gateway resolves a connection with no caller to ask for (<c>DataGatewayService.ResolveConnectionType</c>, before every cached command), and the connection health monitor
/// and the health check read every connection in the system context (<c>ConnectionHealthMonitorWorker</c>, <c>ConnectionsHealthCheckable</c>); a connection's description is now
/// filled on every such read (FDW-6208). With nobody authenticated, or a tenant-less system context, <c>DescriptionService.Language()</c> asks for the <c>DescriptionLanguage</c>
/// setting with no tenant, which the real <c>DefaultEffectiveSettingsProvider</c> answers from the SERVER setting alone. These tests run the real description service and the real
/// settings provider (only the tables are stand-ins) to prove both sides: the read answers when the server setting is stated, and fails with <c>DescriptionLanguageNotStated</c>
/// when it is not.
/// </summary>
[Trait("Category", "Descriptions")]
public sealed class ConnectionLoadWithoutACallerTests
{
    private static readonly Guid Warehouse = Guid.Parse("00000000-0000-0000-0000-0000000000d1");

    private readonly Mock<IServerSettingConfigurationProvider> _server = new(MockBehavior.Strict);
    private readonly Mock<ITenantSettingConfigurationProvider> _tenant = new(MockBehavior.Strict);
    private readonly Mock<IRoleSettingConfigurationProvider> _role = new(MockBehavior.Strict);
    private readonly Mock<IDescriptionConfigurationProvider> _descriptions = new(MockBehavior.Strict);

    // The load with no caller: the connection provider over the real description service, the real settings provider and nobody authenticated.
    private ConnectionConfigurationProvider Reader()
    {
        var accessor = new Mock<IAuthenticationContextAccessor>();
        accessor.SetupGet(a => a.Current).Returns((IAuthenticationContext?)null);
        var gateways = new Mock<IConfigurationGatewayProvider>();
        gateways.SetupGet(g => g.KeyChunkSize).Returns(100);
        var settings = new DefaultEffectiveSettingsProvider(_server.Object, _tenant.Object, _role.Object, NullLogger<DefaultEffectiveSettingsProvider>.Instance);
        var descriptions = new DescriptionService(_descriptions.Object, settings, gateways.Object, accessor.Object, 4000, NullLogger<DescriptionService>.Instance);
        return new ConnectionConfigurationProvider(
            NullLogger<ConnectionConfigurationProvider>.Instance, gateways.Object, "PlatformConfiguration", "conn", "Connection", descriptions, descriptions);
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
    public async Task AReadWithNoCallerAnswersWhenTheServerSettingIsStatedAndNeverReadsATenantOrRoleSetting()
    {
        TheServerSettingIs(Stated("en-US"));
        _descriptions
            .Setup(d => d.Get(It.IsAny<DomainQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => GenericResult<ConfigurationPage<DescriptionDomainConfiguration>>.Success(
                new ConfigurationPage<DescriptionDomainConfiguration>(0, int.MaxValue - 1, false, _ => Stream(
                [
                    new DescriptionDomainConfiguration
                    {
                        Name = "Connection/Text/00000000-0000-0000-0000-0000000000d1/en-US/1", DescriptionType = "Connection", DescriptionSubType = "Text",
                        ThingId = Warehouse, Language = "en-US", Ordinal = 1, ThingName = "warehouse", Text = "The warehouse database",
                    },
                ]))));
        var connections = new List<IConnectionImplementationConfiguration> { new MsSqlConnectionConfiguration { Id = Warehouse, Name = "warehouse" } };

        var read = await Reader().Read(connections, null, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue();
        connections[0].Description.ShouldBe("The warehouse database");
        _tenant.VerifyNoOtherCalls();
        _role.VerifyNoOtherCalls();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AReadWithNoCallerFailsWithLanguageNotStatedWhenTheServerSettingIsMissingAndReadsNoDescription()
    {
        TheServerSettingIs(null);
        var connections = new List<IConnectionImplementationConfiguration> { new MsSqlConnectionConfiguration { Id = Warehouse, Name = "warehouse" } };

        var read = await Reader().Read(connections, null, TestContext.Current.CancellationToken);

        read.IsFailure.ShouldBeTrue();
        read.Code!.Name.ShouldBe("DescriptionLanguageNotStated");
        connections[0].Description.ShouldBeNull();
        _descriptions.VerifyNoOtherCalls();
        _tenant.VerifyNoOtherCalls();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AReadWithNoCallerFailsWithLanguageNotStatedWhenTheServerSettingIsInactive()
    {
        var inactive = Stated("en-US");
        inactive.IsActive = false;
        TheServerSettingIs(inactive);
        var connections = new List<IConnectionImplementationConfiguration> { new MsSqlConnectionConfiguration { Id = Warehouse, Name = "warehouse" } };

        var read = await Reader().Read(connections, null, TestContext.Current.CancellationToken);

        read.IsFailure.ShouldBeTrue();
        read.Code!.Name.ShouldBe("DescriptionLanguageNotStated");
        _descriptions.VerifyNoOtherCalls();
    }
}
