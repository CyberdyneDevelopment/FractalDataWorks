using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Endpoints;
using Shouldly;

namespace Fdw.Services.Pipelines.Tests;

/// <summary>
/// What a saved pipeline reaches (FDW-1161): triggering it, scheduling it and judging the roles for it all read
/// <see cref="PipelineReach.Of"/> from the stored detail, so a run is judged on the pipeline as it is stored, stages and load
/// steps included.
/// </summary>
public sealed class PipelineReachTests
{
    private static PipelineDetailResponse SavedOverADataSet() => new()
    {
        Name = "Nightly",
        PipelineType = "BatchCopy",
        SourceDataSet = "Orders",
        DestinationDataSet = "OrdersCopy"
    };

    private static PipelineDetailResponse SavedOverAContainer() => new()
    {
        Name = "Nightly",
        PipelineType = "BatchCopy",
        // The engine record writes an unused data set side as an empty string, and the detail carries it as read.
        SourceDataSet = string.Empty,
        SourceDataStoreName = "Feeds",
        SourceDataStorePath = "orders/v1",
        SourceContainerName = "orders",
        DestinationDataSet = "OrdersCopy"
    };

    private static PipelineDetailResponse SavedIntoAContainer() => new()
    {
        Name = "Nightly",
        PipelineType = "BatchCopy",
        SourceDataSet = "Orders",
        DestinationDataSet = string.Empty,
        DestinationDataStoreName = "Warehouse",
        DestinationDataStorePath = "orders/v1",
        DestinationContainerName = "orders"
    };

    private static PipelineLoadStep Step(string destination, string? source = null, PipelineLoadDestinationContainer? container = null) => new()
    {
        Name = "Step",
        Kind = "BulkInsert",
        DestinationDataSet = destination,
        Options = new PipelineLoadStepOptions { SourceDataSet = source, DestinationContainer = container }
    };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ASavedPipelineReachesItsOwnSourceAndDestination()
    {
        var reach = PipelineReach.Of(SavedOverADataSet());

        reach.PipelineName.ShouldBe("Nightly");
        reach.SourceDataSet.ShouldBe("Orders");
        reach.DestinationDataSet.ShouldBe("OrdersCopy");
        reach.Steps.DataSets.ShouldBeEmpty();
        reach.Steps.Containers.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ASavedPipelineIntoADataStoreContainerReachesItsThreeDestinationParts()
    {
        var reach = PipelineReach.Of(SavedIntoAContainer());

        reach.DestinationDataStoreName.ShouldBe("Warehouse");
        reach.DestinationDataStorePath.ShouldBe("orders/v1");
        reach.DestinationContainerName.ShouldBe("orders");
        reach.SourceDataSet.ShouldBe("Orders");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ASavedPipelineOverADataStoreContainerReachesItsThreeParts()
    {
        var reach = PipelineReach.Of(SavedOverAContainer());

        reach.SourceDataStoreName.ShouldBe("Feeds");
        reach.SourceDataStorePath.ShouldBe("orders/v1");
        reach.SourceContainerName.ShouldBe("orders");
        reach.DestinationDataSet.ShouldBe("OrdersCopy");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AUnionAndALookupInsideABranchAreReadWhereverTheStageNests()
    {
        var saved = SavedOverADataSet();
        saved.Transforms =
        [
            new PipelineTransformDto
            {
                Name = "Split",
                OperationType = "Branch",
                Paths =
                [
                    new PipelinePathDto
                    {
                        Stages =
                        [
                            new PipelineTransformDto { Name = "AddArchive", OperationType = "Union", UnionSource = "OrdersArchive" },
                            new PipelineTransformDto
                            {
                                Name = "Enrich",
                                OperationType = "Lookup",
                                Lookup = new LookupDto { LookupDataSet = "Customers" }
                            }
                        ]
                    }
                ]
            }
        ];

        var steps = PipelineReach.Of(saved).Steps;

        steps.DataSets.ShouldBe([new ReachedDataSet("OrdersArchive", false), new ReachedDataSet("Customers", false)]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ALookupOverADataStoreContainerReadsThatContainerAndReachesNoDataSet()
    {
        var saved = SavedOverADataSet();
        saved.Transforms =
        [
            new PipelineTransformDto
            {
                Name = "Enrich",
                OperationType = "Lookup",
                Lookup = new LookupDto
                {
                    LookupDataStoreName = "Warehouse",
                    LookupDataStorePath = "ref/v1",
                    LookupContainerName = "customers"
                }
            }
        ];

        var steps = PipelineReach.Of(saved).Steps;

        steps.Containers.ShouldBe([new ReachedContainer("Warehouse", "ref/v1", "customers", false)]);
        steps.DataSets.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void LoadAndRejectStepsWriteTheirDestinationsAndReadTheirSources()
    {
        var saved = SavedOverADataSet();
        saved.LoadSteps = [Step("Staging", source: "Orders")];
        saved.RejectSteps = [Step("Rejects")];

        var steps = PipelineReach.Of(saved).Steps;

        steps.DataSets.ShouldBe([
            new ReachedDataSet("Staging", true),
            new ReachedDataSet("Orders", false),
            new ReachedDataSet("Rejects", true)]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AnExecuteStepsContainerIsWrittenByTheStore()
    {
        var saved = SavedOverADataSet();
        saved.LoadSteps = [Step("Staging", container: new PipelineLoadDestinationContainer { DataStoreName = "Warehouse", PathName = "dbo", ContainerName = "Rollup" })];

        var steps = PipelineReach.Of(saved).Steps;

        steps.Containers.ShouldBe([new ReachedContainer("Warehouse", "dbo", "Rollup", true)]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ANameSeveralStagesAndStepsShareAppearsOnce()
    {
        var saved = SavedOverADataSet();
        saved.LoadSteps = [Step("Staging"), Step("Staging")];
        saved.RejectSteps = [Step("Staging")];

        PipelineReach.Of(saved).Steps.DataSets.ShouldBe([new ReachedDataSet("Staging", true)]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ABlankNameIsNotReached()
    {
        var saved = SavedOverADataSet();
        saved.Transforms = [new PipelineTransformDto { Name = "AddNothing", OperationType = "Union", UnionSource = " " }];
        saved.LoadSteps = [Step(string.Empty)];

        PipelineReach.Of(saved).Steps.DataSets.ShouldBeEmpty();
    }
}
