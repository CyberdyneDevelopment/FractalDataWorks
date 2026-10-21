using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs.Building;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// FDW-1952: a container source may state a physical path and name to read in place of the declared container's. The two settings take a
/// run parameter like the declared path and container do, they are in the catalogue and the binding table, and a source that states one is
/// built only on the ETL data gateway: any other gateway fails the build naming the component, never ignoring the override.
/// </summary>
public sealed class PhysicalAddressTests
{
    private static PipelineComponentConfiguration Source(string? physicalPath, string? physicalName) => new()
    {
        Name = "file",
        ComponentType = "ContainerSource",
        DataStoreName = "Landing",
        PathName = "inbound",
        ContainerName = "Orders",
        BatchSize = 50,
        PhysicalPath = physicalPath,
        PhysicalName = physicalName,
    };

    private static FlowBuildContext On(IDataGateway gateway) => GraphTestKit.ContextOn(new GraphPipelineConfiguration { Name = "Ingest" }, gateway);

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ThePhysicalSettingsAreInTheCatalogueAndTheBindingTable()
    {
        var settings = ComponentTypes.ByName("ContainerSource").Settings;

        foreach (var name in new[] { "physicalPath", "physicalName" })
        {
            settings.Single(setting => setting.Name == name).TakesParameterText.ShouldBeTrue();
            ComponentSettingBindings.Find(name).ShouldNotBeNull();
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void TheBindingsReadAndWriteTheComponent()
    {
        var component = Source(null, null);

        ComponentSettingBindings.Find("physicalName")!.Set(component, "orders-2026-10-19.csv");
        ComponentSettingBindings.Find("physicalPath")!.Set(component, "inbound/ercot");

        component.PhysicalName.ShouldBe("orders-2026-10-19.csv");
        component.PhysicalPath.ShouldBe("inbound/ercot");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void TheStartOfARunLeavesThePhysicalSettingsForTheStepAndTheStepRendersThemFromTheVariablesThen()
    {
        var context = GraphTestKit.ContextWith(new GraphPipelineConfiguration { Name = "Ingest" }, new Dictionary<string, object?> { ["File"] = "orders.csv" });

        var atStart = ComponentParameters.Render(Source(null, "{{File}}"), context);

        atStart.IsSuccess.ShouldBeTrue(atStart.CurrentMessage);
        atStart.Value!.PhysicalName.ShouldBe("{{File}}");

        var atTheStep = ComponentParameters.RenderLate(atStart.Value, new Dictionary<string, object?> { ["File"] = "orders.csv" }, NullLogger.Instance);

        atTheStep.IsSuccess.ShouldBeTrue(atTheStep.CurrentMessage);
        atTheStep.Value!.PhysicalName.ShouldBe("orders.csv");
        atTheStep.Value.ContainerName.ShouldBe("Orders");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AValueTheStepHasNotGotFailsNamingTheComponentTheSettingAndTheName()
    {
        var rendered = ComponentParameters.RenderLate(Source(null, "{{FileName}}"), new Dictionary<string, object?> { ["FileName"] = null }, NullLogger.Instance);

        rendered.IsFailure.ShouldBeTrue();
        rendered.CurrentMessage.ShouldNotBeNull().ShouldContain("FileName");
        rendered.CurrentMessage.ShouldContain("file");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ASourceThatNamesAPhysicalValueIsBuiltWhenItsStageStartsAndAnyOtherIsBuiltAtOnce()
    {
        // Why a gateway that cannot read a physical name: a source built now is refused on it, so one that is built later is not refused yet.
        var context = On(new Mock<IDataGateway>().Object);

        var late = GraphFlowBuilder.Build(Flow(Source(null, "{{FileName}}")), context);
        var plain = GraphFlowBuilder.Build(Flow(Source(null, "orders.csv")), context);

        late.IsSuccess.ShouldBeTrue(late.CurrentMessage);
        plain.IsFailure.ShouldBeTrue();
        plain.CurrentMessage.ShouldNotBeNull().ShouldContain("file");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task ASourceReadsThePhysicalNameAnEarlierStepCalculated()
    {
        string? seenName = null;
        var gateway = new Mock<IEtlDataGateway>();
        gateway.Setup(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<IDataCommand, DataStoreTarget, string?, string?, CancellationToken>((_, _, _, name, _) => seenName = name)
            .Returns(Task.FromResult<IGenericResult<IEnumerable<Dictionary<string, object?>>>>(
                GenericResult<IEnumerable<Dictionary<string, object?>>>.Success([])));
        var deferred = GraphFlowBuilder.Build(Flow(Source(null, "{{FileName}}")), On(gateway.Object));
        var variables = new FlowVariables();
        variables.Declare("FileName", null);
        variables.TrySet("FileName", "feed-20261019.csv");

        var ran = await deferred.Value!.Run(GraphTestKit.StepContext(variables, new FlowRunTally(new FlowExecutor())), TestContext.Current.CancellationToken);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        seenName.ShouldBe("feed-20261019.csv");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task AComponentThatFailsToBuildFailsWhenItsStageStartsNamingTheVariable()
    {
        var deferred = GraphFlowBuilder.Build(Flow(Source(null, "{{FileName}}")), On(new Mock<IEtlDataGateway>().Object));
        var variables = new FlowVariables();
        variables.Declare("FileName", null);

        var ran = await deferred.Value!.Run(GraphTestKit.StepContext(variables, new FlowRunTally(new FlowExecutor())), TestContext.Current.CancellationToken);

        ran.IsFailure.ShouldBeTrue();
        ran.CurrentMessage.ShouldNotBeNull().ShouldContain("FileName");
    }

    private static GraphPipelineConfiguration Flow(PipelineComponentConfiguration source) =>
        GraphTestKit.Steps(Part(source, 1));

    private static GraphPart Part(PipelineComponentConfiguration source, int ordinal) =>
        GraphTestKit.DataFlow("flow", [source, GraphTestKit.Mapped("map", ("Key", "Id"))], [GraphTestKit.Link("file", "map")], [GraphTestKit.Dropped("map")], ordinal);

    private static PipelineComponentConfiguration Calculates(string variable) => new()
    {
        Name = "calc", ComponentType = "SetVariable", VariableName = variable, ConstantText = "{{Stamp}}", TransformationName = "Concatenate", Stage = 5,
        Parameters = { new PipelineComponentParameterConfiguration { ParameterName = "prefix", ValueText = "feed-" } },
    };

    private static GraphPipelineConfiguration Calculating(PipelineComponentConfiguration source, int flowOrdinal)
    {
        var graph = GraphTestKit.Steps(Calculates("FileName"), Part(source, flowOrdinal));
        graph.Parameters.Add(new PipelineParameterConfiguration { Name = "Stamp", ParameterType = "String" });
        graph.Variables.Add(new PipelineVariableConfiguration { Name = "FileName", DataType = "String", ValueText = "" });
        return graph;
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void TheSaveCheckAcceptsAVariableALowerStageSetsInAPhysicalSettingAndRefusesItInAnyOther()
    {
        var accepted = GraphShapeValidator.Problems(Calculating(Source(null, "{{FileName}}"), flowOrdinal: 6), NullLogger.Instance);
        accepted.ShouldNotContain(problem => problem.Reason.Contains("FileName", StringComparison.Ordinal));

        var inTheContainer = Calculating(Source(null, null), flowOrdinal: 6);
        inTheContainer.Components.Single(component => component.Name == "file").ContainerName = "{{FileName}}";
        var refused = GraphShapeValidator.Problems(inTheContainer, NullLogger.Instance);
        refused.ShouldContain(problem => problem.Reason.Contains("FileName", StringComparison.Ordinal) && problem.Reason.Contains("read when the run starts", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void TheSaveCheckRefusesAPhysicalSettingThatNamesAVariableNoLowerStageSets()
    {
        var problems = GraphShapeValidator.Problems(Calculating(Source(null, "{{FileName}}"), flowOrdinal: 4), NullLogger.Instance);

        problems.ShouldContain(problem => problem.Reason.Contains("no component of a lower stage", StringComparison.Ordinal) && problem.Reason.Contains("FileName", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ASourceThatStatesAPhysicalNameIsRefusedOnAGatewayThatCannotReadItAndTheFailureNamesTheComponent()
    {
        var built = new ContainerSourceFlowComponentBuilder().Create(Source(null, "orders.csv"), On(new Mock<IDataGateway>().Object));

        built.IsSuccess.ShouldBeFalse();
        built.CurrentMessage.ShouldNotBeNull().ShouldContain("file");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ASourceThatStatesAPhysicalPathIsBuiltOnTheEtlDataGateway()
    {
        var built = new ContainerSourceFlowComponentBuilder().Create(Source("inbound/ercot", null), On(new Mock<IEtlDataGateway>().Object));

        built.IsSuccess.ShouldBeTrue(built.CurrentMessage);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ASourceThatStatesNoPhysicalAddressIsBuiltOnAnyGateway()
    {
        var built = new ContainerSourceFlowComponentBuilder().Create(Source(null, null), On(new Mock<IDataGateway>().Object));

        built.IsSuccess.ShouldBeTrue(built.CurrentMessage);
    }
}
