using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Etl.Execution.Claims;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Execution.Claims;

/// <summary>
/// FDW-6939 (slice 3 of FDW-6141): what a run claims in the operational database is what the scheduler works out for it (<see cref="PipelineRunClaim"/>) and the key its
/// pipeline states, mapped onto the keys the claim procedures compare: the tables it writes, one Exclusive key for a run whose writes cannot be told, the cap of its pipeline and
/// the stated key, which always begins with the tenant's id. A run with no tenant cannot be claimed.
/// </summary>
public sealed class PipelineRunClaimRequestsTests
{
    private static readonly Guid Tenant = Guid.Parse("22222222-0000-0000-0000-000000000002");
    private static readonly Guid Execution = Guid.Parse("33333333-0000-0000-0000-000000000003");
    private static readonly Guid Pipeline = Guid.Parse("44444444-0000-0000-0000-000000000004");
    private static readonly PipelineRunLimitValues Limits = new(PerServer: 4, PerTenant: 2);
    private static readonly PipelineRunIdentity Run = new(Execution, Pipeline, "Chat", Tenant);

    private static PipelineExecutionRequest Request(Guid? tenant = null, IReadOnlyDictionary<string, object?>? parameters = null) => new()
    {
        ExecutionId = Execution,
        PipelineId = Pipeline,
        PipelineName = "Chat",
        TriggerSource = "Api",
        RunRoles = ["etl-runner"],
        TenantId = tenant ?? Tenant,
        Parameters = parameters,
    };

    private static IGenericResult<PipelineRunClaimRequest> Of(PipelineExecutionRequest request, PipelineRunClaim claim, string? template = null) =>
        PipelineRunClaimRequests.Of(request, claim, template, "Reference.Api", "b", NullLogger.Instance);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TheTablesARunWritesAreItsWriteKeysAsTheSchedulerNamesThem()
    {
        var claim = PipelineRunClaim.Writing(Run, ["c1/etl/Orders", "c1/etl/Lines"], Limits, cap: null);

        var made = Of(Request(), claim);

        made.IsSuccess.ShouldBeTrue(made.CurrentMessage);
        var keys = made.Value.ShouldNotBeNull().Keys;
        keys.Select(key => key.Type.Name).Distinct().ShouldBe(["WriteKey"]);
        keys.Select(key => key.ClaimKey).OrderBy(text => text, StringComparer.Ordinal).ShouldBe(["c1/etl/Lines", "c1/etl/Orders"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TheRequestStatesTheRunItsPipelineTheTenantAndTheHostAndSlotThatClaimIt()
    {
        var made = Of(Request(), PipelineRunClaim.Writing(Run, ["c1/etl/Orders"], Limits, cap: null)).Value.ShouldNotBeNull();

        made.ExecutionId.ShouldBe(Execution);
        made.PipelineId.ShouldBe(Pipeline);
        made.TenantId.ShouldBe(Tenant);
        made.ExecutingHost.ShouldBe("Reference.Api");
        made.ExecutingSlot.ShouldBe("b");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ARunWhoseWritesCannotBeToldClaimsOneExclusiveKey()
    {
        var made = Of(Request(), PipelineRunClaim.RunningAlone(Run, Limits, cap: null));

        made.IsSuccess.ShouldBeTrue(made.CurrentMessage);
        made.Value.ShouldNotBeNull().Keys.ShouldHaveSingleItem().Type.Name.ShouldBe("Exclusive");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TheCapOfThePipelineIsAPipelineCapKeyNamingThePipelineWithItsLimit()
    {
        var claim = PipelineRunClaim.Writing(Run, [], Limits, new PipelineRunCap($"pipeline/{Pipeline}", Limit: 3, RefusesWhenFull: false));

        var key = Of(Request(), claim).Value.ShouldNotBeNull().Keys.ShouldHaveSingleItem();

        key.Type.Name.ShouldBe("PipelineCap");
        key.ClaimKey.ShouldBe(Pipeline.ToString("D"));
        key.Cap.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AStatedKeyIsTheTemplateExpandedOverTheRunsParametersAndBeginsWithTheTenant()
    {
        var parameters = new Dictionary<string, object?> { ["conversation"] = "c-9" };
        var claim = PipelineRunClaim.Writing(Run, [], Limits, cap: null);

        var key = Of(Request(parameters: parameters), claim, "{{conversation}}").Value.ShouldNotBeNull().Keys.ShouldHaveSingleItem();

        key.Type.Name.ShouldBe("StatedKey");
        key.ClaimKey.ShouldBe($"{Tenant:D}:c-9");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ARunThatStatesAKeyAndWritesTablesClaimsBothAndAnExclusiveRunClaimsItsStatedKeyToo()
    {
        var parameters = new Dictionary<string, object?> { ["conversation"] = "c-9" };

        var writing = Of(Request(parameters: parameters), PipelineRunClaim.Writing(Run, ["c1/etl/Orders"], Limits, cap: null), "{{conversation}}").Value.ShouldNotBeNull();
        var alone = Of(Request(parameters: parameters), PipelineRunClaim.RunningAlone(Run, Limits, cap: null), "{{conversation}}").Value.ShouldNotBeNull();

        writing.Keys.Select(key => key.Type.Name).OrderBy(name => name, StringComparer.Ordinal).ShouldBe(["StatedKey", "WriteKey"]);
        alone.Keys.Select(key => key.Type.Name).OrderBy(name => name, StringComparer.Ordinal).ShouldBe(["Exclusive", "StatedKey"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ARunThatWritesNothingStatesNoKeyAndHasNoCapClaimsNothing()
    {
        var made = Of(Request(), PipelineRunClaim.Writing(Run, [], Limits, cap: null));

        made.IsSuccess.ShouldBeTrue(made.CurrentMessage);
        made.Value.ShouldNotBeNull().Keys.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AStatedKeyWhoseParameterHasNoValueRefusesTheClaimWithTheTemplatesCode()
    {
        var made = Of(Request(), PipelineRunClaim.Writing(Run, [], Limits, cap: null), "{{conversation}}");

        made.IsFailure.ShouldBeTrue();
        made.Messages.Select(message => message.Code).ShouldContain("PIPELINERUNCLAIM-60872");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ARunWithNoTenantCannotBeClaimedAndIsRefusedWithItsCode()
    {
        var request = new PipelineExecutionRequest
        {
            ExecutionId = Execution, PipelineId = Pipeline, PipelineName = "Chat", TriggerSource = "Api", RunRoles = ["etl-runner"],
        };

        var made = Of(request, PipelineRunClaim.Writing(Run, ["c1/etl/Orders"], Limits, cap: null));

        made.IsFailure.ShouldBeTrue();
        made.Messages.Select(message => message.Code).ShouldContain("PIPELINERUNCLAIM-60871");
    }
}
