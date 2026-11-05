using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>FDW-3638: the variation provider narrows the data set provider to the data sets of kind Variation.</summary>
public sealed class DataSetVariationConfigurationProviderTests
{
    private static readonly Guid VariationId = Guid.NewGuid();
    private static readonly Guid SimpleId = Guid.NewGuid();

    private readonly Mock<IDataSetConfigurationProvider> _dataSets = new();
    private readonly VariationDataSetConfiguration _variation = new()
    {
        Id = VariationId,
        Name = "AbacusSolutions/GasPrices",
        Implementation = "Variation",
        OwnerWorkspaceId = Guid.NewGuid(),
        BaseDataSetId = Guid.NewGuid(),
    };

    private readonly SimpleDataSetConfiguration _simple = new() { Id = SimpleId, Name = "GasObservations", Implementation = "Simple" };

    private DataSetVariationConfigurationProvider Provider()
    {
        _dataSets
            .Setup(p => p.Get(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string name, int _, CancellationToken _) =>
                GenericResult<IDataSetImplementationConfiguration>.Success(name == _variation.Name ? _variation : _simple));
        _dataSets
            .Setup(p => p.Get(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, int _, CancellationToken _) =>
                GenericResult<IDataSetImplementationConfiguration>.Success(id == VariationId ? _variation : _simple));
        _dataSets
            .Setup(p => p.Delete(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        return new DataSetVariationConfigurationProvider(
            _dataSets.Object,
            new DataSetVariationListProvider(
                NullLogger<DataSetVariationListProvider>.Instance, Mock.Of<IConfigurationGatewayProvider>(), "PlatformConfiguration", "data", "DataSetVariationList"),
            NullLogger<DataSetVariationConfigurationProvider>.Instance);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW3638")]
    public async Task AVariationReadByNameIsReturned()
    {
        var result = await Provider().Get("AbacusSolutions/GasPrices", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value.ShouldBeSameAs(_variation);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW3638")]
    public async Task ADataSetOfAnotherKindIsNotAVariationAndAnswersNothing()
    {
        var result = await Provider().Get("GasObservations", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW3638")]
    public async Task AVariationReadByIdIsReturnedAndAnotherKindIsNot()
    {
        var provider = Provider();

        (await provider.Get(VariationId, TestContext.Current.CancellationToken)).Value.ShouldBeSameAs(_variation);
        (await provider.Get(SimpleId, TestContext.Current.CancellationToken)).Value.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW3638")]
    public async Task AVariationIsRetiredThroughTheDataSetProvider()
    {
        var result = await Provider().Delete(VariationId, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        _dataSets.Verify(p => p.Delete(VariationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW3638")]
    public async Task ADataSetThatIsNotAVariationIsNotRetiredByTheVariationProvider()
    {
        var result = await Provider().Delete(SimpleId, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        _dataSets.Verify(p => p.Delete(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
