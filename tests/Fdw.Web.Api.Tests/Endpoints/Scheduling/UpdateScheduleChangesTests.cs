using System.Reflection;
using System.Text.Json;
using Fdw.Services.Scheduling.Abstractions.Configuration;
using Fdw.Services.Scheduling.Abstractions.Results;
using Fdw.Services.Scheduling.Clients.Abstractions;
using Fdw.Services.Scheduling.Endpoints;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Web.Api.Tests.Endpoints.Scheduling;

/// <summary>
/// Tests for <see cref="UpdateScheduleChanges"/> (FDW-1170): the one list of members a schedule update may change, the rule that
/// an update states at least one of them, the message that names them and the code that applies each to the stored schedule.
/// </summary>
public sealed class UpdateScheduleChangesTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private static readonly Guid OtherPipeline = Guid.Parse("01a10d18-c1b3-7cf7-b8d0-4598df9f271f");

    private static UpdateScheduleRequest Body(string json) => JsonSerializer.Deserialize<UpdateScheduleRequest>(json, Web)!;

    private static ScheduleImplementationConfiguration Stored()
    {
        var schedule = new ScheduleImplementationConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "UsgsEveryFifteenMinutes",
            PipelineId = Guid.NewGuid(),
            PipelineName = "Usgs",
            Implementation = "Cron",
            CronExpression = "0 2 * * *",
            IntervalSeconds = 60,
            IsEnabled = true,
        };
        schedule.ReplaceRunRoles(["Viewer"]);
        return schedule;
    }

    private static string Snapshot(ScheduleImplementationConfiguration schedule) => string.Join(
        "|",
        schedule.PipelineId,
        schedule.PipelineName,
        schedule.ScheduleType,
        schedule.CronExpression,
        schedule.IntervalSeconds,
        schedule.TimeZoneId,
        schedule.IsEnabled,
        string.Join(",", schedule.Parameters.Select(parameter => $"{parameter.Key}={parameter.Value}")),
        string.Join(",", schedule.RunRoleNames));

    [Theory]
    [InlineData("PipelineId", """{ "pipelineId": "01a10d18-c1b3-7cf7-b8d0-4598df9f271f", "pipelineName": "Other" }""")]
    [InlineData("CronExpression", """{ "cronExpression": "0 3 * * *" }""")]
    [InlineData("IntervalSeconds", """{ "intervalSeconds": 120 }""")]
    [InlineData("TimeZoneId", """{ "timeZoneId": "America/Chicago" }""")]
    [InlineData("IsEnabled", """{ "isEnabled": false }""")]
    [InlineData("Parameters", """{ "parameters": [ { "name": "Region", "value": "West" } ] }""")]
    [InlineData("RunRoles", """{ "runRoles": ["Admin"] }""")]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EveryMemberOnTheListChangesTheSavedScheduleOnItsOwn(string member, string json)
    {
        var body = Body(json);

        UpdateScheduleChanges.Stated(body).ShouldBeTrue();
        UpdateScheduleChanges.Stated(body, member).ShouldBeTrue();
        UpdateScheduleChanges.Names.ShouldContain(member);
    }

    [Theory]
    [InlineData("""{ }""")]
    [InlineData("""{ "name": "UsgsEveryFifteenMinutes" }""")]
    [InlineData("""{ "timeZoneId": null }""")]
    [InlineData("""{ "pipelineName": "Other" }""")]
    [InlineData("""{ "schedulerType": "Cron" }""")]
    [InlineData("""{ "schedulerType": "Interval" }""")]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ABodyThatStatesNoMemberChangesNothing(string json)
    {
        UpdateScheduleChanges.Stated(Body(json)).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void TheRefusalNamesEveryMemberTheListHolds()
    {
        foreach (var member in UpdateScheduleChanges.Names)
        {
            UpdateScheduleChanges.RefusalMessage.ShouldContain(member);
        }

        UpdateScheduleChanges.RefusalMessage.ShouldContain("TimeZoneId");
    }

    // FDW-1171: the refusal tells the caller the type is not something an update can change, since a body that states only the
    // type is refused as stating nothing.
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void TheRefusalSaysTheSchedulerTypeCannotBeChanged()
    {
        foreach (var member in UpdateScheduleChanges.Unchangeable)
        {
            UpdateScheduleChanges.RefusalMessage.ShouldContain(member);
        }

        UpdateScheduleChanges.RefusalMessage.ShouldContain("cannot be changed");
    }

    // The members of the request are the list, bar the route's name, the pipeline's name (sent only with its id, applied with it)
    // and the members an update states but never changes (FDW-1171). A member of neither kind would be accepted and dropped.
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EveryMemberOfTheRequestExceptTheTwoNamesIsOnTheList()
    {
        var statable = typeof(UpdateScheduleRequest)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.Name)
            .Where(name => !string.Equals(name, nameof(UpdateScheduleRequest.Name), StringComparison.Ordinal)
                && !string.Equals(name, nameof(UpdateScheduleRequest.PipelineName), StringComparison.Ordinal))
            .ToList();

        statable.ShouldBe([.. UpdateScheduleChanges.Names, .. UpdateScheduleChanges.Unchangeable], ignoreOrder: true);
    }

    // The body a proxy forwards is the client request. A member the server request has and the client request lacks (or the
    // reverse) is dropped on the way, so the two carry the same members, the route's name aside.
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void TheClientRequestAndTheServerRequestCarryTheSameMembers()
    {
        static IEnumerable<string> Members(Type type) => type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.Name)
            .Where(name => !string.Equals(name, nameof(UpdateScheduleRequest.Name), StringComparison.Ordinal));

        Members(typeof(UpdateScheduleClientRequest)).ShouldBe(Members(typeof(UpdateScheduleRequest)), ignoreOrder: true);
    }

    // FDW-1170: an accepted member is never dropped. Stating it alone changes the stored schedule. The type an update cannot
    // change is not on the list: it is judged, and refused when it differs (FDW-1171).
    [Theory]
    [InlineData("PipelineId", """{ "pipelineId": "01a10d18-c1b3-7cf7-b8d0-4598df9f271f", "pipelineName": "Other" }""")]
    [InlineData("CronExpression", """{ "cronExpression": "0 3 * * *" }""")]
    [InlineData("IntervalSeconds", """{ "intervalSeconds": 120 }""")]
    [InlineData("TimeZoneId", """{ "timeZoneId": "America/Chicago" }""")]
    [InlineData("IsEnabled", """{ "isEnabled": false }""")]
    [InlineData("Parameters", """{ "parameters": [ { "name": "Region", "value": "West" } ] }""")]
    [InlineData("RunRoles", """{ "runRoles": ["Admin"] }""")]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AStatedMemberReachesTheStoredSchedule(string member, string json)
    {
        var schedule = Stored();
        var before = Snapshot(schedule);
        var body = Body(json);
        IReadOnlyList<string>? judgedRoles = body.RunRoles is null ? null : new[] { "Admin" };

        UpdateScheduleChanges.Apply(body, schedule, judgedRoles);

        Snapshot(schedule).ShouldNotBe(before, member);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void OnlyTheSchedulerTypeIsStatedAndNeverChanged()
    {
        UpdateScheduleChanges.Unchangeable.ShouldBe([nameof(UpdateScheduleRequest.SchedulerType)]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void ADifferentSchedulerTypeIsRefusedNotIgnored()
    {
        var result = UpdateScheduleChanges.CheckType(Body("""{ "cronExpression": "0 3 * * *", "schedulerType": "Interval" }"""), "Cron", NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBeSameAs(SchedulingResultCodes.ScheduleTypeChangeRefused);
    }

    [Theory]
    [InlineData("""{ "cronExpression": "0 3 * * *" }""")]
    [InlineData("""{ "cronExpression": "0 3 * * *", "schedulerType": "Cron" }""")]
    [InlineData("""{ "cronExpression": "0 3 * * *", "schedulerType": "cron" }""")]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void ASchedulerTypeThatIsNotStatedOrIsTheStoredOneIsNotRefused(string json)
    {
        UpdateScheduleChanges.CheckType(Body(json), "Cron", NullLogger.Instance).IsSuccess.ShouldBeTrue();
    }

    // A type that equals the stored one is accepted and changes nothing, so it must not leave the stored schedule different.
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void ASchedulerTypeStatedBesideAnotherMemberLeavesTheStoredTypeStanding()
    {
        var schedule = Stored();

        UpdateScheduleChanges.Apply(Body("""{ "cronExpression": "0 3 * * *", "schedulerType": "Cron" }"""), schedule, runRoles: null);

        schedule.Implementation.ShouldBe("Cron");
        schedule.CronExpression.ShouldBe("0 3 * * *");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void AStatedTimeZoneIsTheTimeZoneTheScheduleStores()
    {
        var schedule = Stored();

        UpdateScheduleChanges.Apply(Body("""{ "timeZoneId": "America/Chicago" }"""), schedule, runRoles: null);

        schedule.TimeZoneId.ShouldBe("America/Chicago");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void AStatedTimeZoneBesideAnotherMemberIsNotDropped()
    {
        var schedule = Stored();

        UpdateScheduleChanges.Apply(Body("""{ "cronExpression": "0 3 * * *", "timeZoneId": "America/Chicago" }"""), schedule, runRoles: null);

        schedule.CronExpression.ShouldBe("0 3 * * *");
        schedule.TimeZoneId.ShouldBe("America/Chicago");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void ABodyThatStatesNoTimeZoneLeavesTheStoredOneStanding()
    {
        var schedule = Stored();
        schedule.TimeZoneId = "Europe/Paris";

        UpdateScheduleChanges.Apply(Body("""{ "isEnabled": false }"""), schedule, runRoles: null);

        schedule.TimeZoneId.ShouldBe("Europe/Paris");
        schedule.IsEnabled.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ANewPipelineCarriesItsNameWithIt()
    {
        var schedule = Stored();

        UpdateScheduleChanges.Apply(Body("""{ "pipelineId": "01a10d18-c1b3-7cf7-b8d0-4598df9f271f", "pipelineName": "Other" }"""), schedule, runRoles: null);

        schedule.PipelineId.ShouldBe(OtherPipeline);
        schedule.PipelineName.ShouldBe("Other");
    }
}
