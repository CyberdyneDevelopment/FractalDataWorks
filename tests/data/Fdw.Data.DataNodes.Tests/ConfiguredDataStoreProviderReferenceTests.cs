using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.DataNodes.Tests;

/// <summary>
/// A caller that only needs a store's existence, its connection or whether the host declared it is given the store's row, not its tree (FDW-4253).
/// </summary>
/// <remarks>
/// Why a mock that must never be asked to build: the defect was the tree. A store of 500 containers is ~2470 requests and 19 s to compose and build
/// (FDW-4252), and five callers built it to read one flag. These tests hold the read to the row at the record level and the builder untouched.
/// </remarks>
public sealed class ConfiguredDataStoreProviderReferenceTests
{
    private static readonly Guid ConnectionId = Guid.Parse("11111111-2222-3333-4444-555555555555");

    private readonly Mock<IDomainConfigurationProvider<IDataStoreImplementationConfiguration>> _configurationProvider = new();
    private readonly Mock<IDataStoreBuilderSelector> _builderSelector = new();

    private ConfiguredDataStoreProvider CreateSut()
        => new(new Mock<ILogger<ConfiguredDataStoreProvider>>().Object, _configurationProvider.Object, _builderSelector.Object);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Performance")]
    public async Task TheReferenceIsTheStoreRowReadAtTheRecordLevelAndNothingIsBuilt()
    {
        var store = new Mock<IDataStoreImplementationConfiguration>();
        store.SetupGet(s => s.Name).Returns("Warehouse");
        store.SetupGet(s => s.ConnectionId).Returns(ConnectionId);
        _configurationProvider
            .Setup(p => p.Get("Warehouse", ConfigurationLevels.RecordOnly, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataStoreImplementationConfiguration>.Success(store.Object));

        var result = await CreateSut().GetReference("Warehouse", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value.ShouldBe(new DataStoreReference("Warehouse", ConnectionId, false));
        _configurationProvider.Verify(p => p.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _configurationProvider.Verify(p => p.Get(It.IsAny<string>(), It.Is<int>(levels => levels != ConfigurationLevels.RecordOnly), It.IsAny<CancellationToken>()), Times.Never);
        _builderSelector.VerifyNoOtherCalls();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AStoreThatIsNotThereOrNotVisibleIsNotFoundAndNothingIsBuilt()
    {
        _configurationProvider
            .Setup(p => p.Get("Hidden", ConfigurationLevels.RecordOnly, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataStoreImplementationConfiguration>.Failure(new GenericMessage("not visible")));

        var result = await CreateSut().GetReference("Hidden", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        _builderSelector.VerifyNoOtherCalls();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AReadThatBrokeIsNotReportedAsAStoreThatIsNotThere()
    {
        // FDW-4281: the ETL gateway refuses a script on any failure that is not "missing", so a broken lookup must keep its own failure here as Get(name) does.
        var broke = GenericResult<IDataStoreImplementationConfiguration>.Failure(new GenericMessage("the configuration read broke"));
        _configurationProvider
            .Setup(p => p.Get("Warehouse", ConfigurationLevels.RecordOnly, It.IsAny<CancellationToken>()))
            .ReturnsAsync(broke);

        var result = await CreateSut().GetReference("Warehouse", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        FailureCategory.IsMissing(result).ShouldBeFalse();
        _builderSelector.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ABlankNameIsRefusedBeforeAnythingIsRead(string? name)
    {
        var result = await CreateSut().GetReference(name!, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        _configurationProvider.VerifyNoOtherCalls();
        _builderSelector.VerifyNoOtherCalls();
    }
}
