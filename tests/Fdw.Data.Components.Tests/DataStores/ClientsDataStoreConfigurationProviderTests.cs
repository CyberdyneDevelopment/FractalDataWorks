using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Components.DataStores;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Data.Clients;
using Fdw.Services.Data.Clients.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Data.Components.Tests.DataStores;

/// <summary>
/// Tests for <see cref="ClientsDataStoreConfigurationProvider"/> — the UI-side
/// <see cref="Fdw.Services.Abstractions.IServiceConfigurationProvider{TConfig}"/> that maps
/// <see cref="DataStoreApiClient"/> DTOs to <see cref="DataStoreConfiguration"/>.
/// </summary>
public sealed class ClientsDataStoreConfigurationProviderTests
{
    private readonly Mock<DataStoreApiClient> _apiClient = new(new HttpClient(), NullLogger<DataStoreApiClient>.Instance);

    private ClientsDataStoreConfigurationProvider CreateSut()
        => new(NullLogger<ClientsDataStoreConfigurationProvider>.Instance, _apiClient.Object);

    // ====================================================================
    // Constructor
    // ====================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CtorThrowsArgumentNullExceptionWhenApiClientIsNull()
    {
        Should.Throw<ArgumentNullException>(() =>
            new ClientsDataStoreConfigurationProvider(NullLogger<ClientsDataStoreConfigurationProvider>.Instance, null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CtorAcceptsNullLoggerAndFallsBackToNullLogger()
    {
        var sut = new ClientsDataStoreConfigurationProvider(null, _apiClient.Object);

        sut.ShouldNotBeNull();
    }

    // ====================================================================
    // Get(string name)
    // ====================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task GetByNameReturnsMappedConfigurationWithPathsContainersAndFieldsWhenClientSucceeds()
    {
        var field = new DataStoreFieldPayload
        {
            Id = Guid.NewGuid(),
            Name = "CustomerId",
            NativeDataType = "int",
            IsNullable = false,
            Ordinal = 0,
        };
        var container = new DataStoreContainerPayload
        {
            Id = Guid.NewGuid(),
            Name = "Customers",
            ContainerType = "Table",
            Fields = [field],
        };
        var path = new DataStorePathPayload
        {
            Id = Guid.NewGuid(),
            Name = "dbo",
            PhysicalPath = "dbo",
            PathType = "Schema",
            Containers = [container],
        };
        var detail = new DataStoreDetailPayload
        {
            Id = Guid.NewGuid(),
            Name = "Store1",
            DisplayName = "Store One",
            Description = "test store",
            StoreType = "MsSql",
            IsActive = true,
            Paths = [path],
        };
        _apiClient
            .Setup(c => c.GetDataStore("Store1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DataStoreDetailPayload>.Success(detail));
        var sut = CreateSut();

        var result = await sut.Get("Store1", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        var configuration = result.Value;
        configuration.Id.ShouldBe(detail.Id);
        configuration.Name.ShouldBe("Store1");
        configuration.DisplayName.ShouldBe("Store One");
        configuration.ServiceOptionType.ShouldBe("MsSql");
        configuration.Paths.Count.ShouldBe(1);

        var mappedPath = configuration.Paths[0];
        mappedPath.Name.ShouldBe("dbo");
        mappedPath.PathValue.ShouldBe("dbo");
        mappedPath.DataStoreId.ShouldBe(configuration.Id);
        mappedPath.Containers.Count.ShouldBe(1);

        var mappedContainer = mappedPath.Containers[0];
        mappedContainer.Name.ShouldBe("Customers");
        mappedContainer.TypeId.ShouldBe("Table");
        mappedContainer.DataPathId.ShouldBe(mappedPath.Id);
        mappedContainer.Fields.Count.ShouldBe(1);

        var mappedField = mappedContainer.Fields[0];
        mappedField.Name.ShouldBe("CustomerId");
        mappedField.DataType.ShouldBe("int");
        mappedField.IsNullable.ShouldBeFalse();
        mappedField.DataContainerId.ShouldBe(mappedContainer.Id);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task GetByNameLeavesKnownGapFieldsAtPocoDefaults()
    {
        var detail = new DataStoreDetailPayload { Id = Guid.NewGuid(), Name = "Store1" };
        _apiClient
            .Setup(c => c.GetDataStore("Store1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DataStoreDetailPayload>.Success(detail));
        var sut = CreateSut();

        var result = await sut.Get("Store1", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        // Why: ConnectionId is a known API gap (DTO carries ConnectionName, not the Guid) — must stay
        // at the POCO default rather than being invented (NO FALLBACKS).
        result.Value.ShouldNotBeNull();
        result.Value.ConnectionId.ShouldBe(Guid.Empty);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task GetByNameReturnsFailureWhenClientFails()
    {
        _apiClient
            .Setup(c => c.GetDataStore("MissingStore", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DataStoreDetailPayload>.Failure(new GenericMessage("not found")));
        var sut = CreateSut();

        var result = await sut.Get("MissingStore", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task GetByNameReturnsFailureWhenClientReturnsSuccessWithNullValue()
    {
        _apiClient
            .Setup(c => c.GetDataStore("Store1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DataStoreDetailPayload>.Success(null!));
        var sut = CreateSut();

        var result = await sut.Get("Store1", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    // ====================================================================
    // Get(Guid id)
    // ====================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task GetByIdReturnsFailureWhenClientListFails()
    {
        _apiClient
            .Setup(c => c.GetDataStores(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<DataStoreSummaryPayload>>.Failure(new GenericMessage("load failed")));
        var sut = CreateSut();

        var result = await sut.Get(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task GetByIdReturnsFailureWhenNoSummaryMatchesId()
    {
        _apiClient
            .Setup(c => c.GetDataStores(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<DataStoreSummaryPayload>>.Success(
                [new DataStoreSummaryPayload { Id = Guid.NewGuid(), Name = "Other" }]));
        var sut = CreateSut();

        var result = await sut.Get(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task GetByIdDelegatesToGetByNameWhenSummaryFound()
    {
        var id = Guid.NewGuid();
        _apiClient
            .Setup(c => c.GetDataStores(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<DataStoreSummaryPayload>>.Success(
                [new DataStoreSummaryPayload { Id = id, Name = "Store1" }]));
        _apiClient
            .Setup(c => c.GetDataStore("Store1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<DataStoreDetailPayload>.Success(new DataStoreDetailPayload { Id = id, Name = "Store1" }));
        var sut = CreateSut();

        var result = await sut.Get(id, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Name.ShouldBe("Store1");
        _apiClient.Verify(c => c.GetDataStore("Store1", It.IsAny<CancellationToken>()), Times.Once);
    }

    // ====================================================================
    // Get() — all DataStores (shallow)
    // ====================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task GetAllReturnsMappedShallowConfigurationsWhenClientSucceeds()
    {
        var summary = new DataStoreSummaryPayload { Id = Guid.NewGuid(), Name = "Store1", Description = "d" };
        _apiClient
            .Setup(c => c.GetDataStores(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<DataStoreSummaryPayload>>.Success([summary]));
        var sut = CreateSut();

        var result = await sut.Get(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Count.ShouldBe(1);
        result.Value[0].Name.ShouldBe("Store1");
        // Why: the shallow summary endpoint carries no Paths — ConfiguredDataStoreProvider.Get()
        // composes the full aggregate per store via a follow-up Get(name).
        result.Value[0].Paths.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task GetAllReturnsFailureWhenClientFails()
    {
        _apiClient
            .Setup(c => c.GetDataStores(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<DataStoreSummaryPayload>>.Failure(new GenericMessage("load failed")));
        var sut = CreateSut();

        var result = await sut.Get(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    // ====================================================================
    // Save / Delete — read-only provider, not supported
    // ====================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task SaveReturnsFailureBecauseProviderIsReadOnly()
    {
        var sut = CreateSut();

        var result = await sut.Save(new DataStoreConfiguration { Name = "Store1" }, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task DeleteByIdReturnsFailureBecauseProviderIsReadOnly()
    {
        var sut = CreateSut();

        var result = await sut.Delete(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task DeleteByNameReturnsFailureBecauseProviderIsReadOnly()
    {
        var sut = CreateSut();

        var result = await sut.Delete("Store1", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }
}
