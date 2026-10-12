using Fdw.Services.Scheduling.Abstractions.Configuration;

namespace Fdw.Services.Scheduling.Tests;

/// <summary>
/// A schedule records the roles its runs act with (FDW-1161): whoever creates or changes it names them, and they replace the
/// recorded ones whole.
/// </summary>
public sealed class ScheduleRunRolesTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ANewScheduleRecordsNoRole()
    {
        new ScheduleImplementationConfiguration().RunRoleNames.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void TheNamedRolesAreRecordedOneRowEach()
    {
        var schedule = new ScheduleImplementationConfiguration();

        schedule.ReplaceRunRoles(["DataEngineer", "Analyst"]);

        schedule.RunRoleNames.ShouldBe(["DataEngineer", "Analyst"]);
        schedule.RunRoles.Select(role => role.RoleName).ShouldBe(["DataEngineer", "Analyst"]);
        schedule.RunRoles.Select(role => role.Id).Distinct().Count().ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void NamingRolesAgainReplacesTheRecordedOnesWhole()
    {
        var schedule = new ScheduleImplementationConfiguration();
        schedule.ReplaceRunRoles(["DataEngineer", "Analyst"]);

        schedule.ReplaceRunRoles(["Operator"]);

        schedule.RunRoleNames.ShouldBe(["Operator"]);
    }
}
