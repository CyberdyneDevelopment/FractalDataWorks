using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Fdw.Collections;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fdw.ServiceTypes.Tests;

/// <summary>
/// FDW-4588: a collection and its options declare the services they need registered, and the collection's Initialize checks them all once, each missing one with its own message.
/// </summary>
/// <remarks>
/// Each test closes over its OWN interface: the declarations, the funcs and the phase state are static on a generic base and so per closed generic.
/// </remarks>
public class ServiceRequirementsTests
{
    public interface IOneMissing : IServiceTypeRegistration;
    public interface ITwoMissing : IServiceTypeRegistration;
    public interface IReplacedBody : IServiceTypeRegistration;
    public interface IAppendedBody : IServiceTypeRegistration;
    public interface IOptionDeclares : IServiceTypeRegistration;
    public interface IOptionNeverAttached : IServiceTypeRegistration;
    public interface IForced : IServiceTypeRegistration;
    public interface ILifetimes : IServiceTypeRegistration;
    public interface IDeferred : IServiceTypeRegistration;
    public interface IDeclaredTwice : IServiceTypeRegistration;

    public abstract class OptionBase;

    public interface IAlpha;
    public interface IBeta;
    public interface IGamma;

    private sealed class Alpha : IAlpha;
    private sealed class Beta : IBeta;
    private sealed class Gamma : IGamma;

    private sealed class Coded : IGenericMessage
    {
        public Coded(string code) => Code = code;

        public string Message => "missing " + Code;
        public string? Code { get; }
        public string? Source => nameof(ServiceRequirementsTests);
    }

    private static IGenericMessage AlphaMissing(ILogger logger) => new Coded("ALPHA-1");
    private static IGenericMessage BetaMissing(ILogger logger) => new Coded("BETA-1");
    private static IGenericMessage GammaMissing(ILogger logger) => new Coded("GAMMA-1");

    private sealed class OneMissingCollection : ServiceTypeCollectionBase<OptionBase, IOneMissing>
    {
        static OneMissingCollection() => Requires<IAlpha>(AlphaMissing);
    }

    private sealed class TwoMissingCollection : ServiceTypeCollectionBase<OptionBase, ITwoMissing>
    {
        static TwoMissingCollection()
        {
            Requires<IAlpha>(AlphaMissing);
            Requires<IBeta>(BetaMissing);
        }
    }

    private sealed class ReplacedBodyCollection : ServiceTypeCollectionBase<OptionBase, IReplacedBody>
    {
        static ReplacedBodyCollection() => Requires<IAlpha>(AlphaMissing);
    }

    private sealed class AppendedBodyCollection : ServiceTypeCollectionBase<OptionBase, IAppendedBody>
    {
        static AppendedBodyCollection() => Requires<IAlpha>(AlphaMissing);
    }

    private sealed class OptionDeclaresCollection : ServiceTypeCollectionBase<OptionBase, IOptionDeclares>;
    private sealed class OptionNeverAttachedCollection : ServiceTypeCollectionBase<OptionBase, IOptionNeverAttached>;

    private sealed class ForcedCollection : ServiceTypeCollectionBase<OptionBase, IForced>
    {
        static ForcedCollection() => Requires<IAlpha>(AlphaMissing);
    }

    private sealed class LifetimesCollection : ServiceTypeCollectionBase<OptionBase, ILifetimes>
    {
        static LifetimesCollection()
        {
            Requires<IAlpha>(AlphaMissing);
            Requires<IBeta>(BetaMissing);
            Requires<IGamma>(GammaMissing);
        }
    }

    private sealed class DeferredCollection : ServiceTypeCollectionBase<OptionBase, IDeferred>
    {
        static DeferredCollection() => Requires<IAlpha>(AlphaMissing);
    }

    private sealed class DeclaredTwiceCollection : ServiceTypeCollectionBase<OptionBase, IDeclaredTwice>
    {
        static DeclaredTwiceCollection()
        {
            Requires<IAlpha>(AlphaMissing);
            Requires<IAlpha>(BetaMissing);
        }
    }

    private sealed class Option : IServiceTypeRegistration, IServiceRequirements
    {
        private readonly List<ServiceRequirement> _requirements = new();

        public Option(string name) => Name = name;

        public Guid Id { get; } = Guid.NewGuid();
        public string Name { get; }
        public string DisplayName => Name;
        public string Description => Name;
        public string Category => "NotCategorized";
        public string DataStore => "PlatformConfiguration";
        public string PathName => "cfg";
        public string Container => Name;
        public IReadOnlyList<ServiceRequirement> Requirements => _requirements;

        public Option Requires<T>(Func<ILogger, IGenericMessage> message)
        {
            _requirements.Add(new ServiceRequirement(typeof(T), message));
            return this;
        }

        public IGenericResult<IHostApplicationBuilder> Configure(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null, bool force = false, bool defer = false)
            => GenericResult<IHostApplicationBuilder>.Success(builder);

        public IGenericResult<IHostApplicationBuilder> Register(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null, bool force = false, bool defer = false)
            => GenericResult<IHostApplicationBuilder>.Success(builder);

        public IGenericResult<IHost> Initialize(IHost host, ILoggerFactory? loggerFactory = null, bool force = false, bool defer = false)
            => GenericResult<IHost>.Success(host);
    }

    // The declarations sit in the derived collection's static constructor, which the generated module initializer runs before anything else in a real host; nothing here touches
    // the derived type, so each test runs it first.
    private static void Prime(Type collection) => RuntimeHelpers.RunClassConstructor(collection.TypeHandle);

    private static IHost Host(Action<IServiceCollection>? register = null)
    {
        var services = new ServiceCollection();
        register?.Invoke(services);
        var provider = services.BuildServiceProvider();
        var host = new Mock<IHost>();
        host.Setup(h => h.Services).Returns(provider);
        return host.Object;
    }

    private static string[] Codes(IGenericResult<IHost> result) => result.Messages.Select(m => m.Code ?? string.Empty).ToArray();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void AMissingCollectionRequirementFailsWithItsOwnMessageAndTheBodyDoesNotRun()
    {
        Prime(typeof(OneMissingCollection));
        var ran = false;
        ServiceTypeCollectionBase<OptionBase, IOneMissing>.Initialization((host, loggerFactory) =>
        {
            ran = true;
            return GenericResult<IHost>.Success(host);
        });

        var result = ServiceTypeCollectionBase<OptionBase, IOneMissing>.Initialize(Host());

        result.IsFailure.ShouldBeTrue();
        Codes(result).ShouldBe(["ALPHA-1"]);
        ran.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void SeveralMissingRequirementsGiveSeveralMessagesInOneResult()
    {
        Prime(typeof(TwoMissingCollection));
        var result = ServiceTypeCollectionBase<OptionBase, ITwoMissing>.Initialize(Host());

        result.IsFailure.ShouldBeTrue();
        Codes(result).OrderBy(c => c, StringComparer.Ordinal).ToArray().ShouldBe(["ALPHA-1", "BETA-1"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void AReplacedInitializationBodyIsStillChecked()
    {
        Prime(typeof(ReplacedBodyCollection));
        var ran = false;
        ServiceTypeCollectionBase<OptionBase, IReplacedBody>.Initialization((host, loggerFactory) =>
        {
            ran = true;
            return GenericResult<IHost>.Success(host);
        });

        var result = ServiceTypeCollectionBase<OptionBase, IReplacedBody>.Initialize(Host());

        result.IsFailure.ShouldBeTrue();
        ran.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void AnAppendedInitializationBodyIsStillCheckedAndRunsWhenTheServiceIsThere()
    {
        Prime(typeof(AppendedBodyCollection));
        var ran = false;
        ServiceTypeCollectionBase<OptionBase, IAppendedBody>.AppendInitialization((host, loggerFactory) =>
        {
            ran = true;
            return GenericResult<IHost>.Success(host);
        });

        var refused = ServiceTypeCollectionBase<OptionBase, IAppendedBody>.Initialize(Host());
        refused.IsFailure.ShouldBeTrue();
        ran.ShouldBeFalse();

        var accepted = ServiceTypeCollectionBase<OptionBase, IAppendedBody>.Initialize(Host(s => s.AddSingleton<IAlpha, Alpha>()));
        accepted.IsSuccess.ShouldBeTrue();
        ran.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void AnOptionsRequirementIsCheckedWhenItIsAttached()
    {
        OptionDeclaresCollection.RegisterMember(new Option("Declaring").Requires<IBeta>(BetaMissing));

        var result = ServiceTypeCollectionBase<OptionBase, IOptionDeclares>.Initialize(Host());

        result.IsFailure.ShouldBeTrue();
        Codes(result).ShouldBe(["BETA-1"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ARequirementOfAnOptionThatWasNeverAttachedIsNeverAskedFor()
    {
        var detached = new Option("Detached").Requires<IBeta>(BetaMissing);
        detached.Requirements.Count.ShouldBe(1);

        var result = ServiceTypeCollectionBase<OptionBase, IOptionNeverAttached>.Initialize(Host());

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AForcedRunAfterTheServiceIsRegisteredSucceeds()
    {
        Prime(typeof(ForcedCollection));
        var first = ServiceTypeCollectionBase<OptionBase, IForced>.Initialize(Host());
        first.IsFailure.ShouldBeTrue();

        var second = ServiceTypeCollectionBase<OptionBase, IForced>.Initialize(Host(s => s.AddSingleton<IAlpha, Alpha>()), force: true);

        second.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void SingletonScopedAndTransientRegistrationsAllSatisfyARequirement()
    {
        Prime(typeof(LifetimesCollection));
        var result = ServiceTypeCollectionBase<OptionBase, ILifetimes>.Initialize(Host(s =>
        {
            s.AddSingleton<IAlpha, Alpha>();
            s.AddScoped<IBeta, Beta>();
            s.AddTransient<IGamma, Gamma>();
        }));

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ADeferredPhaseChecksNothingUntilItRuns()
    {
        Prime(typeof(DeferredCollection));
        var deferred = ServiceTypeCollectionBase<OptionBase, IDeferred>.Initialize(Host(), defer: true);
        deferred.IsSuccess.ShouldBeTrue();

        var run = ServiceTypeCollectionBase<OptionBase, IDeferred>.Initialize(Host());
        run.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AServiceDeclaredTwiceIsReportedOnce()
    {
        Prime(typeof(DeclaredTwiceCollection));
        var result = ServiceTypeCollectionBase<OptionBase, IDeclaredTwice>.Initialize(Host());

        result.IsFailure.ShouldBeTrue();
        Codes(result).ShouldBe(["ALPHA-1"]);
    }
}
