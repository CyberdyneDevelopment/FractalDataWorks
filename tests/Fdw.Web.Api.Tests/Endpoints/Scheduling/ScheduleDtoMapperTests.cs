using System.Collections;
using System.Reflection;
using Fdw.Services.Scheduling.Abstractions.Configuration;
using Fdw.Services.Scheduling.Endpoints;

namespace Fdw.Web.Api.Tests.Endpoints.Scheduling;

/// <summary>
/// Tests for <see cref="ScheduleDtoMapper"/> (FDW-1166): every response that shows a schedule shows the members the schedule
/// stores, the list rows as much as the single read.
/// </summary>
public sealed class ScheduleDtoMapperTests
{
    private static ScheduleImplementationConfiguration Stored()
    {
        var schedule = new ScheduleImplementationConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "UsgsEveryFifteenMinutes",
            PipelineId = Guid.NewGuid(),
            PipelineName = "Usgs",
            Implementation = "Interval",
            IntervalSeconds = 900,
            IsEnabled = true,
            Parameters = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase) { ["Region"] = "West" }
        };
        schedule.ReplaceRunRoles(["Admin", "DataEngineer"]);
        return schedule;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void AListedScheduleShowsTheRolesItsRunsActWith()
    {
        var row = ScheduleDtoMapper.Summary(Stored());

        row.RunRoles.ShouldNotBeNull().ShouldBe(["Admin", "DataEngineer"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void AListedScheduleShowsItsRunParameters()
    {
        var row = ScheduleDtoMapper.Summary(Stored());

        var parameter = row.Parameters.ShouldNotBeNull().ShouldHaveSingleItem();
        parameter.Name.ShouldBe("Region");
        parameter.Value.ShouldBe("West");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void AListedScheduleAndTheSameScheduleReadAloneAgreeOnTheirRolesAndParameters()
    {
        var schedule = Stored();

        var row = ScheduleDtoMapper.Summary(schedule);
        var detail = ScheduleDtoMapper.Detail(schedule);

        row.RunRoles.ShouldBe(detail.RunRoles);
        row.Parameters.ShouldNotBeNull().Select(p => (p.Name, p.Value)).ShouldBe(detail.Parameters.ShouldNotBeNull().Select(p => (p.Name, p.Value)));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AScheduleThatRecordsNoRolesShowsAnEmptyListNotAMissingOne()
    {
        var schedule = new ScheduleImplementationConfiguration { Name = "Manual", PipelineName = "Usgs", Implementation = "Manual" };

        ScheduleDtoMapper.Summary(schedule).RunRoles.ShouldNotBeNull().ShouldBeEmpty();
        ScheduleDtoMapper.Detail(schedule).RunRoles.ShouldNotBeNull().ShouldBeEmpty();
    }

    // Every property of the stored schedule is set to a value that is not its type's default, so a DTO member left unset by a
    // mapper shows up as a default value.
    private static ScheduleImplementationConfiguration FullyPopulated()
    {
        var schedule = Stored();
        schedule.Description = "Every fifteen minutes";
        schedule.Implementation = "Cron";
        schedule.CronExpression = "0 0/15 * * * ?";
        schedule.TimeZoneId = "Europe/Berlin";
        schedule.NextRunTime = DateTimeOffset.UtcNow.AddMinutes(15);
        schedule.LastRunTime = DateTimeOffset.UtcNow.AddMinutes(-15);
        schedule.CreateDate = DateTimeOffset.UtcNow.AddDays(-2);
        schedule.ModifyDate = DateTimeOffset.UtcNow.AddDays(-1);
        schedule.CreateBy = "creator";
        schedule.ModifyBy = "modifier";
        schedule.CreateOnBehalfOf = "creator-for";
        schedule.ModifyOnBehalfOf = "modifier-for";
        return schedule;
    }

    private static bool IsUnset(object? value) => value switch
    {
        null => true,
        string text => text.Length == 0,
        Guid id => id == Guid.Empty,
        DateTimeOffset moment => moment == default,
        bool flag => !flag,
        int number => number == 0,
        ICollection items => items.Count == 0,
        _ => false
    };

    private static IEnumerable<string> MembersLeftUnset(object dto) => dto.GetType()
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(property => property.SetMethod is { IsPublic: true } && IsUnset(property.GetValue(dto)))
        .Select(property => property.Name);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void TheDetailSetsEveryMemberItDeclares()
    {
        MembersLeftUnset(ScheduleDtoMapper.Detail(FullyPopulated())).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void TheListRowSetsEveryMemberItDeclares()
    {
        MembersLeftUnset(ScheduleDtoMapper.Summary(FullyPopulated())).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void TheDetailAndTheListRowAgreeOnTheTimeZoneAndTheAudit()
    {
        var schedule = FullyPopulated();

        var detail = ScheduleDtoMapper.Detail(schedule);
        var row = ScheduleDtoMapper.Summary(schedule);

        detail.TimeZoneId.ShouldBe("Europe/Berlin");
        detail.TimeZoneId.ShouldBe(row.TimeZoneId);
        detail.CreatedAt.ShouldBe(row.CreatedAt);
        detail.CreatedBy.ShouldBe(row.CreatedBy);
        detail.ModifiedBy.ShouldBe(row.ModifiedBy);
        detail.CreatedOnBehalfOf.ShouldBe(row.CreatedOnBehalfOf);
        detail.ModifiedOnBehalfOf.ShouldBe(row.ModifiedOnBehalfOf);
        detail.UpdatedAt.ShouldBe(schedule.ModifyDate);
    }
}
