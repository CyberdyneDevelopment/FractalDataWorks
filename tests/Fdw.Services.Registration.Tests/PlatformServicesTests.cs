using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Fdw.ServiceTypes;
using Shouldly;
using Xunit;

namespace Fdw.Services.Registration.Tests;

/// <summary>
/// Tests for <see cref="PlatformServices"/> and <see cref="PlatformServiceEntry"/>. Each test resets
/// the process-global registry first via <see cref="PlatformServices.ResetForTesting"/> — this static
/// registry is a one-time, freeze-once bootstrap structure by design (see its own remarks), so tests
/// must explicitly return it to the unfrozen, empty state rather than relying on any built-in isolation.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "CoreFramework")]
public sealed class PlatformServicesTests : IDisposable
{
    public PlatformServicesTests() => PlatformServices.ResetForTesting();

    public void Dispose() => PlatformServices.ResetForTesting();

    private sealed class CallCounter
    {
        public int InitializeCalls { get; private set; }
        public int ConfigureCalls { get; private set; }
        public int RegisterCalls { get; private set; }

        public ServiceTypeCollectionDescriptor Descriptor(string category, Type collectionType) => new(
            category,
            collectionType,
            (builder, _) => { ConfigureCalls++; return builder; },
            (builder, _) => { RegisterCalls++; return builder; },
            (services, _) => { InitializeCalls++; return services; });
    }

    [Fact]
    public void AddReturnsNewEntry()
    {
        var counter = new CallCounter();
        var entry = PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)), 0);

        entry.CategoryName.ShouldBe("Widget");
        entry.Group.ShouldBe(0);
        entry.Initialized.ShouldBeFalse();
    }

    [Fact]
    public void AddWithSameCategoryAndSameCollectionTypeIsIdempotent()
    {
        var counter = new CallCounter();
        var first = PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)), 0);
        var second = PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)), 0);

        second.ShouldBeSameAs(first);
    }

    [Fact]
    public void AddWithSameCategoryButDifferentCollectionTypeThrows()
    {
        var counter = new CallCounter();
        PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)), 0);

        Should.Throw<InvalidOperationException>(() =>
            PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(int)), 0));
    }

    [Fact]
    public void AddAfterFreezeThrows()
    {
        var counter = new CallCounter();
        PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)), 0);
        PlatformServices.Entries(); // triggers freeze

        Should.Throw<InvalidOperationException>(() =>
            PlatformServices.Add("Gadget", counter.Descriptor("Gadget", typeof(int)), 0));
    }

    [Fact]
    public void AddOrdersEntriesByGroupAscendingRegardlessOfInsertionOrder()
    {
        // Why: Group is now generator-emitted straight from [ServiceTypeCollection(Group = n)] — each
        // domain declares its own layer via the Add(..., group) argument; there is no SetGroup override.
        var counter = new CallCounter();
        PlatformServices.Add("Second", counter.Descriptor("Second", typeof(string)), 1);
        PlatformServices.Add("First", counter.Descriptor("First", typeof(int)), 0);

        var entries = PlatformServices.Entries();
        entries[0].CategoryName.ShouldBe("First");
        entries[1].CategoryName.ShouldBe("Second");
    }

    [Fact]
    public void AddWithManualTrueExcludesDomainFromSweepsButKeepsEntryDotWalkDrivable()
    {
        var swept = new CallCounter();
        var declaredManual = new CallCounter();
        PlatformServices.Add("Swept", swept.Descriptor("Swept", typeof(string)), 0);
        var manualEntry = PlatformServices.Add("Declared", declaredManual.Descriptor("Declared", typeof(int)), 0, manual: true);

        // The Manual indicator is visible on the entry so a host can see the domain is handled out-of-band.
        manualEntry.Manual.ShouldBeTrue();

        var builder = Host.CreateApplicationBuilder();

        var services = builder.Services;
        PlatformServices.Register(builder);
        PlatformServices.Initialize(services.BuildServiceProvider());

        // The swept domain runs in the sweep; the Manual domain is skipped by it.
        swept.RegisterCalls.ShouldBe(1);
        swept.InitializeCalls.ShouldBe(1);
        declaredManual.RegisterCalls.ShouldBe(0);
        declaredManual.InitializeCalls.ShouldBe(0);

        // But the Manual domain's entry stays dot-walkable and fully drivable by the host — the sweep
        // did not touch it, so this is the first (and only) time its Register runs.
        manualEntry.Register(Host.CreateApplicationBuilder());
        declaredManual.RegisterCalls.ShouldBe(1);
    }

    [Fact]
    public void EntryInitializeIsIdempotent()
    {
        var counter = new CallCounter();
        var entry = PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)), 0);

        entry.Initialize(EmptyServiceProvider());
        entry.Initialize(EmptyServiceProvider());

        counter.InitializeCalls.ShouldBe(1);
        entry.Initialized.ShouldBeTrue();
    }

    [Fact]
    public void EntryRegisterIsIdempotent()
    {
        var counter = new CallCounter();
        var entry = PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)), 0);

        entry.Register(Host.CreateApplicationBuilder());
        entry.Register(Host.CreateApplicationBuilder());

        counter.RegisterCalls.ShouldBe(1);
        entry.Registered.ShouldBeTrue();
    }

    [Fact]
    public void RegisterSweepSkipsAlreadyManuallyRegisteredEntry()
    {
        var counter = new CallCounter();
        var entry = PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)), 0);

        // Manual, dot-walked register (mirrors PlatformServices.Widget?.Register(...) usage).
        entry.Register(Host.CreateApplicationBuilder());
        counter.RegisterCalls.ShouldBe(1);

        PlatformServices.Register(Host.CreateApplicationBuilder());

        // Register sweep must not re-run the already-registered entry.
        counter.RegisterCalls.ShouldBe(1);
    }

    [Fact]
    public void InitializeSkipsAlreadyManuallyInitializedEntry()
    {
        var counter = new CallCounter();
        var entry = PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)), 0);

        // Manual, dot-walked initialize (mirrors PlatformServices.Widget?.Initialize(...) usage).
        entry.Initialize(EmptyServiceProvider());
        counter.InitializeCalls.ShouldBe(1);

        PlatformServices.Initialize(EmptyServiceProvider());

        // Initialize must not re-run the already-initialized entry.
        counter.InitializeCalls.ShouldBe(1);
    }

    [Fact]
    public void InitializeRunsEveryEntryExactlyOnceInGroupOrder()
    {
        var counterA = new CallCounter();
        var counterB = new CallCounter();
        PlatformServices.Add("B", counterB.Descriptor("B", typeof(string)), 1);
        PlatformServices.Add("A", counterA.Descriptor("A", typeof(int)), 0);

        PlatformServices.Initialize(EmptyServiceProvider());

        counterA.InitializeCalls.ShouldBe(1);
        counterB.InitializeCalls.ShouldBe(1);
    }

    [Fact]
    public void ConfigureAndRegisterInvokeEveryEntry()
    {
        var counter = new CallCounter();
        PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)), 0);

        PlatformServices.Configure(EmptyHostApplicationBuilder());
        PlatformServices.Register(Host.CreateApplicationBuilder());

        counter.ConfigureCalls.ShouldBe(1);
        counter.RegisterCalls.ShouldBe(1);
    }

    // ── Phase-delegate override (author-variant selection; keyset stays frozen) ──────────────────────

    [Fact]
    public void OverrideRegisterReplacesDescriptorRegisterDelegate()
    {
        var counter = new CallCounter();
        var entry = PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)), 0);
        var overrideRan = 0;
        entry.OverrideRegister((builder, _) => { overrideRan++; return builder; });

        entry.Register(Host.CreateApplicationBuilder());

        overrideRan.ShouldBe(1);
        counter.RegisterCalls.ShouldBe(0); // descriptor default did NOT run
        entry.Registered.ShouldBeTrue();
    }

    [Fact]
    public void OverrideInitializeReplacesDescriptorInitializeDelegate()
    {
        var counter = new CallCounter();
        var entry = PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)), 0);
        var overrideRan = 0;
        entry.OverrideInitialize((services, _) => { overrideRan++; return services; });

        entry.Initialize(EmptyServiceProvider());

        overrideRan.ShouldBe(1);
        counter.InitializeCalls.ShouldBe(0);
        entry.Initialized.ShouldBeTrue();
    }

    [Fact]
    public void OverrideConfigureReplacesDescriptorConfigureDelegate()
    {
        var counter = new CallCounter();
        var entry = PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)), 0);
        var overrideRan = 0;
        entry.OverrideConfigure((builder, _) => { overrideRan++; return builder; });

        entry.Configure(EmptyHostApplicationBuilder());

        overrideRan.ShouldBe(1);
        counter.ConfigureCalls.ShouldBe(0);
    }

    [Fact]
    public void WithoutOverrideTheDescriptorDefaultRunsVerbatim()
    {
        var counter = new CallCounter();
        var entry = PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)), 0);

        entry.Configure(EmptyHostApplicationBuilder());
        entry.Register(Host.CreateApplicationBuilder());
        entry.Initialize(EmptyServiceProvider());

        counter.ConfigureCalls.ShouldBe(1);
        counter.RegisterCalls.ShouldBe(1);
        counter.InitializeCalls.ShouldBe(1);
    }

    [Fact]
    public void OverrideRegisterAfterRegisterHasRunThrows()
    {
        var counter = new CallCounter();
        var entry = PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)), 0);
        entry.Register(Host.CreateApplicationBuilder());

        Should.Throw<InvalidOperationException>(() => entry.OverrideRegister((builder, _) => builder));
    }

    [Fact]
    public void OverrideInitializeAfterInitializeHasRunThrows()
    {
        var counter = new CallCounter();
        var entry = PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)), 0);
        entry.Initialize(EmptyServiceProvider());

        Should.Throw<InvalidOperationException>(() => entry.OverrideInitialize((services, _) => services));
    }

    [Fact]
    public void OverrideConfigureAfterConfigureHasRunThrows()
    {
        var counter = new CallCounter();
        var entry = PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)), 0);
        entry.Configure(EmptyHostApplicationBuilder());

        Should.Throw<InvalidOperationException>(() => entry.OverrideConfigure((b, _) => b));
    }

    [Fact]
    public void RegisterSweepUsesTheSelectedOverride()
    {
        var counter = new CallCounter();
        var entry = PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)), 0);
        var overrideRan = 0;
        entry.OverrideRegister((builder, _) => { overrideRan++; return builder; });

        PlatformServices.Register(Host.CreateApplicationBuilder());

        overrideRan.ShouldBe(1);
        counter.RegisterCalls.ShouldBe(0);
    }

    [Fact]
    public void OverrideReturnsSameEntryForFluentChaining()
    {
        var counter = new CallCounter();
        var entry = PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)), 0);

        entry.OverrideRegister((builder, _) => builder).ShouldBeSameAs(entry);
    }

    private static IServiceProvider EmptyServiceProvider() => new ServiceCollection().BuildServiceProvider();

    private static IHostApplicationBuilder EmptyHostApplicationBuilder() => Host.CreateApplicationBuilder();
}
