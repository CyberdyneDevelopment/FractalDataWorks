using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Fdw.Services.Etl.Projects.Abstractions.TypeCollections;
using Fdw.Services.Etl.Projects.Execution;
using Fdw.Services.Pipelines.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Projects.Tests.Execution;

/// <summary>
/// The organization an orchestration run's own status broadcasts are addressed to (FDW-5986): the organization of the pipelines the node runs, and none when they state
/// none or more than one, never one that is guessed.
/// </summary>
public sealed class OrchestrationOwningOrgTests
{
    private static readonly Guid OrgA = Guid.Parse("aaaaaaaa-0000-0000-0000-00000000000a");
    private static readonly Guid OrgB = Guid.Parse("bbbbbbbb-0000-0000-0000-00000000000b");
    private static readonly Guid P1 = Guid.Parse("11111111-0000-0000-0000-000000000001");
    private static readonly Guid P2 = Guid.Parse("22222222-0000-0000-0000-000000000002");
    private static readonly Guid P3 = Guid.Parse("33333333-0000-0000-0000-000000000003");
    private static readonly Guid Run = Guid.Parse("99999999-0000-0000-0000-000000000009");

    private static OrchestrationNodeImplementationConfiguration Node(string type, IList<OrchestrationNodeImplementationConfiguration>? children = null, params Guid[] pipelines) => new()
    {
        NodeTypeId = OrchestrationNodeTypes.ByName(type).Id,
        Name = type,
        Children = children ?? [],
        PipelineMemberships = [.. pipelines.Select((id, ordinal) => new OrchestrationNodePipelineMembershipConfiguration { PipelineId = id, Ordinal = ordinal })]
    };

    private static Mock<IPipelineConfigurationProvider> Provider(params (Guid Id, Guid? Org)[] pipelines)
    {
        var provider = new Mock<IPipelineConfigurationProvider>();
        foreach (var (id, org) in pipelines)
        {
            var configuration = new Mock<IPipelineImplementationConfiguration>();
            configuration.SetupGet(c => c.OrgId).Returns(org);
            provider
                .Setup(p => p.Get(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<IPipelineImplementationConfiguration>.Success(configuration.Object));
        }

        return provider;
    }

    private static Task<Guid?> Of(IOrchestrationNodeImplementationConfiguration node, Mock<IPipelineConfigurationProvider> provider) =>
        OrchestrationOwningOrg.Of(node, provider.Object, NullLogger.Instance, Run, TestContext.Current.CancellationToken);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ANodeThatRunsPipelinesOfOneOrganizationIsAddressedToThatOrganization()
    {
        var node = Node("Step", null, P1);

        var org = await Of(node, Provider((P1, OrgA)));

        org.ShouldBe(OrgA);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task SeveralPipelinesOfTheSameOrganizationGiveItAndEachPipelineIsReadOnce()
    {
        var node = Node("Step", null, P1, P2, P1);
        var provider = Provider((P1, OrgA), (P2, OrgA));

        var org = await Of(node, provider);

        org.ShouldBe(OrgA);
        provider.Verify(p => p.Get(P1, It.IsAny<CancellationToken>()), Times.Once);
        provider.Verify(p => p.Get(P2, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ThePipelinesOfChildNodesAtAnyDepthAreWalked()
    {
        var step = Node("Step", null, P2);
        var stage = Node("Stage", [step]);
        var project = Node("Project", [stage]);

        var org = await Of(project, Provider((P2, OrgB)));

        org.ShouldBe(OrgB);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task APipelineThatStatesNoOrganizationDoesNotHideTheOneAnotherStates()
    {
        var node = Node("Step", null, P1, P2);

        var org = await Of(node, Provider((P1, null), (P2, OrgA)));

        org.ShouldBe(OrgA);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ANodeThatRunsNoPipelineIsAddressedToNoOrganization()
    {
        var org = await Of(Node("Project", [Node("Stage")]), Provider());

        org.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task PipelinesThatStateNoOrganizationGiveNone()
    {
        var org = await Of(Node("Step", null, P1, P2), Provider((P1, null), (P2, null)));

        org.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task PipelinesOfMoreThanOneOrganizationGiveNoneAndNeverOneOfThem()
    {
        var org = await Of(Node("Step", null, P1, P2), Provider((P1, OrgA), (P2, OrgB)));

        org.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task APipelineThatCannotBeReadGivesNoneAndNeverTheOrganizationOfTheOthers()
    {
        var provider = Provider((P1, OrgA));
        provider
            .Setup(p => p.Get(P3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IPipelineImplementationConfiguration>.Failure(Mock.Of<IGenericMessage>()));

        var org = await Of(Node("Step", null, P1, P3), provider);

        org.ShouldBeNull();
    }
}
