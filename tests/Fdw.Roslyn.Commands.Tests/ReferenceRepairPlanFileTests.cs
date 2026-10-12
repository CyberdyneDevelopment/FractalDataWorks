using System;
using System.IO;
using System.Linq;
using Fdw.Roslyn.Commands.Workspace.Helpers;
using Fdw.Roslyn.Commands.Workspace.Results;

namespace Fdw.Roslyn.Commands.Tests;

/// <summary>
/// Tests for the hand-editable repair plan: deleting a line is how a reviewer rejects a fix.
/// </summary>
public sealed class ReferenceRepairPlanFileTests
{
    private static ReferenceRepair Repair(string id, string assembly) => new()
    {
        Id = id,
        Project = id.Split("=>")[0],
        RequiredAssembly = assembly,
        LedgerMatch = "Fdw.Data.MsSql.Marker",
        ReferenceKind = "ProjectReference",
    };

    private static string TempFile() =>
        Path.Combine(Path.GetTempPath(), "fdw595-plan-" + Guid.NewGuid().ToString("N") + ".txt");

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void WrittenPlanRoundTripsEveryId()
    {
        var path = TempFile();
        try
        {
            var proposals = new[] { Repair("A=>X", "X"), Repair("B=>Y", "Y") };
            ReferenceRepairPlanFile.Write(path, proposals, DateTimeOffset.UnixEpoch).ShouldBe(2);

            ReferenceRepairPlanFile.ReadApprovedIds(path).ShouldBe(new[] { "A=>X", "B=>Y" });
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void DeletingALineRejectsThatRepair()
    {
        var path = TempFile();
        try
        {
            ReferenceRepairPlanFile.Write(path, new[] { Repair("A=>X", "X"), Repair("B=>Y", "Y") }, DateTimeOffset.UnixEpoch);

            // The reviewer prunes the second entry.
            var kept = File.ReadAllLines(path).Where(l => !l.StartsWith("B=>Y", StringComparison.Ordinal)).ToArray();
            File.WriteAllLines(path, kept);

            ReferenceRepairPlanFile.ReadApprovedIds(path).ShouldBe(new[] { "A=>X" });
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void CommentsAndBlankLinesAreIgnored()
    {
        var path = TempFile();
        try
        {
            File.WriteAllText(path, "# a comment\n\n   \nA=>X | ProjectReference | needs: X\n# another\n");

            ReferenceRepairPlanFile.ReadApprovedIds(path).ShouldBe(new[] { "A=>X" });
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void PlanCarriesTheReasonSoAReviewerCanJudgeIt()
    {
        var path = TempFile();
        try
        {
            ReferenceRepairPlanFile.Write(path, new[] { Repair("A=>X", "Fdw.Data.Types.Databases") }, DateTimeOffset.UnixEpoch);
            var text = File.ReadAllText(path);

            text.ShouldContain("DELETE any line you do NOT want applied");
            text.ShouldContain("needs: Fdw.Data.Types.Databases");
            text.ShouldContain("because: Fdw.Data.MsSql.Marker");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AnEmptyPrunedPlanApprovesNothing()
    {
        var path = TempFile();
        try
        {
            File.WriteAllText(path, "# everything was rejected\n");
            ReferenceRepairPlanFile.ReadApprovedIds(path).ShouldBeEmpty();
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }
}
