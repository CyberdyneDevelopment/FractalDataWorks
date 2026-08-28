using System.Diagnostics.CodeAnalysis;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.ServiceTypes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Abstractions.Tests;

/// <summary>
/// Tests the phase mechanism on <see cref="ServiceTypeBase{TService,TFactory,TConfiguration}"/>: each
/// phase is a func with a default, a gerund that replaces it, and a method that invokes whatever the
/// func currently holds.
/// </summary>
/// <remarks>
/// Replaces ServiceTypeBaseTwoParameterTests and ServiceTypeBaseProviderTests. Those covered the
/// 2-generic base, the 4-generic TProvider tier's typed RegisterFactory dispatch, and the
/// Invoke*/nullable-override interception — all removed. The override BEHAVIOUR they asserted is still
/// worth covering, so it is re-asserted here against the func API that replaced it.
/// </remarks>
public class ServiceTypePhaseFuncTests
{
    [ExcludeFromCodeCoverage]
    private sealed class SimpleService : IGenericService
    {
        public string Id => "test";
        public string ServiceType => "Test";
        public bool IsAvailable => true;

        public Task<IGenericResult<T>> Execute<T>(IGenericCommand command, CancellationToken cancellationToken)
            => Task.FromResult(GenericResult<T>.Success(default!));

        public Task<IGenericResult> Execute(IGenericCommand command, CancellationToken cancellationToken)
            => Task.FromResult(GenericResult.Success());
    }

    [ExcludeFromCodeCoverage]
    private sealed class SimpleFactory : IServiceFactory<SimpleService, IServiceConfiguration>
    {
        public IGenericResult<SimpleService> Create(IServiceConfiguration configuration)
            => GenericResult<SimpleService>.Success(new SimpleService());

        public IGenericResult<SimpleService> Create(IGenericConfiguration configuration)
            => GenericResult<SimpleService>.Success(new SimpleService());

        IGenericResult<IGenericService> IServiceFactory.Create(IGenericConfiguration configuration)
            => GenericResult<IGenericService>.Success(new SimpleService());

        IGenericResult<T> IServiceFactory.Create<T>(IGenericConfiguration configuration)
            => GenericResult<T>.Success(default!);
    }

    private sealed class TestServiceType : ServiceTypeBase<SimpleService, SimpleFactory, IServiceConfiguration>
    {
        public TestServiceType()
            : base("Test", "Services:Test", "Test Type", "A test service type", "Testing")
        {
        }
    }

    private static IHostApplicationBuilder NewBuilder() => Host.CreateApplicationBuilder();

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void PhaseDefaultsReturnTheirInputAndDoNothing()
    {
        var serviceType = new TestServiceType();
        var builder = NewBuilder();
        var before = builder.Services.Count;

        var configured = serviceType.Configure(builder);
        configured.IsSuccess.ShouldBeTrue();
        configured.Value.ShouldBeSameAs(builder);

        var registered = serviceType.Register(builder, null);
        registered.IsSuccess.ShouldBeTrue();
        registered.Value.ShouldBeSameAs(builder);

        builder.Services.Count.ShouldBe(before);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConfigurationReplacesTheConfigureBody()
    {
        var serviceType = new TestServiceType();
        var ran = 0;
        serviceType.Configuration(b => { ran++; return GenericResult<IHostApplicationBuilder>.Success(b); });

        serviceType.Configure(NewBuilder());

        ran.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void RegistrationReplacesTheRegisterBodyAndItsReturnValueIsUsed()
    {
        var serviceType = new TestServiceType();
        var replacement = NewBuilder();
        serviceType.Registration((_, _) => GenericResult<IHostApplicationBuilder>.Success(replacement));

        serviceType.Register(NewBuilder(), null)
            .Value.ShouldBeSameAs(replacement);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void InitializationReplacesTheInitializeBodyAndItsReturnValueIsUsed()
    {
        var serviceType = new TestServiceType();
        var replacement = Host.CreateApplicationBuilder().Build();
        serviceType.Initialization((_, _) => GenericResult<IHost>.Success(replacement));

        serviceType.Initialize(Host.CreateApplicationBuilder().Build(), null)
            .Value.ShouldBeSameAs(replacement);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ReplacingAPhaseAffectsOnlyThatInstance()
    {
        var replaced = new TestServiceType();
        var untouched = new TestServiceType();
        var ran = 0;
        replaced.Configuration(b => { ran++; return GenericResult<IHostApplicationBuilder>.Success(b); });

        untouched.Configure(NewBuilder());

        ran.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void SettingAPhaseToNullIsRefusedWithoutThrowing()
    {
        var serviceType = new TestServiceType();

        // A null body is reported through MessageLogging and the existing body is left alone, rather
        // than throwing — a phase setter is called from a constructor, where an exception takes the
        // whole option out instead of naming the one body that was wrong.
        Should.NotThrow(() => serviceType.Configuration(null!));
        Should.NotThrow(() => serviceType.Registration(null!));
        Should.NotThrow(() => serviceType.Initialization(null!));

        serviceType.Configure(NewBuilder()).IsSuccess.ShouldBeTrue();
        serviceType.Register(NewBuilder(), loggerFactory: null).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void PublishesItsServiceFactoryAndConfigurationTypes()
    {
        var serviceType = new TestServiceType();

        serviceType.ServiceType.ShouldBe(typeof(SimpleService));
        serviceType.FactoryType.ShouldBe(typeof(SimpleFactory));
        serviceType.ConfigurationType.ShouldBe(typeof(IServiceConfiguration));
        serviceType.SectionName.ShouldBe(serviceType.ConfigurationKey);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void IsReachableThroughTheNonGenericIServiceTypeInterface()
    {
        IServiceType serviceType = new TestServiceType();
        var ran = 0;
        serviceType.Configuration(b => { ran++; return GenericResult<IHostApplicationBuilder>.Success(b); });

        serviceType.Configure(NewBuilder());

        ran.ShouldBe(1);
        serviceType.Name.ShouldBe("Test");
    }

    private abstract class InvariantWiringBase : ServiceTypeBase<SimpleService, SimpleFactory, IServiceConfiguration>
    {
        protected InvariantWiringBase()
            : base("Invariant", "Services:Invariant", "Invariant", "Base with invariant wiring", "Testing")
        {
            // Prepend, not an override: the base contributes here in its own constructor, and the
            // derived constructor that runs afterwards chains onto it rather than assigning over it.
            PrependRegistration((builder, loggerFactory) =>
            {
                BaseRegisterCount++;
                return GenericResult<IHostApplicationBuilder>.Success(builder);
            });
        }

        public int BaseRegisterCount { get; private set; }
    }

    private sealed class OptionThatSetsItsOwnRegistration : InvariantWiringBase
    {
        public int OwnRegisterCount { get; private set; }

        public OptionThatSetsItsOwnRegistration()
            => Registration((builder, loggerFactory) =>
            {
                OwnRegisterCount++;
                return GenericResult<IHostApplicationBuilder>.Success(builder);
            });
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void SettingARegistrationBodyReplacesWhateverWasChained()
    {
        // The three setters say what they do: Registration replaces, PrependRegistration runs before
        // what is already there, AppendRegistration runs after. An option whose constructor calls
        // Registration is choosing to replace, so the base's prepended body goes with it.
        var option = new OptionThatSetsItsOwnRegistration();

        option.Register(NewBuilder(), loggerFactory: null);

        option.BaseRegisterCount.ShouldBe(0);
        option.OwnRegisterCount.ShouldBe(1);
    }
}
