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

    private static TransformContext CreateContext(IDataGateway? dataGateway = null) =>
        new(Guid.NewGuid(), NullLogger.Instance, new Dictionary<string, object?>(), fieldTransformChains: new Dictionary<Guid, IReadOnlyList<IFieldMappingTransformation>>(), dataGateway: dataGateway);

    private static PipelineTransformConfiguration CreateConfig(params PipelineTransformLookupConfiguration[] lookups) =>
        new() { Id = Guid.NewGuid(), Name = "Lookup1", OperationType = "Lookup", Lookups = [.. lookups] };

    private static PipelineTransformLookupConfiguration LookupColumn(
        string lookupValueField,
        string dataSet = "Devices",
        string lookupKeyField = "Id",
        string sourceKeyField = "DeviceId",
        string? outputFieldPrefix = null,
        string joinType = "Left") =>
        new()
        {
            Id = Guid.NewGuid(),
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
                It.IsAny<DataSetTarget>(),
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
            It.IsAny<IDataCommand>(), It.IsAny<DataSetTarget>(), It.IsAny<CancellationToken>()), Times.Never);
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
            It.IsAny<IDataCommand>(), It.IsAny<DataSetTarget>(), It.IsAny<CancellationToken>()), Times.Never);
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
            It.IsAny<IDataCommand>(), It.IsAny<DataSetTarget>(), It.IsAny<CancellationToken>()), Times.Never);
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
            It.IsAny<IDataCommand>(), It.IsAny<DataSetTarget>(), It.IsAny<CancellationToken>()), Times.Once);
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
            It.IsAny<IDataCommand>(), It.IsAny<DataSetTarget>(), It.IsAny<CancellationToken>()), Times.Once);
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
            It.IsAny<IDataCommand>(), It.IsAny<DataSetTarget>(), It.IsAny<CancellationToken>()), Times.Once);
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
    public async Task TransformBatchReusesCachedRecordsAcrossPagesOfOneRunWithoutRequeryingDataGateway()
    {
        // Arrange — one context is one run; a paged run hands it every page in turn.
        var rows = new List<Dictionary<string, object?>> { new() { ["Id"] = "A1", ["Name"] = "Device One" } };
        var gateway = CreateGatewayMock(GenericResult<IEnumerable<Dictionary<string, object?>>>.Success(rows));
        var config = CreateConfig(LookupColumn("Name"));
        var inputs = new List<IDictionary<string, object?>> { new Dictionary<string, object?> { ["DeviceId"] = "A1" } };
        var run = CreateContext(gateway.Object);

        // Act — two pages of the same run with the same connection/dataset/keys.
        var first = await _sut.TransformBatch(inputs, config, run, TestContext.Current.CancellationToken);
        var second = await _sut.TransformBatch(inputs, config, run, TestContext.Current.CancellationToken);

        // Assert
        new List<IDictionary<string, object?>>(first.Value!)[0]["Name"].ShouldBe("Device One");
        new List<IDictionary<string, object?>>(second.Value!)[0]["Name"].ShouldBe("Device One");
        gateway.Verify(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(
            It.IsAny<IDataCommand>(), It.IsAny<DataSetTarget>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformBatchNeverServesAnotherRunsCachedRecords()
    {
        // Arrange — two runs look up the same key against sources that now disagree. A cache shared by
        // both would hand run two the record run one cached.
        var config = CreateConfig(LookupColumn("Name"));
        var inputs = new List<IDictionary<string, object?>> { new Dictionary<string, object?> { ["DeviceId"] = "A1" } };
        var runOneGateway = CreateGatewayMock(GenericResult<IEnumerable<Dictionary<string, object?>>>.Success(
            new List<Dictionary<string, object?>> { new() { ["Id"] = "A1", ["Name"] = "Device One" } }));
        var runTwoGateway = CreateGatewayMock(GenericResult<IEnumerable<Dictionary<string, object?>>>.Success(
            new List<Dictionary<string, object?>> { new() { ["Id"] = "A1", ["Name"] = "Renamed Device" } }));

        // Act
        await _sut.TransformBatch(inputs, config, CreateContext(runOneGateway.Object), TestContext.Current.CancellationToken);
        var second = await _sut.TransformBatch(inputs, config, CreateContext(runTwoGateway.Object), TestContext.Current.CancellationToken);

        // Assert
        new List<IDictionary<string, object?>>(second.Value!)[0]["Name"].ShouldBe("Renamed Device");
        runTwoGateway.Verify(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(
            It.IsAny<IDataCommand>(), It.IsAny<DataSetTarget>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformBatchKeepsARunsCacheWhenAnotherRunFinishesBetweenItsPages()
    {
        // Arrange — FDW-952: run A is mid-run when run B starts and finishes. A finishing run used to empty
        // the one process-wide cache, so run A's next page came back without its lookup column.
        var config = CreateConfig(LookupColumn("Name"));
        var inputs = new List<IDictionary<string, object?>> { new Dictionary<string, object?> { ["DeviceId"] = "A1" } };
        var gatewayA = CreateGatewayMock(GenericResult<IEnumerable<Dictionary<string, object?>>>.Success(
            new List<Dictionary<string, object?>> { new() { ["Id"] = "A1", ["Name"] = "Device One" } }));
        var gatewayB = CreateGatewayMock(GenericResult<IEnumerable<Dictionary<string, object?>>>.Success(
            new List<Dictionary<string, object?>> { new() { ["Id"] = "A1", ["Name"] = "Device One" } }));
        var runA = CreateContext(gatewayA.Object);

        // Act
        await _sut.TransformBatch(inputs, config, runA, TestContext.Current.CancellationToken);
        await _sut.TransformBatch(inputs, config, CreateContext(gatewayB.Object), TestContext.Current.CancellationToken);
        var runAPageTwo = await _sut.TransformBatch(inputs, config, runA, TestContext.Current.CancellationToken);

        // Assert — page two is enriched from run A's own cache, with no second query.
        new List<IDictionary<string, object?>>(runAPageTwo.Value!)[0]["Name"].ShouldBe("Device One");
        gatewayA.Verify(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(
            It.IsAny<IDataCommand>(), It.IsAny<DataSetTarget>(), It.IsAny<CancellationToken>()), Times.Once);
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
            It.IsAny<IDataCommand>(), It.IsAny<DataSetTarget>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── DataGateway failure surfaces ─────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformBatchFailsTheStageWhenDataGatewayReturnsFailureResult()
    {
        // Arrange — FDW-1192: a failed preload used to be a soft context error, the stage succeeded and every
        // row then looked like a missing key.
        var gateway = CreateGatewayMock(GenericResult<IEnumerable<Dictionary<string, object?>>>.Failure(
            Fdw.Messages.GenericMessage.Create(Fdw.Messages.MessageSeverity.Error, "query rejected", null, null)));
        var config = CreateConfig(LookupColumn("Name"));
        var inputs = new List<IDictionary<string, object?>> { new Dictionary<string, object?> { ["DeviceId"] = "A1" } };
        var context = CreateContext(gateway.Object);

        // Act
        var result = await _sut.TransformBatch(inputs, config, context, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("ETL-11059");
        result.Messages[^1].Message.ShouldContain("query rejected");
        result.Messages[^1].Message.ShouldContain("Conn1:Devices");
        context.Errors.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformBatchFailsTheStageWhenDataGatewayThrows()
    {
        // Arrange — PerformBatchLookup wraps the gateway call in try/catch and converts a thrown
        // exception into a structured EtlResultCodes.BatchLookupOperationFailed failure; PreloadGroup
        // carries its text into the ETL-11059 failure that fails the stage (FDW-1192).
        var gateway = new Mock<IDataGateway>();
        gateway.Setup(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataSetTarget>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("connection refused"));
        var config = CreateConfig(LookupColumn("Name"));
        var inputs = new List<IDictionary<string, object?>> { new Dictionary<string, object?> { ["DeviceId"] = "A1" } };
        var context = CreateContext(gateway.Object);

        // Act
        var result = await _sut.TransformBatch(inputs, config, context, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("ETL-11059");
        result.Messages[^1].Message.ShouldContain("Batch lookup operation failed");
        result.Messages[^1].Message.ShouldContain("connection refused");
        context.Errors.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformBatchFailsTheStageWhenTheContextHasNoDataGateway()
    {
        // Arrange — FDW-1192: with no gateway the preload used to be skipped without a word and every key was missing.
        var config = CreateConfig(LookupColumn("Name"));
        var inputs = new List<IDictionary<string, object?>> { new Dictionary<string, object?> { ["DeviceId"] = "A1" } };

        // Act
        var result = await _sut.TransformBatch(inputs, config, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("ETL-11060");
        result.Messages[^1].Message.ShouldContain("Lookup1");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformBatchStopsAtTheFirstFailedPreloadWithoutQueryingTheNextGroup()
    {
        // Arrange — two lookup groups (different data sets); the first group's preload fails.
        var gateway = CreateGatewayMock(GenericResult<IEnumerable<Dictionary<string, object?>>>.Failure(
            Fdw.Messages.GenericMessage.Create(Fdw.Messages.MessageSeverity.Error, "query rejected", null, null)));
        var config = CreateConfig(LookupColumn("Name"), LookupColumn("Region", dataSet: "Regions"));
        var inputs = new List<IDictionary<string, object?>> { new Dictionary<string, object?> { ["DeviceId"] = "A1" } };

        // Act
        var result = await _sut.TransformBatch(inputs, config, CreateContext(gateway.Object), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("ETL-11059");
        gateway.Verify(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(
            It.IsAny<IDataCommand>(), It.IsAny<DataSetTarget>(), It.IsAny<CancellationToken>()), Times.Once);
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
        target.Lookups[0].LookupDataSet.ShouldBe("Devices");
    }

    private static FakeTransformOperationSpec LookupSpec(Action<FakeLookupSpec> shape)
    {
        var lookup = new FakeLookupSpec
        {
            LookupKeyField = "Id",
            SourceKeyField = "DeviceId",
            LookupColumns = ["Name"],
            JoinType = "Left",
        };
        shape(lookup);
        return new FakeTransformOperationSpec { Name = "Lookup1", OperationType = "Lookup", Lookup = lookup };
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ALookupOverADataStoreContainerCarriesItsThreePartsAndNeedsNoConnectionName()
    {
        var spec = LookupSpec(l =>
        {
            l.LookupDataStoreName = "Warehouse";
            l.LookupDataStorePath = "ref/v1";
            l.LookupContainerName = "devices";
        });
        var target = new PipelineTransformConfiguration { Id = Guid.NewGuid(), Name = "Lookup1", OperationType = "Lookup" };

        var result = _sut.MapSpecToConfiguration(spec, target, NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue();
        target.Lookups[0].LookupDataStoreName.ShouldBe("Warehouse");
        target.Lookups[0].LookupDataStorePath.ShouldBe("ref/v1");
        target.Lookups[0].LookupContainerName.ShouldBe("devices");
        target.Lookups[0].LookupDataSet.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ALookupNamingNeitherADataSetNorAContainerIsRefused()
    {
        var spec = LookupSpec(_ => { });
        var target = new PipelineTransformConfiguration { Id = Guid.NewGuid(), Name = "Lookup1", OperationType = "Lookup" };

        _sut.MapSpecToConfiguration(spec, target, NullLogger.Instance).IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ALookupNamingBothADataSetAndAContainerIsRefused()
    {
        var spec = LookupSpec(l =>
        {
            l.LookupDataSet = "Devices";
            l.LookupDataStoreName = "Warehouse";
            l.LookupDataStorePath = "ref/v1";
            l.LookupContainerName = "devices";
        });
        var target = new PipelineTransformConfiguration { Id = Guid.NewGuid(), Name = "Lookup1", OperationType = "Lookup" };

        _sut.MapSpecToConfiguration(spec, target, NullLogger.Instance).IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AHalfNamedDataStoreLookupIsRefused()
    {
        var spec = LookupSpec(l =>
        {
            l.LookupDataStoreName = "Warehouse";
            l.LookupContainerName = "devices";
        });
        var target = new PipelineTransformConfiguration { Id = Guid.NewGuid(), Name = "Lookup1", OperationType = "Lookup" };

        _sut.MapSpecToConfiguration(spec, target, NullLogger.Instance).IsSuccess.ShouldBeFalse();
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
