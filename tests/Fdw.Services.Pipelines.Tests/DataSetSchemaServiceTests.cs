using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
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
/// failure, per-field name+type matching, and the empty-abstract-schema edge case). Only
/// <see cref="DataSetConfigurationProvider"/> is mocked (via Moq, since its <c>GetFields</c>/
/// <c>SaveFields</c> members are <c>virtual</c>) — no gateway/database involved.
/// </summary>
[Trait("Category", "Etl")]
public sealed class DataSetSchemaServiceTests
{
    private static Mock<DataSetConfigurationProvider> CreateProviderMock()
    {
        return new Mock<DataSetConfigurationProvider>(
            (ILogger<DataSetConfigurationProvider>?)null!,
            new ConfigurationGatewayProvider(),
            "ConfigurationDb",
            "data");
    }

    private static DataSetFieldDefinition Field(string name, string type, Guid dataSetId = default, int ordinal = 0) =>
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
        var providerMock = CreateProviderMock();
        providerMock
            .Setup(p => p.GetFields(dataSetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<DataSetFieldDefinition>>.Success([]));
        var service = new DataSetSchemaService(providerMock.Object, null);

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
        IReadOnlyList<DataSetFieldDefinition> fields =
        [
            Field("Id", "Guid", dataSetId, 0),
            Field("Name", "String", dataSetId, 1),
        ];
        var providerMock = CreateProviderMock();
        providerMock
            .Setup(p => p.GetFields(dataSetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<DataSetFieldDefinition>>.Success(fields));
        var service = new DataSetSchemaService(providerMock.Object, Mock.Of<ILogger<DataSetSchemaService>>());

        // Act
        var result = await service.GetSchema(dataSetId, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeSameAs(fields);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task GetSchemaReturnsFailureCarryingProviderMessageWhenProviderFails()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var providerMock = CreateProviderMock();
        providerMock
            .Setup(p => p.GetFields(dataSetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<DataSetFieldDefinition>>.Failure(new GenericMessage("gateway exploded")));
        var service = new DataSetSchemaService(providerMock.Object, Mock.Of<ILogger<DataSetSchemaService>>());

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
        var failureMock = new Mock<IGenericResult<IReadOnlyList<DataSetFieldDefinition>>>();
        failureMock.SetupGet(r => r.IsSuccess).Returns(false);
        failureMock.SetupGet(r => r.CurrentMessage).Returns((string?)null);
        var providerMock = CreateProviderMock();
        providerMock
            .Setup(p => p.GetFields(dataSetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(failureMock.Object);
        var service = new DataSetSchemaService(providerMock.Object, Mock.Of<ILogger<DataSetSchemaService>>());

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
        IReadOnlyList<DataSetFieldDefinition> fields = [Field("Id", "Guid", dataSetId)];
        var providerMock = CreateProviderMock();
        providerMock
            .Setup(p => p.SaveFields(dataSetId, fields, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());
        var service = new DataSetSchemaService(providerMock.Object, Mock.Of<ILogger<DataSetSchemaService>>());

        // Act
        var result = await service.SaveSchema(dataSetId, fields, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task SaveSchemaReturnsFailureCarryingProviderMessageWhenProviderFails()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        IReadOnlyList<DataSetFieldDefinition> fields = [Field("Id", "Guid", dataSetId)];
        var providerMock = CreateProviderMock();
        providerMock
            .Setup(p => p.SaveFields(dataSetId, fields, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new GenericMessage("write conflict")));
        var service = new DataSetSchemaService(providerMock.Object, Mock.Of<ILogger<DataSetSchemaService>>());

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
        IReadOnlyList<DataSetFieldDefinition> fields = [Field("Id", "Guid", dataSetId)];
        var failureMock = new Mock<IGenericResult>();
        failureMock.SetupGet(r => r.IsSuccess).Returns(false);
        failureMock.SetupGet(r => r.CurrentMessage).Returns((string?)null);
        var providerMock = CreateProviderMock();
        providerMock
            .Setup(p => p.SaveFields(dataSetId, fields, It.IsAny<CancellationToken>()))
            .ReturnsAsync(failureMock.Object);
        var service = new DataSetSchemaService(providerMock.Object, Mock.Of<ILogger<DataSetSchemaService>>());

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
        IReadOnlyList<DataSetFieldDefinition> physicalFields =
        [
            Field("Amount", "Decimal"),
            Field("Name", "String"),
        ];
        IReadOnlyList<DataSetFieldDefinition> abstractFields = [Field("amount", "decimal")];
        var providerMock = CreateProviderMock();
        providerMock.Setup(p => p.GetFields(physicalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<DataSetFieldDefinition>>.Success(physicalFields));
        providerMock.Setup(p => p.GetFields(abstractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<DataSetFieldDefinition>>.Success(abstractFields));
        var service = new DataSetSchemaService(providerMock.Object, Mock.Of<ILogger<DataSetSchemaService>>());

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
        var providerMock = CreateProviderMock();
        providerMock.Setup(p => p.GetFields(physicalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<DataSetFieldDefinition>>.Success([Field("Name", "String")]));
        providerMock.Setup(p => p.GetFields(abstractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<DataSetFieldDefinition>>.Success([]));
        var service = new DataSetSchemaService(providerMock.Object, Mock.Of<ILogger<DataSetSchemaService>>());

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
        var physicalId = Guid.NewGuid();
        var abstractId = Guid.NewGuid();
        var providerMock = CreateProviderMock();
        providerMock.Setup(p => p.GetFields(physicalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<DataSetFieldDefinition>>.Failure(new GenericMessage("physical load failed")));
        var service = new DataSetSchemaService(providerMock.Object, Mock.Of<ILogger<DataSetSchemaService>>());

        // Act
        var result = await service.ValidateConformance(physicalId, abstractId, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain(physicalId.ToString());
        providerMock.Verify(p => p.GetFields(abstractId, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task ValidateConformancePropagatesFailureWhenAbstractSchemaLoadFails()
    {
        // Arrange
        var physicalId = Guid.NewGuid();
        var abstractId = Guid.NewGuid();
        var providerMock = CreateProviderMock();
        providerMock.Setup(p => p.GetFields(physicalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<DataSetFieldDefinition>>.Success([Field("Name", "String")]));
        providerMock.Setup(p => p.GetFields(abstractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<DataSetFieldDefinition>>.Failure(new GenericMessage("abstract load failed")));
        var service = new DataSetSchemaService(providerMock.Object, Mock.Of<ILogger<DataSetSchemaService>>());

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
        var providerMock = CreateProviderMock();
        providerMock.Setup(p => p.GetFields(physicalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<DataSetFieldDefinition>>.Success([Field("Name", "String")]));
        providerMock.Setup(p => p.GetFields(abstractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<DataSetFieldDefinition>>.Success([Field("MissingField", "String")]));
        var service = new DataSetSchemaService(providerMock.Object, Mock.Of<ILogger<DataSetSchemaService>>());

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
        var providerMock = CreateProviderMock();
        providerMock.Setup(p => p.GetFields(physicalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<DataSetFieldDefinition>>.Success([Field("Amount", "Decimal")]));
        providerMock.Setup(p => p.GetFields(abstractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<DataSetFieldDefinition>>.Success([Field("Amount", "Int32")]));
        var service = new DataSetSchemaService(providerMock.Object, Mock.Of<ILogger<DataSetSchemaService>>());

        // Act
        var result = await service.ValidateConformance(physicalId, abstractId, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain("Amount");
    }
}
