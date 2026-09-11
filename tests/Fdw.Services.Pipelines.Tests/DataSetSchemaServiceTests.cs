using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Messages;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Pipelines.Tests.TestSupport;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Pipelines.Tests;

/// <summary>
/// Covers the orchestration (load -&gt; validate) logic in <see cref="DataSetSchemaService"/>: the
/// happy path for each of the three service verbs, provider-failure propagation (including the
/// "CurrentMessage is null" fallback-text branch), and every branch of
/// <see cref="DataSetSchemaService.ValidateConformance"/> (short-circuit on physical/abstract load
/// failure, per-field name+type matching, and the empty-abstract-schema edge case).
/// </summary>
/// <remarks>
/// <see cref="DataSetConfigurationProvider"/> is REAL here, over a faked configuration store — see
/// <see cref="ConfigurationStore{TProvider,TContract}"/>. It is sealed and its reads are not
/// virtual, so nothing can stand in for it; a test states what the store HOLDS and the provider
/// resolves it the way production does. No gateway/database is involved.
/// </remarks>
[Trait("Category", "Etl")]
public sealed class DataSetSchemaServiceTests
{
    private static DataSetImplementationConfiguration DataSet(
        Guid id, string name, params DataSetFieldConfiguration[] fields) =>
        new() { Id = id, Name = name, Fields = [.. fields] };

    private static DataSetFieldConfiguration Field(
        string name, string type, int ordinal = 0, bool isNullable = false, string? description = null) =>
        new()
        {
            Name = name,
            TypeName = type,
            Ordinal = ordinal,
            IsNullable = isNullable,
            Description = description,
        };

    private static DataSetFieldDefinition Definition(
        string name, string type, Guid dataSetId = default, int ordinal = 0) =>
        new() { DataSetId = dataSetId, FieldName = name, ScalarTypeName = type, Ordinal = ordinal };

    // ------------------------------------------------------------------
    // Constructor
    // ------------------------------------------------------------------

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorThrowsArgumentNullExceptionWhenProviderIsNull()
    {
        Should.Throw<ArgumentNullException>(() =>
            new DataSetSchemaService(null!, Mock.Of<ILogger<DataSetSchemaService>>()));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public async Task ConstructorAcceptsNullLoggerAndFallsBackToNullLogger()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var store = ConfigurationStore.DataSets(DataSet(dataSetId, "Orders"));
        var service = new DataSetSchemaService(store.Provider, null);

        // Act
        var result = await service.GetSchema(dataSetId, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    // ------------------------------------------------------------------
    // GetSchema
    // ------------------------------------------------------------------

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task GetSchemaReturnsFieldsWhenProviderSucceeds()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var store = ConfigurationStore.DataSets(DataSet(
            dataSetId,
            "Orders",
            Field("Id", "Guid", ordinal: 0),
            Field("Name", "String", ordinal: 1, isNullable: true, description: "The order's label")));
        var service = new DataSetSchemaService(store.Provider, Mock.Of<ILogger<DataSetSchemaService>>());

        // Act
        var result = await service.GetSchema(dataSetId, TestContext.Current.CancellationToken);

        // Assert
        // Field-by-field rather than by reference: the dataset's fields are its own child rows, so
        // the service projects a definition out of each one -- there is no caller-supplied list for
        // it to hand back. The projection is the thing under test, DataSetId included: it is stamped
        // from the id asked for, and nothing on the child row carries it.
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new[]
        {
            new DataSetFieldDefinition
            {
                DataSetId = dataSetId,
                FieldName = "Id",
                ScalarTypeName = "Guid",
                IsNullable = false,
                Ordinal = 0,
                Description = null,
            },
            new DataSetFieldDefinition
            {
                DataSetId = dataSetId,
                FieldName = "Name",
                ScalarTypeName = "String",
                IsNullable = true,
                Ordinal = 1,
                Description = "The order's label",
            },
        });
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task GetSchemaReturnsFailureCarryingProviderMessageWhenProviderFails()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var store = ConfigurationStore.DataSets(DataSet(dataSetId, "Orders"));
        store.CannotBeRead(new GenericMessage("gateway exploded"));
        var service = new DataSetSchemaService(store.Provider, Mock.Of<ILogger<DataSetSchemaService>>());

        // Act
        var result = await service.GetSchema(dataSetId, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain("gateway exploded");
        result.CurrentMessage.ShouldContain(dataSetId.ToString());
        result.Messages[^1].Code.ShouldBe("PIPELINES-91000");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public async Task GetSchemaFallsBackToDefaultTextWhenProviderCurrentMessageIsNull()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var store = ConfigurationStore.DataSets(DataSet(dataSetId, "Orders"));
        store.ReadFailsWithoutMessage();
        var service = new DataSetSchemaService(store.Provider, Mock.Of<ILogger<DataSetSchemaService>>());

        // Act
        var result = await service.GetSchema(dataSetId, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain("Provider returned failure");
    }

    // ------------------------------------------------------------------
    // SaveSchema
    // ------------------------------------------------------------------

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task SaveSchemaReturnsSuccessWhenProviderSucceeds()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var store = ConfigurationStore.DataSets(DataSet(dataSetId, "Orders"));
        IReadOnlyList<DataSetFieldDefinition> fields = [Definition("Id", "Guid", dataSetId)];
        var service = new DataSetSchemaService(store.Provider, Mock.Of<ILogger<DataSetSchemaService>>());

        // Act
        var result = await service.SaveSchema(dataSetId, fields, TestContext.Current.CancellationToken);

        // Assert
        // The write lands on the implementation row, so that is where the fields have to show up --
        // a success that wrote nothing would satisfy IsSuccess alone.
        result.IsSuccess.ShouldBeTrue();
        store.Saved.ShouldNotBeNull();
        var saved = store.Saved!;
        saved.Id.ShouldBe(dataSetId);
        saved.Fields.Count.ShouldBe(1);
        saved.Fields[0].Name.ShouldBe("Id");
        saved.Fields[0].TypeName.ShouldBe("Guid");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task SaveSchemaReturnsFailureCarryingProviderMessageWhenProviderFails()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var store = ConfigurationStore.DataSets(DataSet(dataSetId, "Orders"));
        store.SaveFails(new GenericMessage("write conflict"));
        IReadOnlyList<DataSetFieldDefinition> fields = [Definition("Id", "Guid", dataSetId)];
        var service = new DataSetSchemaService(store.Provider, Mock.Of<ILogger<DataSetSchemaService>>());

        // Act
        var result = await service.SaveSchema(dataSetId, fields, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain("write conflict");
        result.CurrentMessage.ShouldContain(dataSetId.ToString());
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public async Task SaveSchemaFallsBackToDefaultTextWhenProviderCurrentMessageIsNull()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var store = ConfigurationStore.DataSets(DataSet(dataSetId, "Orders"));
        store.SaveFailsWithoutMessage();
        IReadOnlyList<DataSetFieldDefinition> fields = [Definition("Id", "Guid", dataSetId)];
        var service = new DataSetSchemaService(store.Provider, Mock.Of<ILogger<DataSetSchemaService>>());

        // Act
        var result = await service.SaveSchema(dataSetId, fields, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain("Provider returned failure");
    }

    // ------------------------------------------------------------------
    // ValidateConformance
    // ------------------------------------------------------------------

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task ValidateConformanceReturnsSuccessWhenEveryAbstractFieldMatchesCaseInsensitively()
    {
        // Arrange
        var physicalId = Guid.NewGuid();
        var abstractId = Guid.NewGuid();
        var store = ConfigurationStore.DataSets(
            DataSet(physicalId, "OrdersPhysical", Field("Amount", "Decimal"), Field("Name", "String", ordinal: 1)),
            DataSet(abstractId, "OrdersAbstract", Field("amount", "decimal")));
        var service = new DataSetSchemaService(store.Provider, Mock.Of<ILogger<DataSetSchemaService>>());

        // Act
        var result = await service.ValidateConformance(physicalId, abstractId, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public async Task ValidateConformanceReturnsSuccessWhenAbstractSchemaIsEmpty()
    {
        // Arrange
        var physicalId = Guid.NewGuid();
        var abstractId = Guid.NewGuid();
        var store = ConfigurationStore.DataSets(
            DataSet(physicalId, "OrdersPhysical", Field("Name", "String")),
            DataSet(abstractId, "OrdersAbstract"));
        var service = new DataSetSchemaService(store.Provider, Mock.Of<ILogger<DataSetSchemaService>>());

        // Act
        var result = await service.ValidateConformance(physicalId, abstractId, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task ValidateConformancePropagatesFailureWhenPhysicalSchemaLoadFails()
    {
        // Arrange
        // The abstract dataset IS held and IS readable: the only reason it can go unread is the
        // short-circuit under test.
        var physicalId = Guid.NewGuid();
        var abstractId = Guid.NewGuid();
        var store = ConfigurationStore.DataSets(
            DataSet(physicalId, "OrdersPhysical"),
            DataSet(abstractId, "OrdersAbstract", Field("Name", "String")));
        store.CannotBeRead(physicalId, new GenericMessage("physical load failed"));
        var service = new DataSetSchemaService(store.Provider, Mock.Of<ILogger<DataSetSchemaService>>());

        // Act
        var result = await service.ValidateConformance(physicalId, abstractId, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain(physicalId.ToString());
        store.Reads.ShouldContain(physicalId);
        store.Reads.ShouldNotContain(abstractId);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task ValidateConformancePropagatesFailureWhenAbstractSchemaLoadFails()
    {
        // Arrange
        var physicalId = Guid.NewGuid();
        var abstractId = Guid.NewGuid();
        var store = ConfigurationStore.DataSets(
            DataSet(physicalId, "OrdersPhysical", Field("Name", "String")),
            DataSet(abstractId, "OrdersAbstract"));
        store.CannotBeRead(abstractId, new GenericMessage("abstract load failed"));
        var service = new DataSetSchemaService(store.Provider, Mock.Of<ILogger<DataSetSchemaService>>());

        // Act
        var result = await service.ValidateConformance(physicalId, abstractId, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain(abstractId.ToString());
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ValidateConformanceFailsWhenAbstractFieldIsMissingFromPhysicalSchema()
    {
        // Arrange
        var physicalId = Guid.NewGuid();
        var abstractId = Guid.NewGuid();
        var store = ConfigurationStore.DataSets(
            DataSet(physicalId, "OrdersPhysical", Field("Name", "String")),
            DataSet(abstractId, "OrdersAbstract", Field("MissingField", "String")));
        var service = new DataSetSchemaService(store.Provider, Mock.Of<ILogger<DataSetSchemaService>>());

        // Act
        var result = await service.ValidateConformance(physicalId, abstractId, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain("MissingField");
        result.Messages[^1].Code.ShouldBe("PIPELINES-21000");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ValidateConformanceFailsWhenFieldTypeDiffersFromAbstractSchema()
    {
        // Arrange
        var physicalId = Guid.NewGuid();
        var abstractId = Guid.NewGuid();
        var store = ConfigurationStore.DataSets(
            DataSet(physicalId, "OrdersPhysical", Field("Amount", "Decimal")),
            DataSet(abstractId, "OrdersAbstract", Field("Amount", "Int32")));
        var service = new DataSetSchemaService(store.Provider, Mock.Of<ILogger<DataSetSchemaService>>());

        // Act
        var result = await service.ValidateConformance(physicalId, abstractId, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain("Amount");
    }
}
