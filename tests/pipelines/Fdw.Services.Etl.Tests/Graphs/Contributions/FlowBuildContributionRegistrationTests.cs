using System;
using System.Linq;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Graphs.Building;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Graphs.Contributions;

/// <summary>
/// FDW-6240: the package of a component type names its contributions in its builder's Needs and registers nothing. The graph type registers every type a collected
/// builder needs, and the host's Initialize refuses a need that is not a contribution, a contribution registered twice and one that cannot be
/// constructed, each with its own code, and warns of a registered contribution no builder names.
/// </summary>
public sealed class FlowBuildContributionRegistrationTests
{
    private const string NotAContribution = "FLOW-60359";
    private const string RegisteredTwice = "FLOW-60361";
    private const string Unresolvable = "FLOW-60362";
    private const string Unused = "FLOW-60363";

    private static ServiceCollection Registered(params IFlowStepBuilder[] steps)
    {
        var services = new ServiceCollection();
        var registered = FlowBuildNeeds.Register(services, steps, [], NullLogger.Instance);
        registered.IsSuccess.ShouldBeTrue(registered.CurrentMessage);
        return services;
    }

    private static ServiceProvider Provider(ServiceCollection services) =>
        services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = false });

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void EveryTypeABuilderNeedsIsRegisteredOnceAsAContribution()
    {
        var services = Registered(new NeedingStepBuilder("Ask", typeof(AnswerContribution)), new NeedingStepBuilder("Tell", typeof(AnswerContribution)));

        var descriptor = services.Where(d => d.ServiceType == typeof(IFlowBuildContribution)).ShouldHaveSingleItem();
        descriptor.ImplementationType.ShouldBe(typeof(AnswerContribution));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ANeedThatIsNotAConcreteContributionIsRefusedNamingTheBuilderAndTheType()
    {
        var registered = FlowBuildNeeds.Register(new ServiceCollection(), [new NeedingStepBuilder("Ask", typeof(string))], [], NullLogger.Instance);

        registered.IsFailure.ShouldBeTrue();
        var message = registered.Messages.ShouldHaveSingleItem();
        message.Code.ShouldBe(NotAContribution);
        message.Message.ShouldContain("Ask");
        message.Message.ShouldContain(typeof(string).ToString());
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void TheRegisteredContributionsResolveForTheBuild()
    {
        using var provider = Provider(Registered(new NeedingStepBuilder("Ask", typeof(AnswerContribution))));

        using var scope = provider.CreateScope();
        var resolved = FlowBuildNeeds.Resolve(scope.ServiceProvider, NullLogger.Instance);

        resolved.IsSuccess.ShouldBeTrue(resolved.CurrentMessage);
        resolved.Value.ShouldNotBeNull().Find<AnswerContribution>().ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void EachRunsScopeGetsItsOwnContributionSoOnePersonsServicesNeverReachAnothersRun()
    {
        using var provider = Provider(Registered(new NeedingStepBuilder("Ask", typeof(AnswerContribution))));
        using var firstRun = provider.CreateScope();
        using var secondRun = provider.CreateScope();

        var first = FlowBuildNeeds.Resolve(firstRun.ServiceProvider, NullLogger.Instance).Value.ShouldNotBeNull().Find<AnswerContribution>().ShouldNotBeNull();
        var again = FlowBuildNeeds.Resolve(firstRun.ServiceProvider, NullLogger.Instance).Value.ShouldNotBeNull().Find<AnswerContribution>().ShouldNotBeNull();
        var second = FlowBuildNeeds.Resolve(secondRun.ServiceProvider, NullLogger.Instance).Value.ShouldNotBeNull().Find<AnswerContribution>().ShouldNotBeNull();

        again.ShouldBeSameAs(first);
        second.ShouldNotBeSameAs(first);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void AContributionRegisteredTwiceIsRefused()
    {
        var services = Registered(new NeedingStepBuilder("Ask", typeof(AnswerContribution)));
        services.AddScoped<IFlowBuildContribution, AnswerContribution>();
        using var provider = Provider(services);

        using var scope = provider.CreateScope();
        var resolved = FlowBuildNeeds.Resolve(scope.ServiceProvider, NullLogger.Instance);

        resolved.IsFailure.ShouldBeTrue();
        var message = resolved.Messages.ShouldHaveSingleItem();
        message.Code.ShouldBe(RegisteredTwice);
        message.Message.ShouldContain(typeof(AnswerContribution).ToString());
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void AContributionThatCannotBeConstructedIsRefusedWithItsOwnCode()
    {
        using var provider = Provider(Registered(new NeedingStepBuilder("Ask", typeof(UnresolvableContribution))));

        using var scope = provider.CreateScope();
        var resolved = FlowBuildNeeds.Resolve(scope.ServiceProvider, NullLogger.Instance);

        resolved.IsFailure.ShouldBeTrue();
        var message = resolved.Messages.ShouldHaveSingleItem();
        message.Code.ShouldBe(Unresolvable);
        message.Message.ShouldContain(nameof(IUnregisteredService));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AContributionNoBuilderNamesIsAWarningAndNotARefusal()
    {
        var registered = new FlowBuildContributions([new AnswerContribution()]);

        var unused = FlowBuildNeeds.Unused(registered, [new NeedingStepBuilder("Ask")], [], NullLogger.Instance);

        var warning = unused.ShouldHaveSingleItem();
        warning.Code.ShouldBe(Unused);
        warning.Message.ShouldContain(typeof(AnswerContribution).ToString());
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void FdwsOwnBuildersNeedNoContributionSoTheGraphTypeRegistersNone()
    {
        var services = new ServiceCollection();

        FlowBuildNeeds.Register(services, FlowStepBuilders.All(), FlowComponentBuilders.All(), NullLogger.Instance).IsSuccess.ShouldBeTrue();

        services.ShouldNotContain(d => d.ServiceType == typeof(IFlowBuildContribution));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void EveryBuilderFdwShipsIsCollected()
    {
        // The collections' generated NotFound sentinels (nested in FlowStepBuilders and FlowComponentBuilders) derive from the builder bases but are not
        // options, so they are never collected.
        Type[] shipped = [.. typeof(FlowBuildNeeds).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && (typeof(IFlowStepBuilder).IsAssignableFrom(type) || typeof(IFlowComponentBuilder).IsAssignableFrom(type)))
            .Where(type => type.DeclaringType != typeof(FlowStepBuilders) && type.DeclaringType != typeof(FlowComponentBuilders))];

        Type[] collected = [.. FlowStepBuilders.All().Select(builder => builder.GetType()).Concat(FlowComponentBuilders.All().Select(builder => builder.GetType()))];

        shipped.ShouldNotBeEmpty();
        collected.ShouldBeSubsetOf(shipped);
        shipped.ShouldAllBe(type => collected.Contains(type));
    }
}
