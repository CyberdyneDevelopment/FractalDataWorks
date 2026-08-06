using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Transforms;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Transforms;

/// <summary>
/// Tests for <see cref="LookupTransformType"/> — reads the typed
/// <see cref="PipelineTransformConfiguration.Lookups"/> cascade children (FDW-556, replaces the deleted
/// <c>ConfigurationJson</c> blob). Covers structural fail-loud gates, the single deduplicated batch
/// query per distinct connection+dataset+key group, multi-column enrichment sharing one group, and —
/// the join-correctness core of this cycle — Inner (<c>FailOnMissing</c>) vs Left join semantics on an
/// unmatched key.
/// </summary>
public sealed class LookupTransformTypeTests
{
    private readonly LookupTransformType _sut = new();

    public LookupTransformTypeTests()
    {
        // Why: the record cache is a static, process-wide dictionary keyed by connection+dataset+keys.
        // Without clearing it, a cache entry populated by one test would leak into another and make
        // the DataGateway mock verification in a later test silently skip the call it expects.
        LookupTransformType.ClearCache();
    }

    private static TransformContext CreateContext(IDataGateway? dataGateway = null) =>
        new(Guid.NewGuid(), NullLogger.Instance, new Dictionary<string, object?>(), dataGateway: dataGateway);

    private static PipelineTransformConfiguration CreateConfig(params PipelineTransformLookupConfiguration[] lookups) =>
        new() { Id = Guid.NewGuid(), Name = "Lookup1", OperationType = "Lookup", Lookups = [.. lookups] };

    private static PipelineTransformLookupConfiguration LookupColumn(
        string lookupValueField,
        string connectionName = "Conn1",
        string dataSet = "Devices",
        string lookupKeyField = "Id",
        string sourceKeyField = "DeviceId",
        string? outputFieldPrefix = null,
        string joinType = "Left") =>
        new()
        {
            Id = Guid.NewGuid(),
            LookupConnectionName = connectionName,
            LookupDataSet = dataSet,
            LookupKeyField = lookupKeyField,
            SourceKeyField = sourceKeyField,
            OutputFieldPrefix = outputFieldPrefix,
            LookupValueField = lookupValueField,
            JoinType = joinType,
        };

    private static Mock<IDataGateway> CreateGatewayMock(IGenericResult<IEnumerable<Dictionary<string, object?>>> response)
    {
        var mock = new Mock<IDataGateway>();
        mock.Setup(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(
                It.IsAny<IDataCommand>(),
                It.IsAny<DataStoreTarget>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
        return mock;
    }

    // ── Structural fail-loud branches (FDW-556 — no silent pass-through) ───────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformBatchFailsLoudWhenConfigurationIsNotPipelineTransformConfiguration()
    {
        // Arrange
        var configuration = Mock.Of<IGenericConfiguration>();
        var inputs = new List<IDictionary<string, object?>> { new Dictionary<string, object?>() };

        // Act
        var result = await _sut.TransformBatch(inputs, configuration, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("ETL-11052");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformBatchFailsLoudWhenLookupsListIsEmpty()
    {
        // Arrange
        var gateway = new Mock<IDataGateway>();
        var inputs = new List<IDictionary<string, object?>> { new Dictionary<string, object?> { ["DeviceId"] = "A1" } };

        // Act
        var result = await _sut.TransformBatch(inputs, CreateConfig(), CreateContext(gateway.Object), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("ETL-11046");
        gateway.Verify(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(
            It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformBatchFailsLoudWhenJoinTypeIsUnknown()
    {
        // Arrange
        var gateway = new Mock<IDataGateway>();
        var config = CreateConfig(LookupColumn("Name", joinType: "FullOuter"));
        var inputs = new List<IDictionary<string, object?>> { new Dictionary<string, object?> { ["DeviceId"] = "A1" } };

        // Act
        var result = await _sut.TransformBatch(inputs, config, CreateContext(gateway.Object), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("ETL-11054");
        gateway.Verify(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(
            It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task TransformBatchReturnsEmptyForEmptyInputsWithoutQueryingDataGateway()
    {
        // Arrange
        var gateway = new Mock<IDataGateway>();
        var config = CreateConfig(LookupColumn("Name"));

        // Act
        var result = await _sut.TransformBatch(
            new List<IDictionary<string, object?>>(), config, CreateContext(gateway.Object), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
        gateway.Verify(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(
            It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Matched keys: the live batched query path ───────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformBatchEnrichesRecordsForMatchedKeysViaASingleBatchedQuery()
    {
        // Arrange
        var rows = new List<Dictionary<string, object?>>
        {
            new() { ["Id"] = "A1", ["Name"] = "Device One" },
            new() { ["Id"] = "A2", ["Name"] = "Device Two" },
        };
        var gateway = CreateGatewayMock(GenericResult<IEnumerable<Dictionary<string, object?>>>.Success(rows));
        var config = CreateConfig(LookupColumn("Name"));
        var inputs = new List<IDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["DeviceId"] = "A1" },
            new Dictionary<string, object?> { ["DeviceId"] = "A2" },
        };

        // Act
        var result = await _sut.TransformBatch(inputs, config, CreateContext(gateway.Object), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var enriched = new List<IDictionary<string, object?>>(result.Value!);
        enriched.Count.ShouldBe(2);
        enriched[0]["Name"].ShouldBe("Device One");
        enriched[1]["Name"].ShouldBe("Device Two");
        gateway.Verify(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(
            It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task TransformBatchDedupesRepeatedKeysIntoASingleQuery()
    {
        // Arrange — 3 records share only 2 distinct DeviceId values.
        var rows = new List<Dictionary<string, object?>>
        {
            new() { ["Id"] = "A1", ["Name"] = "Device One" },
            new() { ["Id"] = "A2", ["Name"] = "Device Two" },
        };
        var gateway = CreateGatewayMock(GenericResult<IEnumerable<Dictionary<string, object?>>>.Success(rows));
        var config = CreateConfig(LookupColumn("Name"));
        var inputs = new List<IDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["DeviceId"] = "A1" },
            new Dictionary<string, object?> { ["DeviceId"] = "A1" },
            new Dictionary<string, object?> { ["DeviceId"] = "A2" },
        };

        // Act
        var result = await _sut.TransformBatch(inputs, config, CreateContext(gateway.Object), TestContext.Current.CancellationToken);

        // Assert
        var enriched = new List<IDictionary<string, object?>>(result.Value!);
        enriched[0]["Name"].ShouldBe("Device One");
        enriched[1]["Name"].ShouldBe("Device One");
        enriched[2]["Name"].ShouldBe("Device Two");
        gateway.Verify(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(
            It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformBatchEnrichesMultipleColumnsFromTheSameGroupViaOneQuery()
    {
        // Arrange — two lookup columns share connection/dataset/keys, so PreloadGroup must run once
        // and both columns read from the same cached full record.
        var rows = new List<Dictionary<string, object?>>
        {
            new() { ["Id"] = "A1", ["Name"] = "Device One", ["Region"] = "West" },
        };
        var gateway = CreateGatewayMock(GenericResult<IEnumerable<Dictionary<string, object?>>>.Success(rows));
        var config = CreateConfig(LookupColumn("Name"), LookupColumn("Region"));
        var inputs = new List<IDictionary<string, object?>> { new Dictionary<string, object?> { ["DeviceId"] = "A1" } };

        // Act
        var result = await _sut.TransformBatch(inputs, config, CreateContext(gateway.Object), TestContext.Current.CancellationToken);

        // Assert
        var enriched = new List<IDictionary<string, object?>>(result.Value!);
        enriched[0]["Name"].ShouldBe("Device One");
        enriched[0]["Region"].ShouldBe("West");
        gateway.Verify(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(
            It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task TransformBatchAppliesOutputFieldPrefixWhenConfigured()
    {
        // Arrange
        var rows = new List<Dictionary<string, object?>> { new() { ["Id"] = "A1", ["Name"] = "Device One" } };
        var gateway = CreateGatewayMock(GenericResult<IEnumerable<Dictionary<string, object?>>>.Success(rows));
        var config = CreateConfig(LookupColumn("Name", outputFieldPrefix: "Device_"));
        var inputs = new List<IDictionary<string, object?>> { new Dictionary<string, object?> { ["DeviceId"] = "A1" } };

        // Act
        var result = await _sut.TransformBatch(inputs, config, CreateContext(gateway.Object), TestContext.Current.CancellationToken);

        // Assert
        var enriched = new List<IDictionary<string, object?>>(result.Value!);
        enriched[0]["Device_Name"].ShouldBe("Device One");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task TransformBatchReusesCachedRecordsAcrossCallsWithoutRequeryingDataGateway()
    {
        // Arrange
        var rows = new List<Dictionary<string, object?>> { new() { ["Id"] = "A1", ["Name"] = "Device One" } };
        var gateway = CreateGatewayMock(GenericResult<IEnumerable<Dictionary<string, object?>>>.Success(rows));
        var config = CreateConfig(LookupColumn("Name"));
        var inputs = new List<IDictionary<string, object?>> { new Dictionary<string, object?> { ["DeviceId"] = "A1" } };

        // Act — run the batch twice with the same connection/dataset/keys.
        var first = await _sut.TransformBatch(inputs, config, CreateContext(gateway.Object), TestContext.Current.CancellationToken);
        var second = await _sut.TransformBatch(inputs, config, CreateContext(gateway.Object), TestContext.Current.CancellationToken);

        // Assert
        new List<IDictionary<string, object?>>(first.Value!)[0]["Name"].ShouldBe("Device One");
        new List<IDictionary<string, object?>>(second.Value!)[0]["Name"].ShouldBe("Device One");
        gateway.Verify(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(
            It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Join-type correctness: Inner (FailOnMissing) vs Left on an unmatched key ────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformBatchLeftJoinLeavesFieldUnsetOnUnmatchedKeyWithoutReportingError()
    {
        // Arrange — gateway returns no rows for the requested key.
        var gateway = CreateGatewayMock(GenericResult<IEnumerable<Dictionary<string, object?>>>.Success(new List<Dictionary<string, object?>>()));
        var config = CreateConfig(LookupColumn("Name", joinType: "Left"));
        var inputs = new List<IDictionary<string, object?>> { new Dictionary<string, object?> { ["DeviceId"] = "X9" } };
        var context = CreateContext(gateway.Object);

        // Act
        var result = await _sut.TransformBatch(inputs, config, context, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var enriched = new List<IDictionary<string, object?>>(result.Value!);
        enriched[0].ContainsKey("Name").ShouldBeFalse();
        context.Errors.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformBatchInnerJoinReportsErrorOnUnmatchedKeyWithoutFailingTheBatch()
    {
        // Arrange
        var gateway = CreateGatewayMock(GenericResult<IEnumerable<Dictionary<string, object?>>>.Success(new List<Dictionary<string, object?>>()));
        var config = CreateConfig(LookupColumn("Name", joinType: "Inner"));
        var inputs = new List<IDictionary<string, object?>> { new Dictionary<string, object?> { ["DeviceId"] = "X9" } };
        var context = CreateContext(gateway.Object);

        // Act
        var result = await _sut.TransformBatch(inputs, config, context, TestContext.Current.CancellationToken);

        // Assert — the miss is reported through the context, not a batch failure
        result.IsSuccess.ShouldBeTrue();
        var enriched = new List<IDictionary<string, object?>>(result.Value!);
        enriched[0].ContainsKey("Name").ShouldBeFalse();
        context.Errors.ShouldContain(e => e.Message.Contains("no match found for key 'X9'"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task TransformBatchLeftJoinLeavesFieldUnsetWhenSourceKeyValueIsMissing()
    {
        // Arrange — every record is missing the SourceKeyField entirely.
        var gateway = new Mock<IDataGateway>();
        var config = CreateConfig(LookupColumn("Name", joinType: "Left"));
        var inputs = new List<IDictionary<string, object?>>
        {
            new Dictionary<string, object?>(),
            new Dictionary<string, object?> { ["DeviceId"] = null },
        };

        // Act
        var result = await _sut.TransformBatch(inputs, config, CreateContext(gateway.Object), TestContext.Current.CancellationToken);

        // Assert — the distinct-keys-to-look-up set is empty, so the gateway is never consulted.
        result.IsSuccess.ShouldBeTrue();
        gateway.Verify(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(
            It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── DataGateway failure surfaces ─────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformBatchReportsContextErrorWhenDataGatewayReturnsFailureResult()
    {
        // Arrange
        var gateway = CreateGatewayMock(GenericResult<IEnumerable<Dictionary<string, object?>>>.Failure(
            Fdw.Messages.GenericMessage.Create(Fdw.Messages.MessageSeverity.Error, "query rejected", null, null)));
        var config = CreateConfig(LookupColumn("Name"));
        var inputs = new List<IDictionary<string, object?>> { new Dictionary<string, object?> { ["DeviceId"] = "A1" } };
        var context = CreateContext(gateway.Object);

        // Act
        var result = await _sut.TransformBatch(inputs, config, context, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        context.Errors.ShouldContain(e => e.Message.Contains("query rejected"));
        var enriched = new List<IDictionary<string, object?>>(result.Value!);
        enriched[0].ContainsKey("Name").ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformBatchReportsBatchLookupOperationFailedMessageWhenDataGatewayThrows()
    {
        // Arrange — PerformBatchLookup wraps the gateway call in try/catch and converts a thrown
        // exception into a structured EtlResultCodes.BatchLookupOperationFailed failure; PreloadGroup
        // surfaces that as a context error rather than failing the whole batch.
        var gateway = new Mock<IDataGateway>();
        gateway.Setup(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("connection refused"));
        var config = CreateConfig(LookupColumn("Name"));
        var inputs = new List<IDictionary<string, object?>> { new Dictionary<string, object?> { ["DeviceId"] = "A1" } };
        var context = CreateContext(gateway.Object);

        // Act
        var result = await _sut.TransformBatch(inputs, config, context, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        context.Errors.ShouldContain(e =>
            e.Message.Contains("Batch lookup operation failed") && e.Message.Contains("connection refused"));
    }

    // ── MapSpecToConfiguration: request-spec → typed config dispatch (FDW-556 Part 2.2) ─

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void MapSpecToConfigurationEmitsOneRowPerLookupColumn()
    {
        // Arrange
        var spec = new FakeTransformOperationSpec
        {
            Name = "Lookup1",
            OperationType = "Lookup",
            Lookup = new FakeLookupSpec
            {
                LookupConnectionName = "Conn1",
                LookupDataSet = "Devices",
                LookupKeyField = "Id",
                SourceKeyField = "DeviceId",
                OutputFieldPrefix = "Device_",
                LookupColumns = ["Name", "Region"],
                JoinType = "Left",
            }
        };
        var target = new PipelineTransformConfiguration { Id = Guid.NewGuid(), Name = "Lookup1", OperationType = "Lookup" };

        // Act
        var result = _sut.MapSpecToConfiguration(spec, target, NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        target.Lookups.Count.ShouldBe(2);
        target.Lookups[0].LookupValueField.ShouldBe("Name");
        target.Lookups[1].LookupValueField.ShouldBe("Region");
        target.Lookups[0].LookupConnectionName.ShouldBe("Conn1");
        target.Lookups[0].PipelineTransformId.ShouldBe(target.Id);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void MapSpecToConfigurationFailsLoudWhenLookupSpecIsNull()
    {
        // Arrange
        var spec = new FakeTransformOperationSpec { Name = "Lookup1", OperationType = "Lookup" };
        var target = new PipelineTransformConfiguration { Id = Guid.NewGuid(), Name = "Lookup1", OperationType = "Lookup" };

        // Act
        var result = _sut.MapSpecToConfiguration(spec, target, NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("ETL-11046");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void MapSpecToConfigurationFailsLoudWhenLookupColumnsEmpty()
    {
        // Arrange
        var spec = new FakeTransformOperationSpec
        {
            Name = "Lookup1",
            OperationType = "Lookup",
            Lookup = new FakeLookupSpec
            {
                LookupConnectionName = "Conn1",
                LookupDataSet = "Devices",
                LookupKeyField = "Id",
                SourceKeyField = "DeviceId",
                JoinType = "Left",
            }
        };
        var target = new PipelineTransformConfiguration { Id = Guid.NewGuid(), Name = "Lookup1", OperationType = "Lookup" };

        // Act
        var result = _sut.MapSpecToConfiguration(spec, target, NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("ETL-11046");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void MapSpecToConfigurationFailsLoudWhenJoinTypeUnknown()
    {
        // Arrange
        var spec = new FakeTransformOperationSpec
        {
            Name = "Lookup1",
            OperationType = "Lookup",
            Lookup = new FakeLookupSpec
            {
                LookupConnectionName = "Conn1",
                LookupDataSet = "Devices",
                LookupKeyField = "Id",
                SourceKeyField = "DeviceId",
                LookupColumns = ["Name"],
                JoinType = "FullOuter",
            }
        };
        var target = new PipelineTransformConfiguration { Id = Guid.NewGuid(), Name = "Lookup1", OperationType = "Lookup" };

        // Act
        var result = _sut.MapSpecToConfiguration(spec, target, NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("ETL-11054");
    }
}
