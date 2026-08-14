using System;
using System.Linq;
using System.Net.Http;
using Fdw.ServiceTypes;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Identity;
using Fdw.Services.Identity.Abstractions;
using Fdw.Services.Identity.Authentik;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Identity.Tests;

/// <summary>
/// Boots the identity domain through the real three-phase registration inside a real host builder.
/// </summary>
/// <remarks>
/// <para>
/// This is the test that answers "does it integrate" rather than "does it compile". Everything else in
/// this suite exercises a class directly; this one runs <c>Configure</c>, <c>Register</c>,
/// <c>Build()</c> and <c>Initialize</c> in the order a real entry-point app runs them, and then
/// resolves out of the built container.
/// </para>
/// <para>
/// The domain is NOT given a host service type. Host service types were removed from the framework —
/// <c>PlatformServices</c> is the host, and a domain joins it as a sibling category. A host type here
/// would be reintroducing the thing that was deleted.
/// </para>
/// </remarks>
[Trait("Priority", "P0")]
[Trait("Category", "Configuration")]
public sealed class IdentityHostBootTests
{
    // Why force: the collection tracks each phase run-once, so a second test running Configure would
    // otherwise be skipped and assert nothing. force re-runs the body against this test's own builder.
    private const bool Force = true;

    private static HostApplicationBuilder Builder()
    {
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());

        // The prerequisites a real entry-point app supplies before any domain registers. Named here
        // rather than mocked away, because discovering exactly this list is the point of the test.
        builder.Services.AddHttpClient();
        builder.Services.AddSingleton(new Lazy<IConfigurationGateway>(() =>
            throw new InvalidOperationException("No gateway is needed to register the domain; a read would need one.")));

        return builder;
    }

    [Theory]
    [InlineData("AuthentikClientCredentials")]
    [InlineData("AuthentikJwtFederation")]
    [InlineData("FdwOpenIddict")]
    public void EachMechanismRegistersItsFactoryWithTheDomainProvider(string mechanism)
    {
        // This is the assertion that proves the option attached AND its Register body ran: the
        // provider's factory registry is populated per mechanism only by that option's own
        // registration. The collection's Options array is deliberately protected, so the registry is
        // the real observable rather than an accessor reaching past the design.
        //
        // The failure this guards against is the one the framework calls out by name: an option whose
        // Register body never put its factory in the registry resolves to "No registered service type
        // matches ServiceOptionType" at the first request, which reads like a configuration fault and
        // is not one.
        var builder = Builder();
        IdentityServiceTypes.Configure(builder, NullLoggerFactory.Instance, Force);
        IdentityServiceTypes.Register(builder, NullLoggerFactory.Instance, Force);

        using var host = builder.Build();
        using var scope = host.Services.CreateScope();

        var provider = scope.ServiceProvider
            .GetRequiredService<IFdwServiceProvider<IIdentityService, IdentityServiceConfiguration>>();

        // A configuration naming a registered mechanism must not fail with "no factory registered".
        // It fails for want of a typed body instead, which is the next gate and a different message.
        var built = provider.Get(
            new IdentityServiceConfiguration { Name = "probe", ServiceOptionType = mechanism },
            TestContext.Current.CancellationToken).GetAwaiter().GetResult();

        built.IsFailure.ShouldBeTrue();
        built.CurrentMessage!.ShouldNotContain("NoFactoryRegistered");
    }

    [Fact]
    public void ConfigureAndRegisterSucceedAgainstARealHostBuilder()
    {
        var builder = Builder();
        var loggerFactory = NullLoggerFactory.Instance;

        var configured = IdentityServiceTypes.Configure(builder, loggerFactory, Force);
        configured.IsSuccess.ShouldBeTrue(configured.CurrentMessage);

        var registered = IdentityServiceTypes.Register(builder, loggerFactory, Force);
        registered.IsSuccess.ShouldBeTrue(registered.CurrentMessage);
    }

    [Fact]
    public void TheDomainProviderResolvesOutOfTheBuiltContainer()
    {
        var builder = Builder();
        IdentityServiceTypes.Configure(builder, NullLoggerFactory.Instance, Force);
        IdentityServiceTypes.Register(builder, NullLoggerFactory.Instance, Force);

        using var host = builder.Build();
        using var scope = host.Services.CreateScope();

        // Why scoped: the generator registers every domain provider AddScoped, so resolving from the
        // root container would be the capture bug the framework warns about.
        scope.ServiceProvider
            .GetService<IFdwServiceProvider<IIdentityService, IdentityServiceConfiguration>>()
            .ShouldNotBeNull();
    }

    [Fact]
    public void TheTokenCacheIsRegisteredAsASingletonSoOneTokenServesTheProcess()
    {
        var builder = Builder();
        IdentityServiceTypes.Configure(builder, NullLoggerFactory.Instance, Force);
        IdentityServiceTypes.Register(builder, NullLoggerFactory.Instance, Force);

        using var host = builder.Build();

        var first = host.Services.GetService<IIdentityTokenCache>();
        first.ShouldNotBeNull();

        // A scoped cache would acquire a new token per scope, which is the behaviour the cache exists
        // to prevent — so this asserts identity across scopes, not merely that one is registered.
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetService<IIdentityTokenCache>().ShouldBeSameAs(first);
    }

    [Fact]
    public void TheIdentityHttpClientIsRegisteredUnderItsOwnName()
    {
        var builder = Builder();
        IdentityServiceTypes.Configure(builder, NullLoggerFactory.Instance, Force);
        IdentityServiceTypes.Register(builder, NullLoggerFactory.Instance, Force);

        using var host = builder.Build();

        host.Services.GetRequiredService<IHttpClientFactory>()
            .CreateClient(Fdw.Services.Identity.IdentityHttpClient.Name)
            .ShouldNotBeNull();
    }

    [Fact]
    public void InitializeSucceedsAfterBuild()
    {
        var builder = Builder();
        IdentityServiceTypes.Configure(builder, NullLoggerFactory.Instance, Force);
        IdentityServiceTypes.Register(builder, NullLoggerFactory.Instance, Force);

        using var host = builder.Build();

        var initialized = IdentityServiceTypes.Initialize(host, NullLoggerFactory.Instance, Force);
        initialized.IsSuccess.ShouldBeTrue(initialized.CurrentMessage);
    }
}
