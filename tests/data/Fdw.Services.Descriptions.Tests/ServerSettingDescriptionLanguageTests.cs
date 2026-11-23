using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions.Configuration;
using Fdw.Services.Settings;
using Fdw.Services.Settings.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Descriptions.Tests;

/// <summary>
/// A server setting's description is written with the real description service (FDW-6208), which reads the description language through the effective settings, which read through
/// the server setting provider: the provider takes the writer as a <c>Lazy</c> so the three can be built from one container, in either order. Saving a setting WITH a text on a host that
/// states no <c>DescriptionLanguage</c> fails with <c>DescriptionLanguageNotStated</c> and writes nothing; saving one WITHOUT a text still works, which is how the language itself is
/// stated on a fresh host.
/// </summary>
/// <remarks>The container here mirrors the shape <c>DefaultSettingsServiceType</c> registers (the <c>Lazy</c> beside the effective settings provider's); only the tables are stand-ins.</remarks>
[Trait("Category", "Descriptions")]
public sealed class ServerSettingDescriptionLanguageTests
{
    private static readonly Guid SettingId = Guid.Parse("00000000-0000-0000-0000-0000000000d1");

    private readonly Mock<IServerSettingConfigurationProvider> _server = new(MockBehavior.Loose);
    private readonly Mock<ITenantSettingConfigurationProvider> _tenant = new(MockBehavior.Loose);
    private readonly Mock<IRoleSettingConfigurationProvider> _role = new(MockBehavior.Loose);
    private readonly Mock<IDescriptionConfigurationProvider> _descriptions = new(MockBehavior.Strict);
    private readonly Mock<IConfigurationGatewayProvider> _gateways = new(MockBehavior.Loose);

    public ServerSettingDescriptionLanguageTests()
    {
        _tenant.Setup(p => p.Get(It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IReadOnlyList<ITenantSettingImplementationConfiguration>>.Success([]));
        _role.Setup(p => p.Get(It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IReadOnlyList<IRoleSettingImplementationConfiguration>>.Success([]));
    }

    // The server has no row for the language, or states the one given.
    private void TheServerStates(string? language)
        => _server
            .Setup(p => p.Get(SettingDefinitions.DescriptionLanguage, It.IsAny<CancellationToken>()))
            .ReturnsAsync(language is null
                ? GenericResult<IServerSettingImplementationConfiguration>.Failure(new Fdw.Messages.GenericMessage("no such setting"))
                : GenericResult<IServerSettingImplementationConfiguration>.Success(
                    new ServerSettingImplementationConfiguration { Name = SettingDefinitions.DescriptionLanguage, SettingValue = language, DataType = "String", IsActive = true }));

    // One container shaped like the registration: the provider, the effective settings over it and the description service over those, with the Lazy beside them. Nothing is read.
    private ServiceProvider Container()
    {
        var accessor = new Mock<IAuthenticationContextAccessor>();
        accessor.SetupGet(a => a.Current).Returns((IAuthenticationContext?)null);
        var services = new ServiceCollection();
        services.AddSingleton(sp => new ServerSettingConfigurationProvider(
            NullLogger<ServerSettingConfigurationProvider>.Instance, _gateways.Object, "PlatformConfiguration", "settings", "ServerSetting", sp.GetRequiredService<Lazy<IDescriptionWriter>>()));
        services.AddSingleton<IServerSettingConfigurationProvider>(sp => sp.GetRequiredService<ServerSettingConfigurationProvider>());
        services.AddSingleton(sp => new Lazy<IDescriptionWriter>(() => sp.GetRequiredService<IDescriptionWriter>()));
        services.AddSingleton<IEffectiveSettingsProvider>(sp => new DefaultEffectiveSettingsProvider(
            sp.GetRequiredService<IServerSettingConfigurationProvider>(), _tenant.Object, _role.Object, NullLogger<DefaultEffectiveSettingsProvider>.Instance));
        services.AddSingleton(sp => new DescriptionService(
            _descriptions.Object, sp.GetRequiredService<IEffectiveSettingsProvider>(), _gateways.Object, accessor.Object, 4000, NullLogger<DescriptionService>.Instance));
        services.AddSingleton<IDescriptionWriter>(sp => sp.GetRequiredService<DescriptionService>());
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    // The provider under test over a description service whose settings read from the stand-in server provider (the real provider reads a database).
    private ServerSettingConfigurationProvider Provider()
    {
        var accessor = new Mock<IAuthenticationContextAccessor>();
        accessor.SetupGet(a => a.Current).Returns((IAuthenticationContext?)null);
        var settings = new DefaultEffectiveSettingsProvider(_server.Object, _tenant.Object, _role.Object, NullLogger<DefaultEffectiveSettingsProvider>.Instance);
        var service = new DescriptionService(_descriptions.Object, settings, _gateways.Object, accessor.Object, 4000, NullLogger<DescriptionService>.Instance);
        return new ServerSettingConfigurationProvider(
            NullLogger<ServerSettingConfigurationProvider>.Instance, _gateways.Object, "PlatformConfiguration", "settings", "ServerSetting", new Lazy<IDescriptionWriter>(() => service));
    }

    // Why a test never lets the container prove a cycle by recursing: Microsoft's container sees a cycle only in constructor injection, so two singleton factories that resolve each other
    // recurse until the stack ends and take the test process with them. The tests below resolve on another thread and fail when that thread does not return, and a control of that kind is
    // left out on purpose.
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(20);

    private static async Task<T> Within<T>(Func<T> resolve)
        => await Task.Run(resolve, TestContext.Current.CancellationToken).WaitAsync(Limit, TestContext.Current.CancellationToken).ConfigureAwait(false);

    [Fact(Timeout = 60000)]
    [Trait("Priority", "P0")]
    public async Task TheProviderAndTheDescriptionServiceResolveFromOneContainerProviderFirst()
    {
        using var container = Container();

        var provider = await Within(() => container.GetRequiredService<ServerSettingConfigurationProvider>());
        var service = await Within(() => container.GetRequiredService<IDescriptionWriter>());

        provider.ShouldNotBeNull();
        service.ShouldBeOfType<DescriptionService>();
    }

    [Fact(Timeout = 60000)]
    [Trait("Priority", "P0")]
    public async Task TheProviderAndTheDescriptionServiceResolveFromOneContainerServiceFirst()
    {
        using var container = Container();

        var service = await Within(() => container.GetRequiredService<IDescriptionWriter>());
        var provider = await Within(() => container.GetRequiredService<ServerSettingConfigurationProvider>());

        service.ShouldBeOfType<DescriptionService>();
        provider.ShouldNotBeNull();
    }

    [Fact(Timeout = 120000)]
    [Trait("Priority", "P0")]
    public async Task TwoThreadsResolvingTheProviderAndTheServiceAtOnceBothReturnWhicheverComesFirst()
    {
        // A first request while the host is still starting: one thread builds the provider, another the description service, released together, on a fresh container each time.
        for (var round = 0; round < 40; round++)
        {
            using var container = Container();
            using var gate = new Barrier(2);
            var provider = Task.Run(() => { gate.SignalAndWait(); return container.GetRequiredService<ServerSettingConfigurationProvider>(); }, TestContext.Current.CancellationToken);
            var service = Task.Run(() => { gate.SignalAndWait(); return container.GetRequiredService<IDescriptionWriter>(); }, TestContext.Current.CancellationToken);

            (await provider.WaitAsync(Limit, TestContext.Current.CancellationToken)).ShouldNotBeNull();
            (await service.WaitAsync(Limit, TestContext.Current.CancellationToken)).ShouldBeOfType<DescriptionService>();
        }
    }

    [Fact(Timeout = 120000)]
    [Trait("Priority", "P0")]
    public async Task TheLazyWriterIsFirstReadAsASaveReadsItWhileAnotherThreadBuildsTheServiceAndNeitherWaitsOnTheOther()
    {
        // A save of a setting with a text reads Lazy<IDescriptionWriter>.Value (ServerSettingConfigurationProvider.Written), which resolves the description service for the first time; the
        // second thread resolves the same service. Both start from a provider that is already built, as a save does.
        for (var round = 0; round < 40; round++)
        {
            using var container = Container();
            _ = await Within(() => container.GetRequiredService<ServerSettingConfigurationProvider>());
            var lazy = container.GetRequiredService<Lazy<IDescriptionWriter>>();
            using var gate = new Barrier(2);
            var save = Task.Run(() => { gate.SignalAndWait(); return lazy.Value; }, TestContext.Current.CancellationToken);
            var other = Task.Run(() => { gate.SignalAndWait(); return container.GetRequiredService<IDescriptionWriter>(); }, TestContext.Current.CancellationToken);

            var first = await save.WaitAsync(Limit, TestContext.Current.CancellationToken);
            var second = await other.WaitAsync(Limit, TestContext.Current.CancellationToken);

            first.ShouldBeOfType<DescriptionService>();
            second.ShouldBeSameAs(first);
        }
    }

    [Fact(Timeout = 60000)]
    [Trait("Priority", "P0")]
    public async Task ASettingSavedWithATextOnAHostThatStatesNoLanguageFailsBeforeTheUnitOpensAndWritesNothing()
    {
        TheServerStates(null);
        var provider = Provider();

        var prepared = await provider.Prepare(
            new ServerSettingImplementationConfiguration { Id = SettingId, Name = "MaxRows", Description = "The most rows a query reads" }, TestContext.Current.CancellationToken);

        prepared.IsFailure.ShouldBeTrue();
        prepared.Code!.Name.ShouldBe("DescriptionLanguageNotStated");
        _descriptions.VerifyNoOtherCalls();
    }

    [Fact(Timeout = 60000)]
    [Trait("Priority", "P0")]
    public async Task ASettingSavedWithNoTextWorksOnAHostThatStatesNoLanguageAndAsksNothing()
    {
        TheServerStates(null);
        var provider = Provider();

        var record = new ServerSettingImplementationConfiguration { Id = SettingId, Name = SettingDefinitions.DescriptionLanguage, Description = null };

        var prepared = await provider.Prepare(record, TestContext.Current.CancellationToken);
        var written = await provider.Written(record, null!, TestContext.Current.CancellationToken);

        prepared.IsSuccess.ShouldBeTrue();
        written.IsSuccess.ShouldBeTrue();
        _descriptions.VerifyNoOtherCalls();
        _server.Verify(p => p.Get(SettingDefinitions.DescriptionLanguage, It.IsAny<CancellationToken>()), Times.Never);
    }
}
