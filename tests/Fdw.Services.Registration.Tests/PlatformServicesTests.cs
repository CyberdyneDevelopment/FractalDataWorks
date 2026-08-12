using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Fdw.ServiceTypes;
using Shouldly;
using Xunit;
using Fdw.Results;

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
            (builder, _, _) => { ConfigureCalls++; return GenericResult<IHostApplicationBuilder>.Success(builder); },
            (builder, _, _) => { RegisterCalls++; return GenericResult<IHostApplicationBuilder>.Success(builder); },
            (host, _, _) => { InitializeCalls++; return GenericResult<IHost>.Success(host); });
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

        PlatformServices.Register(builder);
        PlatformServices.Initialize(builder.Build());

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
    public void EntryConfigureIsIdempotent()
    {
        var counter = new CallCounter();
        var entry = PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)), 0);

        entry.Configure(EmptyHostApplicationBuilder());
        entry.Configure(EmptyHostApplicationBuilder());

        counter.ConfigureCalls.ShouldBe(1);
        entry.Configured.ShouldBeTrue();
    }

    // Why this test exists: running a domain early to put it ahead of the others is the documented
    // reason the run-tracking exists at all. Configure previously had no skip, so the early call was
    // paid for twice — once manually and once by the aggregate pass.
    [Fact]
    public void ConfigureSweepSkipsAlreadyManuallyConfiguredEntry()
    {
        var counter = new CallCounter();
        var entry = PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)), 0);

        entry.Configure(EmptyHostApplicationBuilder());
        PlatformServices.Configure(EmptyHostApplicationBuilder());

        counter.ConfigureCalls.ShouldBe(1);
    }

    [Fact]
    public void EntryInitializeIsIdempotent()
    {
        var counter = new CallCounter();
        var entry = PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)), 0);

        entry.Initialize(EmptyHost());
        entry.Initialize(EmptyHost());

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
        entry.Initialize(EmptyHost());
        counter.InitializeCalls.ShouldBe(1);

        PlatformServices.Initialize(EmptyHost());

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

        PlatformServices.Initialize(EmptyHost());

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

    // ── Phase-delegate replacement (author-variant selection; keyset stays frozen) ──────────────────────

    [Fact]
    public void RegistrationReplacesDescriptorRegisterDelegate()
    {
        var counter = new CallCounter();
        var entry = PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)), 0);
        var replacementRan = 0;
        entry.Registration((builder, _) => { replacementRan++; return GenericResult<IHostApplicationBuilder>.Success(builder); });

        entry.Register(Host.CreateApplicationBuilder());

        replacementRan.ShouldBe(1);
        counter.RegisterCalls.ShouldBe(0); // descriptor default did NOT run
        entry.Registered.ShouldBeTrue();
    }

    [Fact]
    public void InitializationReplacesDescriptorInitializeDelegate()
    {
        var counter = new CallCounter();
        var entry = PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)), 0);
        var replacementRan = 0;
        entry.Initialization((host, _) => { replacementRan++; return GenericResult<IHost>.Success(host); });

        entry.Initialize(EmptyHost());

        replacementRan.ShouldBe(1);
        counter.InitializeCalls.ShouldBe(0);
        entry.Initialized.ShouldBeTrue();
    }

    [Fact]
    public void ConfigurationReplacesDescriptorConfigureDelegate()
    {
        var counter = new CallCounter();
        var entry = PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)), 0);
        var replacementRan = 0;
        entry.Configuration((builder, _) => { replacementRan++; return GenericResult<IHostApplicationBuilder>.Success(builder); });

        entry.Configure(EmptyHostApplicationBuilder());

        replacementRan.ShouldBe(1);
        counter.ConfigureCalls.ShouldBe(0);
    }

    [Fact]
    public void WithoutAReplacementTheDescriptorDefaultRunsVerbatim()
    {
        var counter = new CallCounter();
        var entry = PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)), 0);

        entry.Configure(EmptyHostApplicationBuilder());
        entry.Register(Host.CreateApplicationBuilder());
        entry.Initialize(EmptyHost());

        counter.ConfigureCalls.ShouldBe(1);
        counter.RegisterCalls.ShouldBe(1);
        counter.InitializeCalls.ShouldBe(1);
    }

    [Fact]
    public void RegistrationAfterRegisterHasRunThrows()
    {
        var counter = new CallCounter();
        var entry = PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)), 0);
        entry.Register(Host.CreateApplicationBuilder());

        Should.Throw<InvalidOperationException>(() => entry.Registration((builder, _) => GenericResult<IHostApplicationBuilder>.Success(builder)));
    }

    [Fact]
    public void InitializationAfterInitializeHasRunThrows()
    {
        var counter = new CallCounter();
        var entry = PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)), 0);
        entry.Initialize(EmptyHost());

        Should.Throw<InvalidOperationException>(() => entry.Initialization((host, _) => GenericResult<IHost>.Success(host)));
    }

    [Fact]
    public void ConfigurationAfterConfigureHasRunThrows()
    {
        var counter = new CallCounter();
        var entry = PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)), 0);
        entry.Configure(EmptyHostApplicationBuilder());

        Should.Throw<InvalidOperationException>(() => entry.Configuration((b, _) => GenericResult<IHostApplicationBuilder>.Success(b)));
    }

    [Fact]
    public void RegisterSweepUsesTheSelectedReplacement()
    {
        var counter = new CallCounter();
        var entry = PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)), 0);
        var replacementRan = 0;
        entry.Registration((builder, _) => { replacementRan++; return GenericResult<IHostApplicationBuilder>.Success(builder); });

        PlatformServices.Register(Host.CreateApplicationBuilder());

        replacementRan.ShouldBe(1);
        counter.RegisterCalls.ShouldBe(0);
    }

    // Why this test exists separately from ConfigurationReplacesDescriptorConfigureDelegate: that one
    // calls entry.Configure directly, so it passed while the SWEEP reached past the entry to the
    // descriptor and ran the default instead. A replacement that works when invoked by hand and is
    // ignored by the sweep is the failure worth pinning, and only a sweep-level test sees it.
    [Fact]
    public void ConfigureSweepUsesTheSelectedReplacement()
    {
        var counter = new CallCounter();
        var entry = PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)), 0);
        var replacementRan = 0;
        entry.Configuration((builder, _) => { replacementRan++; return GenericResult<IHostApplicationBuilder>.Success(builder); });

        PlatformServices.Configure(Host.CreateApplicationBuilder());

        replacementRan.ShouldBe(1);
        counter.ConfigureCalls.ShouldBe(0);
    }

    [Fact]
    public void InitializeSweepUsesTheSelectedReplacement()
    {
        var counter = new CallCounter();
        var entry = PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)), 0);
        var replacementRan = 0;
        entry.Initialization((host, _) => { replacementRan++; return GenericResult<IHost>.Success(host); });

        PlatformServices.Initialize(EmptyHost());

        replacementRan.ShouldBe(1);
        counter.InitializeCalls.ShouldBe(0);
    }

    [Fact]
    public void ReplacementReturnsSameEntryForFluentChaining()
    {
        var counter = new CallCounter();
        var entry = PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)), 0);

        entry.Registration((builder, _) => GenericResult<IHostApplicationBuilder>.Success(builder)).ShouldBeSameAs(entry);
    }

    private static IHost EmptyHost() => Host.CreateApplicationBuilder().Build();

    private static IHostApplicationBuilder EmptyHostApplicationBuilder() => Host.CreateApplicationBuilder();
}
