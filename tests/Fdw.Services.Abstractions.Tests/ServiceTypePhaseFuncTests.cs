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

        serviceType.Configure(builder).ShouldBeSameAs(builder);
        serviceType.Register(builder, null, "Store", "path", "container").ShouldBeSameAs(builder);

        builder.Services.Count.ShouldBe(before);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConfigurationReplacesTheConfigureBody()
    {
        var serviceType = new TestServiceType();
        var ran = 0;
        serviceType.Configuration(b => { ran++; return b; });

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
        serviceType.Registration((_, _, _, _, _) => replacement);

        serviceType.Register(NewBuilder(), null, "Store", "path", "container")
            .ShouldBeSameAs(replacement);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void InitializationReplacesTheInitializeBodyAndItsReturnValueIsUsed()
    {
        var serviceType = new TestServiceType();
        var replacement = Host.CreateApplicationBuilder().Build();
        serviceType.Initialization((_, _) => replacement);

        serviceType.Initialize(Host.CreateApplicationBuilder().Build(), null)
            .ShouldBeSameAs(replacement);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ReplacingAPhaseAffectsOnlyThatInstance()
    {
        var replaced = new TestServiceType();
        var untouched = new TestServiceType();
        var ran = 0;
        replaced.Configuration(b => { ran++; return b; });

        untouched.Configure(NewBuilder());

        ran.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void SettingAPhaseToNullThrows()
    {
        var serviceType = new TestServiceType();

        Should.Throw<ArgumentNullException>(() => serviceType.Configuration(null!));
        Should.Throw<ArgumentNullException>(() => serviceType.Registration(null!));
        Should.Throw<ArgumentNullException>(() => serviceType.Initialization(null!));
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
        serviceType.Configuration(b => { ran++; return b; });

        serviceType.Configure(NewBuilder());

        ran.ShouldBe(1);
        serviceType.Name.ShouldBe("Test");
    }

    // Why this pair exists: it reproduces the ApiClientTypeBase shape — an intermediate base that must
    // contribute wiring on EVERY option, under a concrete option that sets its own Registration body.
    private abstract class InvariantWiringBase : ServiceTypeBase<SimpleService, SimpleFactory, IServiceConfiguration>
    {
        protected InvariantWiringBase()
            : base("Invariant", "Services:Invariant", "Invariant", "Base with invariant wiring", "Testing")
        {
        }

        public int BaseRegisterCount { get; private set; }

        public override IHostApplicationBuilder Register(
            IHostApplicationBuilder builder,
            ILoggerFactory? loggerFactory,
            string dataStoreName,
            string pathName,
            string containerName)
        {
            BaseRegisterCount++;
            return base.Register(builder, loggerFactory, dataStoreName, pathName, containerName);
        }
    }

    private sealed class OptionThatSetsItsOwnRegistration : InvariantWiringBase
    {
        public int OwnRegisterCount { get; private set; }

        public OptionThatSetsItsOwnRegistration()
            => Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
            {
                OwnRegisterCount++;
                return builder;
            });
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ABaseInvokerOverrideStillRunsWhenTheOptionSetsItsOwnRegistrationBody()
    {
        // The gerund REPLACES the func, so a base that called Registration(...) in its constructor
        // would be silently clobbered by the derived constructor running after it. Overriding the
        // invoker is the sanctioned way to add wiring an option cannot drop.
        var option = new OptionThatSetsItsOwnRegistration();

        option.Register(NewBuilder(), loggerFactory: null, "Store", "path", "container");

        option.BaseRegisterCount.ShouldBe(1);
        option.OwnRegisterCount.ShouldBe(1);
    }
}
