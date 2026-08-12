using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Fdw.Results;

namespace Fdw.ServiceTypes.Tests;

/// <summary>
/// Covers <see cref="ServiceTypeCollectionBase{TBase,TInterface}.RegisterMember"/>: which
/// registrations are idempotent, and which are rejected once the member set has closed.
/// </summary>
/// <remarks>
/// Each test closes over its OWN pair of type arguments. Static state on a generic base is
/// per closed generic, so a collection that freezes in one test cannot affect another — without
/// that, these tests would be order-dependent, which is exactly the property being asserted.
/// </remarks>
public class ServiceTypeCollectionRegistrationTests
{
    // ── one interface + collection per test, so no two tests share a registry ──
    public interface IAlpha : IServiceTypeRegistration;
    public interface IBeta : IServiceTypeRegistration;
    public interface IGamma : IServiceTypeRegistration;

    public abstract class OptionBase;

    // Options is protected static; a derived test collection is the sanctioned way to observe it.
    private sealed class AlphaCollection : ServiceTypeCollectionBase<OptionBase, IAlpha>
    { public static IServiceTypeRegistration[] Members => Options; }
    private sealed class BetaCollection : ServiceTypeCollectionBase<OptionBase, IBeta>
    { public static IServiceTypeRegistration[] Members => Options; }
    private sealed class GammaCollection : ServiceTypeCollectionBase<OptionBase, IGamma>
    { public static IServiceTypeRegistration[] Members => Options; }

    private sealed class Option : IServiceTypeRegistration
    {
        public Option(string name) => Name = name;

        public Guid Id { get; } = Guid.NewGuid();
        public string Name { get; }
        object ITypeOption.Id => Id;
        public string DisplayName => Name;
        public string Description => Name;
        public string Category => "NotCategorized";
        public string DataStore => "ConfigurationDb";
        public string PathName => "cfg";
        public string Container => Name;

        public IGenericResult<IHostApplicationBuilder> Configure(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null)
            => GenericResult<IHostApplicationBuilder>.Success(builder);

        public IGenericResult<IHostApplicationBuilder> Register(
            IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null)
            => GenericResult<IHostApplicationBuilder>.Success(builder);

        public IGenericResult<IHost> Initialize(IHost host, ILoggerFactory? loggerFactory = null)
            => GenericResult<IHost>.Success(host);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void RegisteringTheSameOptionTwiceBeforeTheSetClosesIsANoOp()
    {
        var option = new Option("Same");

        AlphaCollection.RegisterMember(option);
        AlphaCollection.RegisterMember(option);

        // Reading closes the set; the option must appear exactly once.
        AlphaCollection.Members.Length.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ReRegisteringAnAlreadyPresentOptionAfterTheSetClosesIsAlsoANoOp()
    {
        // Why this is the regression: an option is offered from two directions — its own module
        // initializer and the cross-assembly registration an entry point emits. When the second
        // arrival landed after a read had closed the set, the frozen check fired before the
        // duplicate check and threw, even though the collection already held that exact option.
        var option = new Option("Duplicate");
        BetaCollection.RegisterMember(option);

        BetaCollection.Initialize(new NullHost());   // closes the set
        BetaCollection.Members.Length.ShouldBe(1, "the option must be in the closed set");

        Should.NotThrow(() => BetaCollection.RegisterMember(option));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void RegisteringAGenuinelyNewOptionAfterTheSetClosesThrows()
    {
        // The set has been read, so a member added now would never appear in any lookup. Silently
        // accepting it is the failure the closed set exists to prevent.
        GammaCollection.RegisterMember(new Option("First"));
        GammaCollection.Initialize(new NullHost());

        Should.Throw<InvalidOperationException>(() => GammaCollection.RegisterMember(new Option("Late")));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void RegisteringNullThrows()
        => Should.Throw<ArgumentNullException>(() => AlphaCollection.RegisterMember(null!));

    // Why a stub rather than Host.CreateApplicationBuilder().Build(): these tests close the member set
    // and never reach into the container, so the only thing phase 3 needs here is *an* IHost. Building
    // a real one would put a Microsoft.Extensions.Hosting reference on this project to no purpose —
    // IHost itself comes from Hosting.Abstractions, which is already referenced.
    private sealed class NullHost : IHost
    {
        public IServiceProvider Services { get; } = new NullServiceProvider();

        public void Dispose() { }

        public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        private sealed class NullServiceProvider : IServiceProvider
        {
            public object? GetService(Type serviceType) => null;
        }
    }
}
