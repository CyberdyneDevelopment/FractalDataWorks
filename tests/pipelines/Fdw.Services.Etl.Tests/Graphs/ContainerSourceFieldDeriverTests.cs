using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Describing;
using Microsoft.Extensions.Logging.Abstractions;
using static Fdw.Services.Etl.Tests.Graphs.GraphTestKit;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>FDW-5523: a container source's output holds the stored fields of its container when it states the container and the caller can read containers, and is not known otherwise.</summary>
public sealed class ContainerSourceFieldDeriverTests
{
    private static readonly IReadOnlyList<GraphField> Customers = [new GraphField("Id", "Int32"), new GraphField("Name", "String"), new GraphField("Note", null)];

    private static PipelineComponentConfiguration Container(string? dataStore, string? container, string? path = null) =>
        new() { Name = "file", ComponentType = "ContainerSource", DataStoreName = dataStore, ContainerName = container, PathName = path, BatchSize = 100 };

    private static IGenericResult<IReadOnlyList<GraphField>> Found() => GenericResult<IReadOnlyList<GraphField>>.Success(Customers);

    private static DescribeContext Reading(List<ContainerFieldsRequest> asked, IGenericResult<IReadOnlyList<GraphField>> answer) => new(
        (_, _) => Task.FromResult<IGenericResult<IReadOnlyList<GraphField>>>(GenericResult<IReadOnlyList<GraphField>>.Failure(new Fdw.Messages.GenericMessage("no data sets here"))),
        (request, _) =>
        {
            asked.Add(request);
            return Task.FromResult(answer);
        });

    private static Task<IGenericResult<IReadOnlyList<GraphNodeFields>>> Describe(PipelineComponentConfiguration component, DescribeContext context) =>
        GraphDescriber.Describe(Graph([component], []), context, NullLogger.Instance, TestContext.Current.CancellationToken);

    private static IReadOnlyList<GraphField>? Output(IGenericResult<IReadOnlyList<GraphNodeFields>> described) =>
        described.Value!.Single(node => node.Component == "file" && node.Node == "output" && !node.IsInput).Fields;

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AContainerSourceShowsTheStoredFieldsOfItsContainerInOrderWithATypeNoneWhereNobodyStatedOne()
    {
        var asked = new List<ContainerFieldsRequest>();

        var described = await Describe(Container("Crm", "Customers"), Reading(asked, Found()));

        described.IsSuccess.ShouldBeTrue(described.CurrentMessage);
        Output(described)!.Select(field => field.Name).ShouldBe(["Id", "Name", "Note"]);
        Output(described)!.Select(field => field.Type).ShouldBe(["Int32", "String", null]);
        Output(described)!.ShouldAllBe(field => field.Origin == null);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TheReaderIsAskedForWhatTheComponentStatesAndOnlyOnce()
    {
        var asked = new List<ContainerFieldsRequest>();

        await Describe(Container("Crm", "Customers", "sales"), Reading(asked, Found()));

        asked.ShouldBe([new ContainerFieldsRequest("file", "Crm", "sales", "Customers")]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ATextWithAParameterOrNoStoreOrNoContainerIsNotReadAndItsOutputIsNotKnown()
    {
        foreach (var component in new[] { Container("{{Store}}", "Customers"), Container("Crm", "{{Which}}"), Container("Crm", "Customers", "{{Path}}"), Container(null, "Customers"), Container("Crm", null), Container("", "") })
        {
            var asked = new List<ContainerFieldsRequest>();

            var described = await Describe(component, Reading(asked, Found()));

            described.IsSuccess.ShouldBeTrue(described.CurrentMessage);
            Output(described).ShouldBeNull();
            asked.ShouldBeEmpty();
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ACallerThatGaveNoReaderOfContainersLeavesTheOutputNotKnown()
    {
        var context = new DescribeContext((_, _) => Task.FromResult<IGenericResult<IReadOnlyList<GraphField>>>(GenericResult<IReadOnlyList<GraphField>>.Success([])));

        var described = await Describe(Container("Crm", "Customers"), context);

        described.IsSuccess.ShouldBeTrue(described.CurrentMessage);
        Output(described).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AContainerWithNoStoredFieldsIsNotKnownRatherThanKnownToHaveNone()
    {
        var described = await Describe(Container("Crm", "Customers"), Reading([], GenericResult<IReadOnlyList<GraphField>>.Success([])));

        described.IsSuccess.ShouldBeTrue(described.CurrentMessage);
        Output(described).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AContainerThatCannotBeReadFailsTheDescriptionWithTheReadersOwnMessage()
    {
        var failing = GenericResult<IReadOnlyList<GraphField>>.Failure(new Fdw.Messages.GenericMessage("container 'Customers' is not in data store 'Crm'"));

        var described = await Describe(Container("Crm", "Customers"), Reading([], failing));

        described.IsFailure.ShouldBeTrue();
        described.CurrentMessage.ShouldNotBeNull().ShouldContain("container 'Customers' is not in data store 'Crm'");
    }
}
