using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.DataNodes.Tests;

/// <summary>
/// Tests for <see cref="ConfiguredDataStoreProvider"/> — the pure, gateway-free DataStore provider
/// that resolves stores from an injected <see cref="IServiceConfigurationProvider{TConfig}"/> and
/// dispatches transport builds through an injected <see cref="IDataStoreBuilderSelector"/>.
/// </summary>
public sealed class ConfiguredDataStoreProviderTests
{
    private readonly Mock<ILogger<ConfiguredDataStoreProvider>> _logger = new();
    private readonly Mock<IServiceConfigurationProvider<DataStoreConfiguration>> _configurationProvider = new();
    private readonly Mock<IDataStoreBuilderSelector> _builderSelector = new();

    private ConfiguredDataStoreProvider CreateSut()
        => new(_logger.Object, _configurationProvider.Object, _builderSelector.Object);

    // ====================================================================
    // Constructor null-guards
    // ====================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CtorThrowsArgumentNullExceptionWhenConfigurationProviderIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new ConfiguredDataStoreProvider(_logger.Object, null!, _builderSelector.Object));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CtorThrowsArgumentNullExceptionWhenBuilderSelectorIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new ConfiguredDataStoreProvider(_logger.Object, _configurationProvider.Object, null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CtorAcceptsNullLoggerAndFallsBackToNullLogger()
    {
        // Act
        var sut = new ConfiguredDataStoreProvider(null, _configurationProvider.Object, _builderSelector.Object);

        // Assert
        sut.ShouldNotBeNull();
    }

    // ====================================================================
    // Get(string name) — fail-loud validation
    // ====================================================================

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task GetByNameReturnsFailureWhenNameIsNullOrWhitespace(string? name)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = await sut.Get(name!, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        _configurationProvider.Verify(
            p => p.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task GetByNameReturnsFailureWhenConfigurationProviderFails()
    {
        // Arrange
        _configurationProvider
            .Setup(p => p.Get("Store1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DataStoreConfiguration>.Failure(new GenericMessage("not found")));
        var sut = CreateSut();

        // Act
        var result = await sut.Get("Store1", TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        _builderSelector.Verify(
            s => s.Select(It.IsAny<DataStoreConfiguration>(), It.IsAny<ILogger>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task GetByNameReturnsFailureWhenConfigurationProviderReturnsNullValue()
    {
        // Arrange
        _configurationProvider
            .Setup(p => p.Get("Store1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DataStoreConfiguration>.Success(null!));
        var sut = CreateSut();

        // Act
        var result = await sut.Get("Store1", TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        _builderSelector.Verify(
            s => s.Select(It.IsAny<DataStoreConfiguration>(), It.IsAny<ILogger>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task GetByNameReturnsFailureWhenSelectorFails()
    {
        // Arrange
        var cfg = new DataStoreConfiguration { Name = "Store1" };
        _configurationProvider
            .Setup(p => p.Get("Store1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DataStoreConfiguration>.Success(cfg));
        _builderSelector
            .Setup(s => s.Select(It.IsAny<DataStoreConfiguration>(), It.IsAny<ILogger>()))
            .Returns(GenericResult<IDataStoreBuilder>.Failure(new GenericMessage("no builder")));
        var sut = CreateSut();

        // Act
        var result = await sut.Get("Store1", TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task GetByNameReturnsFailureWhenConfigureFailsAndBuildIsNeverCalled()
    {
        // Arrange
        var cfg = new DataStoreConfiguration { Name = "Store1" };
        var builder = new Mock<IDataStoreBuilder>();
        builder
            .Setup(b => b.Configure(It.IsAny<IGenericConfiguration>()))
            .Returns(GenericResult.Failure(new GenericMessage("bad config")));
        _configurationProvider
            .Setup(p => p.Get("Store1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DataStoreConfiguration>.Success(cfg));
        _builderSelector
            .Setup(s => s.Select(It.IsAny<DataStoreConfiguration>(), It.IsAny<ILogger>()))
            .Returns(GenericResult<IDataStoreBuilder>.Success(builder.Object));
        var sut = CreateSut();

        // Act
        var result = await sut.Get("Store1", TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        builder.Verify(b => b.Build(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task GetByNameReturnsSuccessWithBuiltStoreWhenPipelineSucceeds()
    {
        // Arrange
        var cfg = new DataStoreConfiguration { Name = "Store1", ServiceOptionType = "File" };
        var builtStore = new Mock<IDataStore>().Object;
        var builder = new Mock<IDataStoreBuilder>();
        builder
            .Setup(b => b.Configure(It.IsAny<IGenericConfiguration>()))
            .Returns(GenericResult.Success());
        builder
            .Setup(b => b.Build(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataStore>.Success(builtStore));
        _configurationProvider
            .Setup(p => p.Get("Store1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DataStoreConfiguration>.Success(cfg));
        _builderSelector
            .Setup(s => s.Select(It.IsAny<DataStoreConfiguration>(), It.IsAny<ILogger>()))
            .Returns(GenericResult<IDataStoreBuilder>.Success(builder.Object));
        var sut = CreateSut();

        // Act
        var result = await sut.Get("Store1", TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeSameAs(builtStore);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task GetByNameReturnsFailureWhenBuildFails()
    {
        // Arrange
        var cfg = new DataStoreConfiguration { Name = "Store1" };
        var builder = new Mock<IDataStoreBuilder>();
        builder
            .Setup(b => b.Configure(It.IsAny<IGenericConfiguration>()))
            .Returns(GenericResult.Success());
        builder
            .Setup(b => b.Build(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataStore>.Failure(new GenericMessage("build failed")));
        _configurationProvider
            .Setup(p => p.Get("Store1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DataStoreConfiguration>.Success(cfg));
        _builderSelector
            .Setup(s => s.Select(It.IsAny<DataStoreConfiguration>(), It.IsAny<ILogger>()))
            .Returns(GenericResult<IDataStoreBuilder>.Success(builder.Object));
        var sut = CreateSut();

        // Act
        var result = await sut.Get("Store1", TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    // ====================================================================
    // Get(Guid id)
    // ====================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task GetByIdReturnsFailureWhenConfigurationProviderFails()
    {
        // Arrange
        var id = Guid.NewGuid();
        _configurationProvider
            .Setup(p => p.Get(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DataStoreConfiguration>.Failure(new GenericMessage("not found")));
        var sut = CreateSut();

        // Act
        var result = await sut.Get(id, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        _configurationProvider.Verify(
            p => p.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task GetByIdReturnsFailureWhenConfigurationProviderReturnsNullValue()
    {
        // Arrange
        var id = Guid.NewGuid();
        _configurationProvider
            .Setup(p => p.Get(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DataStoreConfiguration>.Success(null!));
        var sut = CreateSut();

        // Act
        var result = await sut.Get(id, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task GetByIdReturnsFailureWhenResolvedConfigurationHasWhitespaceName()
    {
        // Arrange
        var id = Guid.NewGuid();
        _configurationProvider
            .Setup(p => p.Get(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DataStoreConfiguration>.Success(new DataStoreConfiguration { Id = id, Name = "   " }));
        var sut = CreateSut();

        // Act
        var result = await sut.Get(id, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        _configurationProvider.Verify(
            p => p.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task GetByIdDelegatesToGetByNameWhenResolvedConfigurationHasName()
    {
        // Arrange
        var id = Guid.NewGuid();
        var cfg = new DataStoreConfiguration { Id = id, Name = "Store1" };
        var builtStore = new Mock<IDataStore>().Object;
        var builder = new Mock<IDataStoreBuilder>();
        builder.Setup(b => b.Configure(It.IsAny<IGenericConfiguration>())).Returns(GenericResult.Success());
        builder.Setup(b => b.Build(It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IDataStore>.Success(builtStore));
        _configurationProvider
            .Setup(p => p.Get(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DataStoreConfiguration>.Success(cfg));
        _configurationProvider
            .Setup(p => p.Get("Store1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DataStoreConfiguration>.Success(cfg));
        _builderSelector
            .Setup(s => s.Select(It.IsAny<DataStoreConfiguration>(), It.IsAny<ILogger>()))
            .Returns(GenericResult<IDataStoreBuilder>.Success(builder.Object));
        var sut = CreateSut();

        // Act
        var result = await sut.Get(id, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeSameAs(builtStore);
        _configurationProvider.Verify(p => p.Get("Store1", It.IsAny<CancellationToken>()), Times.Once);
    }

    // ====================================================================
    // Get() — all DataStores
    // ====================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task GetAllReturnsFailureWhenConfigurationProviderFails()
    {
        // Arrange
        _configurationProvider
            .Setup(p => p.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<DataStoreConfiguration>>.Failure(new GenericMessage("load failed")));
        var sut = CreateSut();

        // Act
        var result = await sut.Get(TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        _builderSelector.Verify(
            s => s.Select(It.IsAny<DataStoreConfiguration>(), It.IsAny<ILogger>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task GetAllReturnsSuccessWithEmptyListWhenNoConfigurationsExist()
    {
        // Arrange
        _configurationProvider
            .Setup(p => p.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<DataStoreConfiguration>>.Success([]));
        var sut = CreateSut();

        // Act
        var result = await sut.Get(TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task GetAllSkipsShallowConfigurationsWithWhitespaceName()
    {
        // Arrange
        _configurationProvider
            .Setup(p => p.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<DataStoreConfiguration>>.Success(
                [new DataStoreConfiguration { Name = "   " }]));
        var sut = CreateSut();

        // Act
        var result = await sut.Get(TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
        _configurationProvider.Verify(
            p => p.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task GetAllComposesEachStoreViaGetByNameWhenComposedFetchSucceeds()
    {
        // Arrange
        var shallow = new DataStoreConfiguration { Name = "StoreA" };
        var composed = new DataStoreConfiguration { Name = "StoreA", Description = "composed" };
        var builtStore = new Mock<IDataStore>().Object;
        var builder = new Mock<IDataStoreBuilder>();
        builder.Setup(b => b.Configure(It.IsAny<IGenericConfiguration>())).Returns(GenericResult.Success());
        builder.Setup(b => b.Build(It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IDataStore>.Success(builtStore));
        _configurationProvider
            .Setup(p => p.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<DataStoreConfiguration>>.Success([shallow]));
        _configurationProvider
            .Setup(p => p.Get("StoreA", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DataStoreConfiguration>.Success(composed));
        _builderSelector
            .Setup(s => s.Select(composed, It.IsAny<ILogger>()))
            .Returns(GenericResult<IDataStoreBuilder>.Success(builder.Object));
        var sut = CreateSut();

        // Act
        var result = await sut.Get(TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Count.ShouldBe(1);
        result.Value[0].ShouldBeSameAs(builtStore);
        _builderSelector.Verify(s => s.Select(composed, It.IsAny<ILogger>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task GetAllFallsBackToShallowConfigurationWhenComposedFetchFails()
    {
        // Arrange
        var shallow = new DataStoreConfiguration { Name = "StoreA" };
        var builtStore = new Mock<IDataStore>().Object;
        var builder = new Mock<IDataStoreBuilder>();
        builder.Setup(b => b.Configure(It.IsAny<IGenericConfiguration>())).Returns(GenericResult.Success());
        builder.Setup(b => b.Build(It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IDataStore>.Success(builtStore));
        _configurationProvider
            .Setup(p => p.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<DataStoreConfiguration>>.Success([shallow]));
        _configurationProvider
            .Setup(p => p.Get("StoreA", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DataStoreConfiguration>.Failure(new GenericMessage("composed fetch failed")));
        _builderSelector
            .Setup(s => s.Select(shallow, It.IsAny<ILogger>()))
            .Returns(GenericResult<IDataStoreBuilder>.Success(builder.Object));
        var sut = CreateSut();

        // Act
        var result = await sut.Get(TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Count.ShouldBe(1);
        _builderSelector.Verify(s => s.Select(shallow, It.IsAny<ILogger>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task GetAllExcludesStoresWhoseBuildFailsButStillReturnsSuccess()
    {
        // Arrange
        var shallow = new DataStoreConfiguration { Name = "StoreA" };
        _configurationProvider
            .Setup(p => p.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<DataStoreConfiguration>>.Success([shallow]));
        _configurationProvider
            .Setup(p => p.Get("StoreA", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DataStoreConfiguration>.Success(shallow));
        _builderSelector
            .Setup(s => s.Select(It.IsAny<DataStoreConfiguration>(), It.IsAny<ILogger>()))
            .Returns(GenericResult<IDataStoreBuilder>.Failure(new GenericMessage("no builder")));
        var sut = CreateSut();

        // Act
        var result = await sut.Get(TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    // ====================================================================
    // Get(dataStoreName, pathName) — dot-walk
    // ====================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task GetPathReturnsFailureWhenStoreLookupFails()
    {
        // Arrange
        _configurationProvider
            .Setup(p => p.Get("Store1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DataStoreConfiguration>.Failure(new GenericMessage("not found")));
        var sut = CreateSut();

        // Act
        var result = await sut.Get("Store1", "Path1", TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task GetPathReturnsSuccessWhenPathFoundOnBuiltStore()
    {
        // Arrange
        var pathMock = new Mock<IDataPath>().Object;
        var storeMock = new Mock<IDataStore>();
        storeMock.Setup(s => s.Path("Path1")).Returns(GenericResult<IDataPath>.Success(pathMock));
        SetupSuccessfulBuild("Store1", storeMock.Object);
        var sut = CreateSut();

        // Act
        var result = await sut.Get("Store1", "Path1", TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeSameAs(pathMock);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task GetPathReturnsFailureWhenPathNotFoundOnBuiltStore()
    {
        // Arrange
        var storeMock = new Mock<IDataStore>();
        storeMock
            .Setup(s => s.Path("MissingPath"))
            .Returns(GenericResult<IDataPath>.Failure(new GenericMessage("path not found")));
        SetupSuccessfulBuild("Store1", storeMock.Object);
        var sut = CreateSut();

        // Act
        var result = await sut.Get("Store1", "MissingPath", TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    // ====================================================================
    // Get(dataStoreName, pathName, containerName) — dot-walk
    // ====================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task GetContainerReturnsFailureWhenPathLookupFails()
    {
        // Arrange
        _configurationProvider
            .Setup(p => p.Get("Store1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DataStoreConfiguration>.Failure(new GenericMessage("not found")));
        var sut = CreateSut();

        // Act
        var result = await sut.Get("Store1", "Path1", "Container1", TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task GetContainerReturnsSuccessWhenContainerFoundOnResolvedPath()
    {
        // Arrange
        var containerMock = new Mock<IDataContainer>().Object;
        var pathMock = new Mock<IDataPath>();
        pathMock.Setup(p => p.Container("Container1")).Returns(GenericResult<IDataContainer>.Success(containerMock));
        var storeMock = new Mock<IDataStore>();
        storeMock.Setup(s => s.Path("Path1")).Returns(GenericResult<IDataPath>.Success(pathMock.Object));
        SetupSuccessfulBuild("Store1", storeMock.Object);
        var sut = CreateSut();

        // Act
        var result = await sut.Get("Store1", "Path1", "Container1", TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeSameAs(containerMock);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task GetContainerReturnsFailureWhenContainerNotFoundOnResolvedPath()
    {
        // Arrange
        var pathMock = new Mock<IDataPath>();
        pathMock
            .Setup(p => p.Container("MissingContainer"))
            .Returns(GenericResult<IDataContainer>.Failure(new GenericMessage("container not found")));
        var storeMock = new Mock<IDataStore>();
        storeMock.Setup(s => s.Path("Path1")).Returns(GenericResult<IDataPath>.Success(pathMock.Object));
        SetupSuccessfulBuild("Store1", storeMock.Object);
        var sut = CreateSut();

        // Act
        var result = await sut.Get("Store1", "Path1", "MissingContainer", TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    // ====================================================================
    // Test helpers
    // ====================================================================

    // Why: wires the full config->selector->configure->build pipeline to succeed and return the
    // supplied IDataStore, so the dot-walk overloads under test can exercise their Path/Container
    // delegation against a controlled built tree.
    private void SetupSuccessfulBuild(string storeName, IDataStore builtStore)
    {
        var cfg = new DataStoreConfiguration { Name = storeName };
        var builder = new Mock<IDataStoreBuilder>();
        builder.Setup(b => b.Configure(It.IsAny<IGenericConfiguration>())).Returns(GenericResult.Success());
        builder.Setup(b => b.Build(It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IDataStore>.Success(builtStore));
        _configurationProvider
            .Setup(p => p.Get(storeName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DataStoreConfiguration>.Success(cfg));
        _builderSelector
            .Setup(s => s.Select(It.IsAny<DataStoreConfiguration>(), It.IsAny<ILogger>()))
            .Returns(GenericResult<IDataStoreBuilder>.Success(builder.Object));
    }
}
