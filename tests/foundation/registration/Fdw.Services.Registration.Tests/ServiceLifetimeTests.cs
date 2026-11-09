using System;
using System.Linq;
using Fdw.Results;
using Fdw.ServiceTypes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Registration.Tests;

/// <summary>
/// Tests for the lifetime guard (FDW-4640): Configure builds every host's provider with ValidateScopes and ValidateOnBuild, Register fails when a singleton takes a scoped or a
/// transient service, Initialize refuses a host Configure never touched, and resolving every singleton shows what a factory registration hides from the registration walk.
/// </summary>
[Trait("Category", "CoreFramework")]
[Collection("PlatformServicesRegistry")]
public sealed class ServiceLifetimeTests : IDisposable
{
    public ServiceLifetimeTests() => PlatformServices.ResetForTesting();

    public void Dispose() => PlatformServices.ResetForTesting();

    private interface IScopedThing;

    private sealed class ScopedThing : IScopedThing;

    private interface ITransientThing;

    private sealed class TransientThing : ITransientThing;

    private interface ISingletonThing;

    private sealed class SingletonThing : ISingletonThing;

    private sealed class HoldsScoped(IScopedThing scoped) : ISingletonThing
    {
        public IScopedThing Scoped { get; } = scoped;
    }

    private sealed class HoldsTransient(ITransientThing transient) : ISingletonThing
    {
        public ITransientThing Transient { get; } = transient;
    }

    private sealed class HoldsSingleton(ISingletonThing singleton) : IScopedThing
    {
        public ISingletonThing Singleton { get; } = singleton;
    }

    private static ServiceCollection Services(Action<ServiceCollection> add)
    {
        var services = new ServiceCollection();
        add(services);
        return services;
    }

    [Fact]
    public void ASingletonThatTakesAScopedServiceIsNamed()
    {
        var services = Services(s =>
        {
            s.AddScoped<IScopedThing, ScopedThing>();
            s.AddSingleton<ISingletonThing, HoldsScoped>();
        });

        var findings = ServiceLifetimeCheck.Check(services, NullLogger.Instance, []);

        findings.Count.ShouldBe(1);
        findings[0].Code!.ShouldEndWith("-61022");
    }

    [Fact]
    public void ASingletonThatTakesATransientServiceIsNamedWithItsOwnCode()
    {
        var services = Services(s =>
        {
            s.AddTransient<ITransientThing, TransientThing>();
            s.AddSingleton<ISingletonThing, HoldsTransient>();
        });

        var findings = ServiceLifetimeCheck.Check(services, NullLogger.Instance, []);

        findings.Count.ShouldBe(1);
        findings[0].Code!.ShouldEndWith("-61023");
    }

    private interface IFactoryOf<T>;

    private sealed class FactoryOf<T> : IFactoryOf<T>;

    private sealed class OptionsLike<T>(IFactoryOf<T> factory) : ISingletonThing
    {
        public IFactoryOf<T> Factory { get; } = factory;
    }

    [Fact]
    public void ASingletonThatTakesASkippedServiceTypeIsNotNamed()
    {
        var services = Services(s =>
        {
            s.AddTransient(typeof(IFactoryOf<>), typeof(FactoryOf<>));
            s.AddSingleton<ISingletonThing, OptionsLike<string>>();
        });

        ServiceLifetimeCheck.Check(services, NullLogger.Instance, [typeof(IFactoryOf<>)]).ShouldBeEmpty();
        ServiceLifetimeCheck.Check(services, NullLogger.Instance, []).Count.ShouldBe(1);
    }

    [Fact]
    public void TheFrameworkOptionsSingletonsAreNotNamedByTheDefaultList()
    {
        var services = Services(s => s.AddOptions());

        ServiceLifetimeCheck.Check(services, NullLogger.Instance, PlatformServices.LifetimeCheckSkippedServiceTypes).ShouldBeEmpty();
        ServiceLifetimeCheck.Check(services, NullLogger.Instance, []).Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void RegisterNamesAFrameworkRegistrationOnceTheHostEmptiesTheList()
    {
        PlatformServices.LifetimeCheckSkippedServiceTypes = [];
        PlatformServices.Add("Options", new ServiceTypeCollectionDescriptor(
            "Options", typeof(string),
            (builder, _, _, _) => GenericResult<IHostApplicationBuilder>.Success(builder),
            (builder, _, _, _) =>
            {
                builder.Services.AddOptions();
                return GenericResult<IHostApplicationBuilder>.Success(builder);
            },
            (host, _, _, _) => GenericResult<IHost>.Success(host)));

        PlatformServices.Register(Host.CreateApplicationBuilder()).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void ASingletonThatTakesASingletonAndAScopedServiceThatTakesASingletonAreNotNamed()
    {
        var services = Services(s =>
        {
            s.AddSingleton<ISingletonThing, SingletonThing>();
            s.AddScoped<IScopedThing, HoldsSingleton>();
        });

        ServiceLifetimeCheck.Check(services, NullLogger.Instance, []).ShouldBeEmpty();
    }

    [Fact]
    public void AFactoryRegistrationIsNotReadByTheWalk()
    {
        var services = Services(s =>
        {
            s.AddScoped<IScopedThing, ScopedThing>();
            s.AddSingleton<ISingletonThing>(sp => new HoldsScoped(sp.GetRequiredService<IScopedThing>()));
        });

        ServiceLifetimeCheck.Check(services, NullLogger.Instance, []).ShouldBeEmpty();
    }

    [Fact]
    public void ResolvingEverySingletonNamesWhatAFactoryRegistrationHides()
    {
        var services = Services(s =>
        {
            s.AddScoped<IScopedThing, ScopedThing>();
            s.AddSingleton<ISingletonThing>(sp => new HoldsScoped(sp.GetRequiredService<IScopedThing>()));
        });
        using var provider = services.BuildServiceProvider(ContainerValidation.Options);

        var findings = SingletonResolution.ResolveAll(services, provider, NullLogger.Instance);

        findings.Count.ShouldBe(1);
        findings[0].Code!.ShouldEndWith("-61025");
    }

    [Fact]
    public void ResolvingEverySingletonOfAValidHostFindsNothing()
    {
        var services = Services(s =>
        {
            s.AddScoped<IScopedThing, ScopedThing>();
            s.AddSingleton<ISingletonThing, SingletonThing>();
        });
        using var provider = services.BuildServiceProvider(ContainerValidation.Options);

        SingletonResolution.ResolveAll(services, provider, NullLogger.Instance).ShouldBeEmpty();
    }

    [Fact]
    public void RegisterFailsWhenARegistrationIsCaptive()
    {
        PlatformServices.Add("Captive", new ServiceTypeCollectionDescriptor(
            "Captive", typeof(string),
            (builder, _, _, _) => GenericResult<IHostApplicationBuilder>.Success(builder),
            (builder, _, _, _) =>
            {
                builder.Services.AddScoped<IScopedThing, ScopedThing>();
                builder.Services.AddSingleton<ISingletonThing, HoldsScoped>();
                return GenericResult<IHostApplicationBuilder>.Success(builder);
            },
            (host, _, _, _) => GenericResult<IHost>.Success(host)));

        var registered = PlatformServices.Register(Host.CreateApplicationBuilder());

        registered.IsFailure.ShouldBeTrue();
        registered.Messages.Any(m => m.Code!.EndsWith("-61022", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Fact]
    public void RegisterStartsWithTheFindingsWhenTheHostSetsThemToWarn()
    {
        PlatformServices.LifetimeFindingsFailStartup = false;
        PlatformServices.Add("Captive", new ServiceTypeCollectionDescriptor(
            "Captive", typeof(string),
            (builder, _, _, _) => GenericResult<IHostApplicationBuilder>.Success(builder),
            (builder, _, _, _) =>
            {
                builder.Services.AddScoped<IScopedThing, ScopedThing>();
                builder.Services.AddSingleton<ISingletonThing, HoldsScoped>();
                return GenericResult<IHostApplicationBuilder>.Success(builder);
            },
            (host, _, _, _) => GenericResult<IHost>.Success(host)));

        PlatformServices.Register(Host.CreateApplicationBuilder()).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void RegisterSucceedsWhenNoRegistrationIsCaptive()
    {
        PlatformServices.Add("Fine", new ServiceTypeCollectionDescriptor(
            "Fine", typeof(string),
            (builder, _, _, _) => GenericResult<IHostApplicationBuilder>.Success(builder),
            (builder, _, _, _) =>
            {
                builder.Services.AddScoped<IScopedThing, ScopedThing>();
                builder.Services.AddSingleton<ISingletonThing, SingletonThing>();
                return GenericResult<IHostApplicationBuilder>.Success(builder);
            },
            (host, _, _, _) => GenericResult<IHost>.Success(host)));

        PlatformServices.Register(Host.CreateApplicationBuilder()).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ConfigureBuildsTheHostToRefuseACaptiveConstructor()
    {
        var builder = Host.CreateApplicationBuilder();
        PlatformServices.Configure(builder).IsSuccess.ShouldBeTrue();
        builder.Services.AddScoped<IScopedThing, ScopedThing>();
        builder.Services.AddSingleton<ISingletonThing>(sp => new HoldsScoped(sp.GetRequiredService<IScopedThing>()));

        // ValidateScopes: a scoped service resolved from the root provider is refused, whatever the environment.
        using var host = builder.Build();

        Should.Throw<InvalidOperationException>(() => host.Services.GetRequiredService<ISingletonThing>());
    }

    [Fact]
    public void ConfigureBuildsTheHostToCheckEveryConstructorWhenItBuilds()
    {
        var builder = Host.CreateApplicationBuilder();
        PlatformServices.Configure(builder).IsSuccess.ShouldBeTrue();
        builder.Services.AddSingleton<ISingletonThing, HoldsScoped>();

        // ValidateOnBuild: HoldsScoped needs a service that is not registered, so the host does not build.
        Should.Throw<AggregateException>(() => builder.Build());
    }

    [Fact]
    public void InitializeRefusesAHostThatConfigureNeverTouched()
    {
        using var host = Host.CreateApplicationBuilder().Build();

        var initialized = PlatformServices.Initialize(host);

        initialized.IsFailure.ShouldBeTrue();
        initialized.Messages.Single().Code!.ShouldEndWith("-61024");
    }

    [Fact]
    public void InitializeAcceptsAHostThatConfigureBuilt()
    {
        var builder = Host.CreateApplicationBuilder();
        PlatformServices.Configure(builder).IsSuccess.ShouldBeTrue();
        PlatformServices.Register(builder).IsSuccess.ShouldBeTrue();
        using var host = builder.Build();

        PlatformServices.Initialize(host).IsSuccess.ShouldBeTrue();
    }
}
