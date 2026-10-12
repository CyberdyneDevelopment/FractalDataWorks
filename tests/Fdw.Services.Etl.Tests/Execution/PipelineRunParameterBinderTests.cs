using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Etl.Pipelines;
using Fdw.Services.Etl.Pipelines.Loads;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// Tests for <see cref="PipelineRunParameterBinder"/> (FDW-1109): what a run is given when it starts: the typed values it
/// was supplied and, for a run with a tenant, the stored watermark as <c>LastSuccessfulRun</c>.
/// </summary>
public sealed class PipelineRunParameterBinderTests
{
    private static readonly Guid Tenant = Guid.NewGuid();
    private static readonly DateTimeOffset Stored = new(2026, 2, 3, 4, 5, 6, TimeSpan.Zero);

    private readonly Mock<IDataGateway> _gateway = new();
    private int _reads;

    private static BatchCopyPipelineConfiguration Pipeline(params PipelineRunParameterConfiguration[] declared) =>
        new() { Id = Guid.NewGuid(), Name = "copy", RunParameters = declared.ToList() };

    private static PipelineRunParameterConfiguration Declared(string name, string type, bool required = false) =>
        new() { Name = name, ParameterType = type, IsRequired = required };

    private void StoreHolds(params EtlPipelineWatermarkRecord[] rows) =>
        _gateway
            .Setup(g => g.Execute<IEnumerable<EtlPipelineWatermarkRecord>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                _reads++;
                return Task.FromResult<IGenericResult<IEnumerable<EtlPipelineWatermarkRecord>>>(
                    GenericResult<IEnumerable<EtlPipelineWatermarkRecord>>.Success(rows));
            });

    private Task<IGenericResult<PipelineRunParameterBinder>> Bind(BatchCopyPipelineConfiguration pipeline, PipelineExecutionOptions options) =>
        PipelineRunParameterBinder.Bind(_gateway.Object, "Ops", pipeline, options, NullLogger.Instance, TestContext.Current.CancellationToken);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARunWithNoTenantReadsNoWatermarkAndKeepsOnlyTheSuppliedValues()
    {
        // Arrange
        var options = PipelineExecutionOptions.Production with { Parameters = new Dictionary<string, object?> { ["Limit"] = 5L } };

        // Act
        var result = await Bind(Pipeline(Declared("Limit", "Int64")), options);

        // Assert
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.Values["Limit"].ShouldBe(5L);
        result.Value.Values.ContainsKey(PipelineRunParameterValues.LastSuccessfulRun).ShouldBeFalse();
        result.Value.Watermark.ShouldBeNull();
        _reads.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARunWithATenantOffersTheStoredWatermarkAsLastSuccessfulRun()
    {
        // Arrange
        var row = new EtlPipelineWatermarkRecord { Id = Guid.NewGuid(), LastSuccessfulRun = Stored };
        StoreHolds(row);

        // Act
        var result = await Bind(Pipeline(), PipelineExecutionOptions.Production with { TenantId = Tenant });

        // Assert
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.Values[PipelineRunParameterValues.LastSuccessfulRun].ShouldBe(Stored);
        result.Value.Watermark.ShouldBeSameAs(row);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AFirstRunHasNoWatermarkAndNoneIsInvented()
    {
        // Arrange
        StoreHolds();

        // Act
        var result = await Bind(Pipeline(), PipelineExecutionOptions.Production with { TenantId = Tenant });

        // Assert
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.Values.ContainsKey(PipelineRunParameterValues.LastSuccessfulRun).ShouldBeFalse();
        result.Value.Watermark.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ASuppliedLastSuccessfulRunOverridesTheStoredOne()
    {
        // Arrange
        var overridden = new DateTimeOffset(2025, 12, 31, 0, 0, 0, TimeSpan.Zero);
        StoreHolds(new EtlPipelineWatermarkRecord { Id = Guid.NewGuid(), LastSuccessfulRun = Stored });
        var options = PipelineExecutionOptions.Production with
        {
            TenantId = Tenant,
            Parameters = new Dictionary<string, object?> { [PipelineRunParameterValues.LastSuccessfulRun] = overridden },
        };

        // Act
        var result = await Bind(Pipeline(), options);

        // Assert
        result.Value!.Values[PipelineRunParameterValues.LastSuccessfulRun].ShouldBe(overridden);
        result.Value.Watermark.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARunSuppliedAnUndeclaredParameterIsRefusedBeforeAnythingIsRead()
    {
        // Arrange
        var options = PipelineExecutionOptions.Production with
        {
            TenantId = Tenant,
            Parameters = new Dictionary<string, object?> { ["Other"] = 1L },
        };

        // Act
        var result = await Bind(Pipeline(Declared("Limit", "Int64")), options);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("PIPELINERUN-20201");
        _reads.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARunMissingARequiredParameterIsRefused()
    {
        // Act
        var result = await Bind(Pipeline(Declared("Region", "String", required: true)), PipelineExecutionOptions.Production);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("PIPELINERUN-20203");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AFailedWatermarkReadFailsTheRun()
    {
        // Arrange
        _gateway
            .Setup(g => g.Execute<IEnumerable<EtlPipelineWatermarkRecord>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<EtlPipelineWatermarkRecord>>.Failure(new Fdw.Messages.GenericMessage("store down")));

        // Act
        var result = await Bind(Pipeline(), PipelineExecutionOptions.Production with { TenantId = Tenant });

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("PIPELINERUN-20232");
    }
}
