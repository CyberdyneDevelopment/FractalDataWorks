using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Building;
using Fdw.Services.Etl.Pipelines.Loads;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// FDW-6132: a component kind from another package declares the build contributions its builder needs. A contribution the host registered reaches the
/// builder through the build context; one it did not refuses the build with ComponentServiceMissing, and the host's Initialize names every gap with
/// ComponentServicesUnregistered. FDW's own kinds declare none.
/// </summary>
public sealed class FlowBuildContributionTests
{
    private const string ServiceMissing = "FLOW-60355";
    private const string ServicesUnregistered = "FLOW-60356";

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AStepKindThatNeedsAContributionBuildsWhenTheHostRegisteredIt()
    {
        var contribution = new ModelContribution();

        var built = new ModelStepBuilder().Build(Component("Ask", "ModelStep"), [], Context(contribution));

        built.IsSuccess.ShouldBeTrue(built.CurrentMessage);
        built.Value.ShouldNotBeNull().Name.ShouldBe("Ask");
        contribution.Used.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AStepKindThatNeedsAContributionTheHostDidNotRegisterIsRefusedNamingTheComponentTheKindAndTheContribution()
    {
        var built = new ModelStepBuilder().Build(Component("Ask", "ModelStep"), [], Context());

        built.IsFailure.ShouldBeTrue();
        var message = built.Messages.ShouldHaveSingleItem();
        message.Code.ShouldBe(ServiceMissing);
        message.Message.ShouldContain("Ask");
        message.Message.ShouldContain("ModelStep");
        message.Message.ShouldContain(typeof(ModelContribution).ToString());
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AStepKindIsRefusedEvenInATestRunThatWouldSkipItsOutsideEffect()
    {
        var built = new ModelStepBuilder().Build(Component("Ask", "ModelStep"), [], Context(test: new GraphTestRun(10, true)));

        built.Messages.ShouldHaveSingleItem().Code.ShouldBe(ServiceMissing);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AComponentKindThatNeedsContributionsBuildsWhenTheHostRegisteredEveryOne()
    {
        var built = new ToolComponentBuilder().Build(Component("Tools", "ToolSink"), Context(new ToolContribution(), new ModelContribution()));

        built.IsSuccess.ShouldBeTrue(built.CurrentMessage);
        built.Value.ShouldNotBeNull().Name.ShouldBe("Tools");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AComponentKindGivenOnlyOneOfItsTwoContributionsIsRefusedNamingOnlyTheMissingOne()
    {
        var built = new ToolComponentBuilder().Build(Component("Tools", "ToolSink"), Context(new ToolContribution()));

        var message = built.Messages.ShouldHaveSingleItem();
        message.Code.ShouldBe(ServiceMissing);
        message.Message.ShouldContain(typeof(ModelContribution).ToString());
        message.Message.ShouldNotContain(typeof(ToolContribution).ToString());
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AComponentKindNeedingTwoContributionsIsRefusedWithOneMessageForEachMissingOne()
    {
        var built = new ToolComponentBuilder().Build(Component("Tools", "ToolSink"), Context());

        built.IsFailure.ShouldBeTrue();
        built.Messages.Select(message => message.Code).ShouldBe([ServiceMissing, ServiceMissing]);
        built.Messages[0].Message.ShouldContain(typeof(ToolContribution).ToString());
        built.Messages[1].Message.ShouldContain(typeof(ModelContribution).ToString());
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheHostStartCheckNamesEveryKindAndContributionThatIsNotRegistered()
    {
        var gaps = FlowBuildNeeds.Gaps([new ModelStepBuilder()], [new ToolComponentBuilder()], new FlowBuildContributions([new ToolContribution()]), NullLogger.Instance);

        gaps.Select(gap => gap.Code).ShouldBe([ServicesUnregistered, ServicesUnregistered]);
        gaps[0].Message.ShouldContain("ModelStep");
        gaps[0].Message.ShouldContain(typeof(ModelContribution).ToString());
        gaps[1].Message.ShouldContain("ToolSink");
        gaps[1].Message.ShouldContain(typeof(ModelContribution).ToString());
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheHostStartCheckFindsNoGapWhenEveryNeededContributionIsRegistered()
    {
        FlowBuildNeeds.Gaps(
            [new ModelStepBuilder()], [new ToolComponentBuilder()], new FlowBuildContributions([new ModelContribution(), new ToolContribution()]), NullLogger.Instance)
            .ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void FdwsOwnKindsDeclareNoContributionsSoAHostWithNoneHasNoGap()
    {
        FlowStepBuilders.All().Where(builder => builder.Needs.Count > 0).Select(builder => builder.Name).ShouldBeEmpty();
        FlowComponentBuilders.All().Where(builder => builder.Needs.Count > 0).Select(builder => builder.Name).ShouldBeEmpty();
        FlowBuildNeeds.Gaps(FlowStepBuilders.All(), FlowComponentBuilders.All(), FlowBuildContributions.None, NullLogger.Instance).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AContextMadeForAStepOrAStageKeepsTheRunsContributions()
    {
        var context = Context(new ModelContribution());

        context.ForLinks([]).Contribution<ModelContribution>(Component("Ask", "ModelStep")).IsSuccess.ShouldBeTrue();
        context.WithValues(new Dictionary<string, object?>()).Contribution<ModelContribution>(Component("Ask", "ModelStep")).IsSuccess.ShouldBeTrue();
    }

    private static PipelineComponentConfiguration Component(string name, string kind) => new() { Name = name, ComponentType = kind };

    private static FlowBuildContext Context(params IFlowBuildContribution[] contributions) => Context(null, contributions);

    private static FlowBuildContext Context(GraphTestRun? test, params IFlowBuildContribution[] contributions) =>
        new(
            "Assistant",
            Guid.NewGuid(),
            Guid.NewGuid(),
            new Mock<IDataGateway>().Object,
            new PipelineLoadExecutor(new LoadTargetResolver(null, null), null, null, NullLogger.Instance),
            null,
            DateTimeOffset.UtcNow,
            new Dictionary<string, object?>(),
            [],
            NullLogger.Instance,
            TimeSpan.Zero,
            0,
            new FlowBuildContributions(contributions),
            test: test)
        {
            // Why the claims: a step that acts outside the run is claimed before it acts (FDW-6139), and the model step of these tests acts outside the run.
            StepClaims = new Fdw.Services.Etl.Tests.Flow.InMemoryStepClaims(),
        };

    /// <summary>What a model kind's package would register: the services its step uses, injected by constructor.</summary>
    private sealed class ModelContribution : IFlowBuildContribution
    {
        public bool Used { get; set; }
    }

    /// <summary>What a tool kind's package would register.</summary>
    private sealed class ToolContribution : IFlowBuildContribution;

    /// <summary>A step kind from another package: it needs the model contribution and does something outside the run.</summary>
    private sealed class ModelStepBuilder : FlowStepBuilderBase
    {
        public ModelStepBuilder() : base("ModelStep")
        {
        }

        public override bool HasOutsideEffect => true;

        public override IReadOnlyList<Type> Needs => [typeof(ModelContribution)];

        public override IGenericResult<IFlowStep> Create(PipelineComponentConfiguration component, IReadOnlyList<IFlowStep> body, FlowBuildContext context)
        {
            var model = context.Contribution<ModelContribution>(component);
            if (model.IsFailure || model.Value is null)
            {
                return model.ToNewResult<IFlowStep>();
            }

            model.Value.Used = true;
            return GenericResult<IFlowStep>.Success(new FlowSkippedStep(component.Name, component.ComponentType, context.Logger));
        }
    }

    /// <summary>A component kind from another package that needs two contributions.</summary>
    private sealed class ToolComponentBuilder : FlowComponentBuilderBase
    {
        public ToolComponentBuilder() : base("ToolSink")
        {
        }

        public override IReadOnlyList<Type> Needs => [typeof(ToolContribution), typeof(ModelContribution)];

        public override IGenericResult<IFlowComponent> Create(PipelineComponentConfiguration component, FlowBuildContext context) =>
            GenericResult<IFlowComponent>.Success(new FlowDestination(component.Name, (_, _) => Task.FromResult(GenericResult.Success())));
    }
}
