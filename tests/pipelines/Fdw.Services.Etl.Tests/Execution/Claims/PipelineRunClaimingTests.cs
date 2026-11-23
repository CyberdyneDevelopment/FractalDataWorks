using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Execution.Claims;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Settings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Execution.Claims;

/// <summary>
/// FDW-6939 (slice 3 of FDW-6141): what the run code does before a run starts, for a queued run and a run carried out at once alike. It reads the pipeline in the run's own scope, works out what
/// the run claims (the tables it writes, the one Exclusive key of a run whose writes cannot be told, the cap, the stated key), states the host and slot that claim it, and holds the claim
/// through <see cref="PipelineRunClaimHold"/>. A claim that cannot be made refuses the run.
/// </summary>
public sealed class PipelineRunClaimingTests
{
    private static readonly Guid Tenant = Guid.Parse("22222222-0000-0000-0000-000000000002");
    private static readonly Guid Execution = Guid.Parse("33333333-0000-0000-0000-000000000003");
    private static readonly Guid Pipeline = Guid.Parse("44444444-0000-0000-0000-000000000004");

    private sealed class RecordingClaims : IPipelineRunClaimService
    {
        public List<PipelineRunClaimRequest> Claimed { get; } = [];

        public Task<IGenericResult<PipelineRunClaimOutcome>> Claim(PipelineRunClaimRequest request, CancellationToken cancellationToken = default)
        {
            Claimed.Add(request);
            return Task.FromResult<IGenericResult<PipelineRunClaimOutcome>>(
                GenericResult<PipelineRunClaimOutcome>.Success(new PipelineRunClaimOutcome(request.ExecutionId, PipelineRunClaimStates.ByName("Claimed"), 0, [])));
        }

        public Task<IGenericResult<PipelineRunRenewed>> Renew(IReadOnlyList<Guid> executionIds, string executingHost, string? executingSlot, CancellationToken cancellationToken = default)
            => Task.FromResult<IGenericResult<PipelineRunRenewed>>(GenericResult<PipelineRunRenewed>.Success(new PipelineRunRenewed([new PipelineRunRenewal(Execution, true)], [])));

        public Task<IGenericResult<PipelineRunReleased>> Release(Guid executionId, CancellationToken cancellationToken = default)
            => Task.FromResult<IGenericResult<PipelineRunReleased>>(GenericResult<PipelineRunReleased>.Success(new PipelineRunReleased([], [])));
    }

    private readonly Mock<IPipelineConfigurationProvider> _pipelines = new();
    private readonly Mock<IEffectiveSettingsProvider> _settings = new();
    private readonly Mock<IHostEnvironment> _environment = new();
    private readonly Mock<ISlotRole> _slot = new();
    private readonly RecordingClaims _claims = new();

    public PipelineRunClaimingTests()
    {
        _settings.Setup(s => s.GetEffectiveValue<int>(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string?>())).Returns(4);
        _environment.SetupGet(e => e.ApplicationName).Returns("Reference.Etl.Server");
        _slot.SetupGet(s => s.SlotName).Returns("b");
    }

    private PipelineRunClaiming Sut() => new(
        _pipelines.Object, _settings.Object, _claims, new PipelineRunClaimSettings(60, 20, 5, 30000), _environment.Object, _slot.Object, TimeProvider.System,
        logger: NullLogger<PipelineRunClaiming>.Instance);

    private static PipelineExecutionRequest Request(IReadOnlyDictionary<string, object?>? parameters = null, int? testRows = null) => new()
    {
        ExecutionId = Execution,
        PipelineId = Pipeline,
        PipelineName = "Chat",
        TriggerSource = "Api",
        RunRoles = ["etl-runner"],
        TenantId = Tenant,
        Parameters = parameters,
        TestRowsPerSource = testRows,
    };

    private void PipelineIs(IPipelineImplementationConfiguration? configuration) =>
        _pipelines.Setup(p => p.Get(Pipeline, ConfigurationLevels.WholeAggregate, It.Is<ConfigurationReadOptions>(o => !o.FillsMembers), It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuration is null
                ? GenericResult<IPipelineImplementationConfiguration>.Failure(new GenericMessage("not found"))
                : GenericResult<IPipelineImplementationConfiguration>.Success(configuration));

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task APipelineThatCannotBeReadClaimsTheOneExclusiveKeyAndTheClaimStatesTheHostAndSlot()
    {
        PipelineIs(null);

        var acquired = await Sut().Acquire(Request(), TestContext.Current.CancellationToken);
        await acquired.Value.ShouldNotBeNull().DisposeAsync();

        acquired.IsSuccess.ShouldBeTrue(acquired.CurrentMessage);
        var claimed = _claims.Claimed.ShouldHaveSingleItem();
        claimed.Keys.ShouldHaveSingleItem().Type.Name.ShouldBe("Exclusive");
        claimed.ExecutingHost.ShouldBe("Reference.Etl.Server");
        claimed.ExecutingSlot.ShouldBe("b");
        claimed.TenantId.ShouldBe(Tenant);
        claimed.ExecutionId.ShouldBe(Execution);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AGraphPipelineClaimsItsCapAndTheKeyItsTemplateStatesOverTheRunsParameters()
    {
        PipelineIs(new GraphPipelineConfiguration { Name = "Chat", MaxParallelRuns = 3, OverflowBehavior = "Refuse", ClaimKeyTemplate = "{{conversation}}" });
        var parameters = new Dictionary<string, object?> { ["conversation"] = "c-9" };

        var acquired = await Sut().Acquire(Request(parameters, testRows: 10), TestContext.Current.CancellationToken);
        await acquired.Value.ShouldNotBeNull().DisposeAsync();

        acquired.IsSuccess.ShouldBeTrue(acquired.CurrentMessage);
        var keys = _claims.Claimed.ShouldHaveSingleItem().Keys;
        keys.Select(key => key.Type.Name).OrderBy(name => name, StringComparer.Ordinal).ShouldBe(["PipelineCap", "StatedKey"]);
        keys.Single(key => key.Type.Name == "StatedKey").ClaimKey.ShouldBe($"{Tenant:D}:c-9");
        keys.Single(key => key.Type.Name == "PipelineCap").Cap.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AKeyWhoseParameterTheRunDidNotStateRefusesTheRunBeforeAnythingIsClaimed()
    {
        PipelineIs(new GraphPipelineConfiguration { Name = "Chat", MaxParallelRuns = 3, OverflowBehavior = "Refuse", ClaimKeyTemplate = "{{conversation}}" });

        var acquired = await Sut().Acquire(Request(testRows: 10), TestContext.Current.CancellationToken);

        acquired.IsFailure.ShouldBeTrue();
        acquired.Messages.Select(message => message.Code).ShouldContain("PIPELINERUNCLAIM-60872");
        _claims.Claimed.ShouldBeEmpty();
    }
}
