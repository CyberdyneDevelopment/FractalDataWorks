using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Fdw.Data.MsSql.Tests.RealMsSql;

/// <summary>
/// The durable claim of a pipeline run (FDW-6629) against a real SQL Server: the three OpsDb procedures, loaded from the databases repository into a throwaway database
/// (<see cref="ClaimDatabaseFixture"/>), run through <c>ExecuteProcedureCommand</c> and its translator, the way the engine calls them. A test never passes by skipping when the connection is
/// set: a missing <c>FDW_TEST_DATABASES_PATH</c> or file fails the fixture.
/// </summary>
[Collection(nameof(DataMsSqlTestCollection))]
public sealed class RealMsSqlPipelineRunClaimTests(ClaimDatabaseFixture fixture) : IClassFixture<ClaimDatabaseFixture>
{
    private const string Host = "FdwReference.Etl.Server";
    private static readonly Guid Tenant = Guid.Parse("22222222-0000-0000-0000-000000000002");
    private static readonly Guid OtherTenant = Guid.Parse("22222222-0000-0000-0000-000000000003");
    private static readonly Guid Pipeline = Guid.Parse("44444444-0000-0000-0000-000000000004");

    private static string Keys(params (string Type, string Key, int? Cap)[] keys)
        => JsonSerializer.Serialize(keys.Select(key => new { keyType = key.Type, claimKey = key.Key, cap = key.Cap }));

    private static string Write(string key) => Keys(("WriteKey", key, null));

    // Why the holds too: the claim database is shared by every test of the class, and a hold test ends with its hold standing on a key other tests state, so a claim on that key would be Waiting for
    // whichever test ran after it.
    private async Task Reset()
    {
        fixture.RequireDatabase();
        await fixture.Rows("DELETE FROM etl.PipelineRunClaim");
        await fixture.Rows("DELETE FROM etl.PipelineKeyHold");
    }

    private Task<List<List<Dictionary<string, object?>>>> Claim(
        Guid execution, string keys, Guid? tenant = null, string host = Host, string? slot = null, int leaseSeconds = 60, Guid? pipeline = null)
        => fixture.Execute(
            "ClaimPipelineRun",
            ("TenantId", tenant ?? Tenant), ("ExecutionId", execution), ("PipelineId", pipeline ?? Pipeline), ("Keys", keys),
            ("ExecutingHost", host), ("ExecutingSlot", slot), ("LeaseSeconds", leaseSeconds), ("LockTimeoutMilliseconds", 30000));

    private Task<List<List<Dictionary<string, object?>>>> Release(Guid execution)
        => fixture.Execute("ReleasePipelineRunClaims", ("ExecutionId", execution), ("LockTimeoutMilliseconds", 30000));

    private Task<List<List<Dictionary<string, object?>>>> Renew(string executionIds, string host = Host, string? slot = null)
        => fixture.Execute(
            "RenewPipelineRunClaims",
            ("ExecutionIds", executionIds), ("ExecutingHost", host), ("ExecutingSlot", slot), ("LeaseSeconds", 60), ("LockTimeoutMilliseconds", 30000));

    private static string StateOf(List<List<Dictionary<string, object?>>> answer) => (string)answer[0][0]["State"]!;

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ThirtyTwoClaimantsOnOneKeyLeaveExactlyOneClaimedAndThirtyOneWaitingInTheOrderTheyAskedAndEachReleasePromotesTheNext()
    {
        await Reset();
        var runs = Enumerable.Range(0, 32).Select(_ => Guid.NewGuid()).ToList();
        var gate = new TaskCompletionSource();
        var calls = runs.Select(async run =>
        {
            await gate.Task;
            return await Claim(run, Write("conn/path/orders"));
        }).ToList();

        gate.SetResult();
        var answers = await Task.WhenAll(calls);

        answers.Count(answer => StateOf(answer) == "Claimed").ShouldBe(1);
        var rows = await fixture.Rows("SELECT ExecutionId, State, RowId FROM etl.PipelineRunClaim ORDER BY RowId");
        rows.Count.ShouldBe(32);
        rows.Count(row => (string)row["State"]! == "Claimed").ShouldBe(1);
        rows.Count(row => (string)row["State"]! == "Waiting").ShouldBe(31);
        ((string)rows[0]["State"]!).ShouldBe("Claimed");

        var order = rows.Select(row => (Guid)row["ExecutionId"]!).ToList();
        for (var i = 0; i < 31; i++)
        {
            var released = await Release(order[i]);
            released[0].Count.ShouldBe(1);
            ((Guid)released[0][0]["ExecutionId"]!).ShouldBe(order[i + 1]);
        }

        (await Release(order[31]))[0].ShouldBeEmpty();
        ((int)(await fixture.Rows("SELECT COUNT(*) AS N FROM etl.PipelineRunClaim"))[0]["N"]!).ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TwoTenantsWithTheSameStatedTextDoNotConflictAndOneTenantsTwoRunsOnItDo()
    {
        await Reset();
        var text = "order-17";

        StateOf(await Claim(Guid.NewGuid(), Keys(("StatedKey", $"{Tenant:D}:{text}", null)), Tenant)).ShouldBe("Claimed");
        StateOf(await Claim(Guid.NewGuid(), Keys(("StatedKey", $"{OtherTenant:D}:{text}", null)), OtherTenant)).ShouldBe("Claimed");
        StateOf(await Claim(Guid.NewGuid(), Keys(("StatedKey", $"{Tenant:D}:{text}", null)), Tenant)).ShouldBe("Waiting");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AWriteKeyIsComparedWithoutRegardToCase()
    {
        await Reset();

        StateOf(await Claim(Guid.NewGuid(), Write("Conn/Path/Orders"))).ShouldBe("Claimed");
        StateOf(await Claim(Guid.NewGuid(), Write("conn/path/orders"))).ShouldBe("Waiting");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARunWithSeveralKeysIsClaimedOnlyWhenAllAreFreeAndAWaitingRunKeepsItsPlaceOnEveryKey()
    {
        await Reset();
        var holder = Guid.NewGuid();
        var needsTwo = Guid.NewGuid();
        var behind = Guid.NewGuid();

        StateOf(await Claim(holder, Keys(("WriteKey", "k2", null)))).ShouldBe("Claimed");
        StateOf(await Claim(needsTwo, Keys(("WriteKey", "k2", null), ("WriteKey", "k3", null)))).ShouldBe("Waiting");

        // k3 is free, but the run that asked first for it is waiting on k2, so the next one for k3 waits behind it.
        StateOf(await Claim(behind, Keys(("WriteKey", "k3", null)))).ShouldBe("Waiting");

        var released = await Release(holder);
        released[0].Select(row => (Guid)row["ExecutionId"]!).ToList().ShouldBe([needsTwo]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task APipelinesCapLimitsHowManyOfItsRunsAreClaimedAndAReleasePromotesTheNext()
    {
        await Reset();
        var cap = Keys(("PipelineCap", Pipeline.ToString("D"), 2));
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var third = Guid.NewGuid();

        StateOf(await Claim(first, cap)).ShouldBe("Claimed");
        StateOf(await Claim(second, cap)).ShouldBe("Claimed");
        StateOf(await Claim(third, cap)).ShouldBe("Waiting");

        var released = await Release(first);
        released[0].Select(row => (Guid)row["ExecutionId"]!).ToList().ShouldBe([third]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ALapsedLeaseIsRemovedAndReturnedByTheNextCallAndTheNextClaimantTakesTheKey()
    {
        await Reset();
        var holder = Guid.NewGuid();
        var next = Guid.NewGuid();

        StateOf(await Claim(holder, Write("conn/path/leased"), leaseSeconds: 1)).ShouldBe("Claimed");
        await Task.Delay(TimeSpan.FromSeconds(2.5), TestContext.Current.CancellationToken);

        var answer = await Claim(next, Write("conn/path/leased"));

        StateOf(answer).ShouldBe("Claimed");
        answer[1].Count.ShouldBe(1);
        ((Guid)answer[1][0]["ExecutionId"]!).ShouldBe(holder);
        ((string)answer[1][0]["State"]!).ShouldBe("Claimed");

        var renewed = await Renew(JsonSerializer.Serialize(new[] { holder }));
        ((int)renewed[0][0]["Renewed"]!).ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARenewalKeepsTheLeaseOfTheRunsThisHostAndSlotHoldAndNoOtherCopysRuns()
    {
        await Reset();
        var holderA = Guid.NewGuid();

        StateOf(await Claim(holderA, Write("conn/path/shared"), slot: "a")).ShouldBe("Claimed");

        var byCopyB = await Renew(JsonSerializer.Serialize(new[] { holderA }), slot: "b");
        var byCopyA = await Renew(JsonSerializer.Serialize(new[] { holderA }), slot: "a");

        ((int)byCopyB[0][0]["Renewed"]!).ShouldBe(0);
        ((int)byCopyA[0][0]["Renewed"]!).ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AClaimByOneCopyOfTheHostBlocksTheOtherCopyBecauseTheyShareTheTable()
    {
        await Reset();

        StateOf(await Claim(Guid.NewGuid(), Write("conn/path/ab"), slot: "a")).ShouldBe("Claimed");
        StateOf(await Claim(Guid.NewGuid(), Write("conn/path/ab"), slot: "b")).ShouldBe("Waiting");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AnArgumentTheProcedureCannotUseFailsLoudlyAndNothingIsWritten()
    {
        await Reset();

        var lease = await Should.ThrowAsync<SqlException>(() => Claim(Guid.NewGuid(), Write("conn/path/bad"), leaseSeconds: 0));
        var keys = await Should.ThrowAsync<SqlException>(() => Claim(Guid.NewGuid(), "not json"));

        lease.Number.ShouldBe(50021);
        keys.Number.ShouldBe(50022);
        ((int)(await fixture.Rows("SELECT COUNT(*) AS N FROM etl.PipelineRunClaim"))[0]["N"]!).ShouldBe(0);
    }

    private static string Exclusive() => Keys(("Exclusive", "*", null));

    private static List<Guid> Promoted(List<List<Dictionary<string, object?>>> released) => [.. released[0].Select(row => (Guid)row["ExecutionId"]!)];

    // FDW-6939: a run whose writes cannot be told claims one Exclusive row; it conflicts with every other run's row of every key type and every tenant, and every row conflicts with it.
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AnExclusiveRunWaitsForAClaimedWriteRunOfAnotherTenantAndIsPromotedWhenItIsReleased()
    {
        await Reset();
        var writer = Guid.NewGuid();
        var exclusive = Guid.NewGuid();

        StateOf(await Claim(writer, Write("conn/etl/orders"), Tenant)).ShouldBe("Claimed");
        var answer = await Claim(exclusive, Exclusive(), OtherTenant);

        StateOf(answer).ShouldBe("Waiting");
        ((int)answer[0][0]["Ahead"]!).ShouldBe(1);
        Promoted(await Release(writer)).ShouldBe([exclusive]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AWriteRunWaitsForAClaimedExclusiveRunWhateverItsTableAndTenantAndIsPromotedWhenItIsReleased()
    {
        await Reset();
        var exclusive = Guid.NewGuid();
        var writer = Guid.NewGuid();

        StateOf(await Claim(exclusive, Exclusive(), Tenant)).ShouldBe("Claimed");
        StateOf(await Claim(writer, Write("conn/etl/another-table"), OtherTenant)).ShouldBe("Waiting");

        Promoted(await Release(exclusive)).ShouldBe([writer]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARunThatAsksAfterAnExclusiveOneWaitsBehindItEvenOnAnotherTableAndTheRunsAreClaimedInTheOrderTheyAsked()
    {
        await Reset();
        var first = Guid.NewGuid();
        var exclusive = Guid.NewGuid();
        var later = Guid.NewGuid();

        StateOf(await Claim(first, Write("conn/etl/orders"))).ShouldBe("Claimed");
        StateOf(await Claim(exclusive, Exclusive())).ShouldBe("Waiting");
        StateOf(await Claim(later, Write("conn/etl/lines"))).ShouldBe("Waiting");

        Promoted(await Release(first)).ShouldBe([exclusive]);
        Promoted(await Release(exclusive)).ShouldBe([later]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TwoExclusiveRunsTakeTurnsInTheOrderTheyAsked()
    {
        await Reset();
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();

        StateOf(await Claim(first, Exclusive(), Tenant)).ShouldBe("Claimed");
        StateOf(await Claim(second, Exclusive(), OtherTenant)).ShouldBe("Waiting");

        Promoted(await Release(first)).ShouldBe([second]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AnExclusiveRunThatAlsoStatesAKeyHoldsBothAndReleasesBoth()
    {
        await Reset();
        var run = Guid.NewGuid();

        StateOf(await Claim(run, Keys(("Exclusive", "*", null), ("StatedKey", $"{Tenant:D}:chat-9", null)))).ShouldBe("Claimed");
        ((int)(await fixture.Rows($"SELECT COUNT(*) AS N FROM etl.PipelineRunClaim WHERE ExecutionId = '{run}'"))[0]["N"]!).ShouldBe(2);

        _ = await Release(run);
        ((int)(await fixture.Rows("SELECT COUNT(*) AS N FROM etl.PipelineRunClaim"))[0]["N"]!).ShouldBe(0);
    }

    // FDW-6939: a run that waits for a pipeline it started hands its claim back, and takes it again behind the child. An Exclusive parent and an Exclusive child would otherwise wait on each other
    // forever: the parent holds the claim the child needs and waits for the child to end.
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AnExclusiveParentThatYieldsItsClaimLetsItsExclusiveChildRunAndEndAndTakesTheClaimAgain()
    {
        await Reset();
        var parent = Guid.NewGuid();
        var child = Guid.NewGuid();

        StateOf(await Claim(parent, Exclusive())).ShouldBe("Claimed");
        StateOf(await Claim(child, Exclusive())).ShouldBe("Waiting");

        // The parent waits for the child: it releases its claim, which promotes the child.
        Promoted(await Release(parent)).ShouldBe([child]);
        StateOf(await Claim(child, Exclusive())).ShouldBe("Claimed");

        // The child ends; the parent asks again and, with nothing ahead of it, holds the claim.
        _ = await Release(child);
        StateOf(await Claim(parent, Exclusive())).ShouldBe("Claimed");
    }

    // FDW-6939: the hold on a stated key. A held key stops the WAITING runs on it, from starting, until the hold is released; a run already Claimed is not stopped.
    private Task<List<List<Dictionary<string, object?>>>> Place(Guid tenant, string claimKey, Guid heldBy, string reason)
        => fixture.Execute("PlaceKeyHold", ("TenantId", tenant), ("ClaimKey", claimKey), ("HeldBy", heldBy), ("Reason", reason), ("LockTimeoutMilliseconds", 30000));

    private Task<List<List<Dictionary<string, object?>>>> ReleaseHold(Guid tenant, string claimKey)
        => fixture.Execute("ReleaseKeyHold", ("TenantId", tenant), ("ClaimKey", claimKey), ("LockTimeoutMilliseconds", 30000));

    private static string Stated(Guid tenant, string key) => Keys(("StatedKey", $"{tenant:D}:{key}", null));

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AHeldKeyStopsAWaitingRunOnItAndNotARunOnAnotherKeyOrTenantAndReleasingTheHoldPromotesIt()
    {
        await Reset();
        var person = Guid.NewGuid();
        var key = $"{Tenant:D}:chat-9";
        var held = Guid.NewGuid();

        ((bool)(await Place(Tenant, key, person, "waiting for the person to answer"))[0][0]["Placed"]!).ShouldBeTrue();

        StateOf(await Claim(held, Stated(Tenant, "chat-9"), Tenant)).ShouldBe("Waiting");
        StateOf(await Claim(Guid.NewGuid(), Stated(Tenant, "chat-10"), Tenant)).ShouldBe("Claimed");
        StateOf(await Claim(Guid.NewGuid(), Stated(OtherTenant, "chat-9"), OtherTenant)).ShouldBe("Claimed");

        var released = await ReleaseHold(Tenant, key);
        ((bool)released[0][0]["Released"]!).ShouldBeTrue();
        ((Guid)released[0][0]["HeldBy"]!).ShouldBe(person);
        released[1].Select(row => (Guid)row["ExecutionId"]!).ShouldBe([held]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task APlaceOnAKeyThatIsHeldIsRefusedAnswerWhoHoldsItAndTheHoldStands()
    {
        await Reset();
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var key = $"{Tenant:D}:chat-9";

        ((bool)(await Place(Tenant, key, first, "first reason"))[0][0]["Placed"]!).ShouldBeTrue();
        var again = await Place(Tenant, key, second, "second reason");

        ((bool)again[0][0]["Placed"]!).ShouldBeFalse();
        ((Guid)again[0][0]["HeldBy"]!).ShouldBe(first);
        ((string)again[0][0]["Reason"]!).ShouldBe("first reason");
        ((int)(await fixture.Rows("SELECT COUNT(*) AS N FROM etl.PipelineKeyHold"))[0]["N"]!).ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARunAlreadyClaimedOnAKeyIsNotStoppedByAHoldPlacedAfterwardsAndTheNextRunWaitsForTheHoldAndTheRun()
    {
        await Reset();
        var running = Guid.NewGuid();
        var next = Guid.NewGuid();
        var key = $"{Tenant:D}:chat-9";

        StateOf(await Claim(running, Stated(Tenant, "chat-9"), Tenant)).ShouldBe("Claimed");
        _ = await Place(Tenant, key, Guid.NewGuid(), "hold it");
        StateOf(await Claim(next, Stated(Tenant, "chat-9"), Tenant)).ShouldBe("Waiting");

        // The running run ends: the next one is still stopped by the hold, which only a release lifts.
        Promoted(await Release(running)).ShouldBeEmpty();
        (await ReleaseHold(Tenant, key))[1].Select(row => (Guid)row["ExecutionId"]!).ShouldBe([next]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ReleasingAKeyThatIsNotHeldSaysSoAndPromotesWhatIsFree()
    {
        await Reset();

        var released = await ReleaseHold(Tenant, $"{Tenant:D}:never-held");

        ((bool)released[0][0]["Released"]!).ShouldBeFalse();
        released[1].ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AHoldThatStatesNoReasonOrNoKeyFailsLoudly()
    {
        await Reset();

        await Should.ThrowAsync<Exception>(async () => await Place(Tenant, $"{Tenant:D}:chat-9", Guid.NewGuid(), "  "));
        await Should.ThrowAsync<Exception>(async () => await Place(Tenant, " ", Guid.NewGuid(), "a reason"));
        ((int)(await fixture.Rows("SELECT COUNT(*) AS N FROM etl.PipelineKeyHold"))[0]["N"]!).ShouldBe(0);
    }
}
