using System.Collections.Generic;
using Fdw.Services.Workspaces.Endpoints;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Workspaces;

/// <summary>FDW-4404: the readiness report counts a data set through its class, as the switch on the class name did.</summary>
[Collection(nameof(ServicesTestCollection))]
public class WorkspaceReadinessClassesTests
{
    [Fact]
    public void ABoundDataSetIsCountedAndBlocksNothing()
    {
        var report = new WorkspaceReadinessResponse();
        var blocked = new List<string>();

        var tally = WorkspaceReadinessClasses.ByName("Bound").Tally(report, blocked, "Orders", NullLogger.Instance);

        tally.IsSuccess.ShouldBeTrue();
        (report.Bound, report.Partial, report.Sketched, report.Proposed).ShouldBe((1, 0, 0, 0));
        blocked.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("Partial", 0, 1, 0, 0)]
    [InlineData("Sketched", 0, 0, 1, 0)]
    [InlineData("Proposed", 0, 0, 0, 1)]
    public void ADataSetBelowBoundIsCountedInItsOwnClassAndNamedAsBlocking(string name, int bound, int partial, int sketched, int proposed)
    {
        var report = new WorkspaceReadinessResponse();
        var blocked = new List<string>();

        var tally = WorkspaceReadinessClasses.ByName(name).Tally(report, blocked, "Orders", NullLogger.Instance);

        tally.IsSuccess.ShouldBeTrue();
        (report.Bound, report.Partial, report.Sketched, report.Proposed).ShouldBe((bound, partial, sketched, proposed));
        blocked.ShouldBe(["Orders"]);
    }

    [Fact]
    public void AClassNoOptionHoldsIsRefusedNotCountedAsProposed()
    {
        var report = new WorkspaceReadinessResponse();
        var blocked = new List<string>();

        var tally = WorkspaceReadinessClasses.ByName("Verified").Tally(report, blocked, "Orders", NullLogger.Instance);

        tally.IsFailure.ShouldBeTrue();
        tally.Code!.Name.ShouldBe("WorkspaceReadinessClassUnknown");
        (report.Bound, report.Partial, report.Sketched, report.Proposed).ShouldBe((0, 0, 0, 0));
        blocked.ShouldBeEmpty();
    }
}
