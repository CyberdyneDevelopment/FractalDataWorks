using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Execution;
using Fdw.Services.Data.Results;
using Fdw.Services.Data.Transformations;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>FDW-3638: a variation is a data set of kind Variation, and the gateway has nothing of its own to run for one.</summary>
public sealed class VariationDataSetTypeTests
{
    private static DataSetExecutionContext Context(IDataSetImplementationConfiguration config)
    {
        var gateways = new Mock<IConfigurationGatewayProvider>();
        return new DataSetExecutionContext(
            config,
            new Mock<IDataConnectionProvider>().Object,
            new Mock<IDataStoreProvider>().Object,
            new PredicatePushdownAnalyzer(NullLogger<PredicatePushdownAnalyzer>.Instance),
            new FieldMappingTransformationResolver(
                gateways.Object,
                new TransformationConfigurationProvider(
                    NullLogger<TransformationConfigurationProvider>.Instance, gateways.Object, "PlatformConfiguration", "transform", "Transformation")),
            NullLogger.Instance);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW3638")]
    public void VariationIsAKindOfDataSet()
    {
        var kind = DataSetTypes.ByName("Variation");

        ReferenceEquals(kind, DataSetTypes.NotFound).ShouldBeFalse();
        kind.ShouldBeOfType<VariationDataSetType>();
        kind.PagesInStore.ShouldBeFalse();
        kind.AggregatesInStore.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW3638")]
    public async Task ACommandAddressedToAVariationByNameIsRefusedAndNotServedFromItsBase()
    {
        var variation = new VariationDataSetConfiguration
        {
            Name = "GasPrices",
            Implementation = "Variation",
            OwnerWorkspaceId = Guid.NewGuid(),
            BaseDataSetId = Guid.NewGuid(),
        };

        var result = await new VariationDataSetType().Execute<IEnumerable<object>>(
            Context(variation), new QueryCommand<object>(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBeOfType<DataSetVariationNotExecutableCode>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW3638")]
    public async Task ADataSetOfAnotherKindIsAKindMismatchNotARefusalToServeAVariation()
    {
        var simple = new SimpleDataSetConfiguration { Name = "Teams", Implementation = "Variation" };

        var result = await new VariationDataSetType().Execute<IEnumerable<object>>(
            Context(simple), new QueryCommand<object>(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBeOfType<DataSetKindMismatchCode>();
    }
}
