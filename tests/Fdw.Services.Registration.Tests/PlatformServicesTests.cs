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
            (builder, _, _, _) => { ConfigureCalls++; return GenericResult<IHostApplicationBuilder>.Success(builder); },
            (builder, _, _, _) => { RegisterCalls++; return GenericResult<IHostApplicationBuilder>.Success(builder); },
            (host, _, _, _) => { InitializeCalls++; return GenericResult<IHost>.Success(host); });
    }

    [Fact]
    public void AddReturnsNewEntry()
    {
        var counter = new CallCounter();
        var entry = PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)));

        entry.CategoryName.ShouldBe("Widget");
        entry.Initialized.ShouldBeFalse();
    }

    [Fact]
    public void AddWithSameCategoryAndSameCollectionTypeIsIdempotent()
    {
        var counter = new CallCounter();
        var first = PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)));
        var second = PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)));

        second.ShouldBeSameAs(first);
    }

    [Fact]
    public void AddWithSameCategoryButDifferentCollectionTypeThrows()
    {
        var counter = new CallCounter();
        PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)));

        Should.Throw<InvalidOperationException>(() =>
            PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(int))));
    }

    [Fact]
    public void AddAfterFreezeThrows()
    {
        var counter = new CallCounter();
        PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)));
        PlatformServices.Entries(); // triggers freeze

        Should.Throw<InvalidOperationException>(() =>
            PlatformServices.Add("Gadget", counter.Descriptor("Gadget", typeof(int))));
    }

    [Fact]
    public void EntriesKeepRegistrationOrderAndDoNotReorderThemselves()
    {
        // Why this is asserted rather than left implicit: there is no sort any more, so the ONLY thing
        // a caller can rely on is the order things registered in. A host that needs a different order
        // states it by running a domain early or deferring it — not by expecting the collect to know.
        var counter = new CallCounter();
        PlatformServices.Add("Second", counter.Descriptor("Second", typeof(string)));
        PlatformServices.Add("First", counter.Descriptor("First", typeof(int)));

        var entries = PlatformServices.Entries();
        entries[0].CategoryName.ShouldBe("Second");
        entries[1].CategoryName.ShouldBe("First");
    }

    [Fact]
    public void DeferExcludesDomainFromSweepsButKeepsEntryDotWalkDrivable()
    {
        var swept = new CallCounter();
        var deferred = new CallCounter();
        PlatformServices.Add("Swept", swept.Descriptor("Swept", typeof(string)));
        var deferredEntry = PlatformServices.Add("Declared", deferred.Descriptor("Declared", typeof(int)));

        // Claiming the phase without running it: nothing has executed yet.
        deferredEntry.Register(Host.CreateApplicationBuilder(), defer: true);
        deferredEntry.Initialize(Host.CreateApplicationBuilder().Build(), defer: true);
        deferred.RegisterCalls.ShouldBe(0);
        deferred.InitializeCalls.ShouldBe(0);

        var builder = Host.CreateApplicationBuilder();

        PlatformServices.Register(builder);
        PlatformServices.Initialize(builder.Build());

        // The swept domain runs in the sweep; the deferred domain is skipped by it.
        swept.RegisterCalls.ShouldBe(1);
        swept.InitializeCalls.ShouldBe(1);
        deferred.RegisterCalls.ShouldBe(0);
        deferred.InitializeCalls.ShouldBe(0);

        // A deferred phase RUNS on the next explicit call — this is what distinguishes it from a phase
        // that has already run, which the sweep skips identically but an explicit call no-ops.
        deferredEntry.Register(Host.CreateApplicationBuilder());
        deferred.RegisterCalls.ShouldBe(1);

        // And it is now Ran, so a further call does nothing.
        deferredEntry.Register(Host.CreateApplicationBuilder());
        deferred.RegisterCalls.ShouldBe(1);
    }

    [Fact]
    public void EntryConfigureIsIdempotent()
    {
        var counter = new CallCounter();
        var entry = PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)));

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
        var entry = PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)));

        entry.Configure(EmptyHostApplicationBuilder());
        PlatformServices.Configure(EmptyHostApplicationBuilder());

        counter.ConfigureCalls.ShouldBe(1);
    }

    [Fact]
    public void EntryInitializeIsIdempotent()
    {
        var counter = new CallCounter();
        var entry = PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)));

        entry.Initialize(EmptyHost());
        entry.Initialize(EmptyHost());

        counter.InitializeCalls.ShouldBe(1);
        entry.Initialized.ShouldBeTrue();
    }

    [Fact]
    public void EntryRegisterIsIdempotent()
    {
        var counter = new CallCounter();
        var entry = PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)));

        entry.Register(Host.CreateApplicationBuilder());
        entry.Register(Host.CreateApplicationBuilder());

        counter.RegisterCalls.ShouldBe(1);
        entry.Registered.ShouldBeTrue();
    }

    [Fact]
    public void RegisterSweepSkipsAlreadyManuallyRegisteredEntry()
    {
        var counter = new CallCounter();
        var entry = PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)));

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
        var entry = PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)));

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
        PlatformServices.Add("B", counterB.Descriptor("B", typeof(string)));
        PlatformServices.Add("A", counterA.Descriptor("A", typeof(int)));

        PlatformServices.Initialize(EmptyHost());

        counterA.InitializeCalls.ShouldBe(1);
        counterB.InitializeCalls.ShouldBe(1);
    }

    [Fact]
    public void ConfigureAndRegisterInvokeEveryEntry()
    {
        var counter = new CallCounter();
        PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)));

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
        var entry = PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)));
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
        var entry = PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)));
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
        var entry = PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)));
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
        var entry = PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)));

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
        var entry = PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)));
        entry.Register(Host.CreateApplicationBuilder());

        Should.Throw<InvalidOperationException>(() => entry.Registration((builder, _) => GenericResult<IHostApplicationBuilder>.Success(builder)));
    }

    [Fact]
    public void InitializationAfterInitializeHasRunThrows()
    {
        var counter = new CallCounter();
        var entry = PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)));
        entry.Initialize(EmptyHost());

        Should.Throw<InvalidOperationException>(() => entry.Initialization((host, _) => GenericResult<IHost>.Success(host)));
    }

    [Fact]
    public void ConfigurationAfterConfigureHasRunThrows()
    {
        var counter = new CallCounter();
        var entry = PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)));
        entry.Configure(EmptyHostApplicationBuilder());

        Should.Throw<InvalidOperationException>(() => entry.Configuration((b, _) => GenericResult<IHostApplicationBuilder>.Success(b)));
    }

    [Fact]
    public void RegisterSweepUsesTheSelectedReplacement()
    {
        var counter = new CallCounter();
        var entry = PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)));
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
        var entry = PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)));
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
        var entry = PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)));
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
        var entry = PlatformServices.Add("Widget", counter.Descriptor("Widget", typeof(string)));

        entry.Registration((builder, _) => GenericResult<IHostApplicationBuilder>.Success(builder)).ShouldBeSameAs(entry);
    }

    private static IHost EmptyHost() => Host.CreateApplicationBuilder().Build();

    private static IHostApplicationBuilder EmptyHostApplicationBuilder() => Host.CreateApplicationBuilder();
}
