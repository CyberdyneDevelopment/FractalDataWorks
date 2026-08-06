using Fdw.Services.Connections.RoslynWorkspace.Abstractions;
using Fdw.Services.Connections.RoslynWorkspace.Abstractions.Results;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.RoslynWorkspace.Abstractions.Tests;

public class RoslynWorkspaceModesTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "RoslynWorkspaceAbstractions")]
    public void RoslynWorkspaceModes_ByName_Live_ReturnsMode()
    {
        var mode = RoslynWorkspaceModes.ByName("Live");
        mode.ShouldNotBe(RoslynWorkspaceModes.NotFound);
        mode.Name.ShouldBe("Live");
        mode.Id.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "RoslynWorkspaceAbstractions")]
    public void RoslynWorkspaceModes_ByName_Snapshot_ReturnsMode()
    {
        var mode = RoslynWorkspaceModes.ByName("Snapshot");
        mode.ShouldNotBe(RoslynWorkspaceModes.NotFound);
        mode.Name.ShouldBe("Snapshot");
        mode.Id.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "RoslynWorkspaceAbstractions")]
    public void RoslynWorkspaceModes_ByName_Unknown_ReturnsNotFound()
    {
        RoslynWorkspaceModes.ByName("DoesNotExist").ShouldBe(RoslynWorkspaceModes.NotFound);
    }
}

public class RoslynWorkspaceResultCodesTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "RoslynWorkspaceAbstractions")]
    public void RoslynWorkspaceResultCodes_ByName_SolutionPathNotConfigured_ReturnsCode()
    {
        var code = RoslynWorkspaceResultCodes.ByName("SolutionPathNotConfigured");
        code.ShouldNotBe(RoslynWorkspaceResultCodes.NotFound);
        code.Name.ShouldBe("SolutionPathNotConfigured");
        code.Severity.Name.ShouldBe("Error");
        // Catalog invariant: Code == "{prefix}-{number}", Id == EventId == number, Domain == prefix.
        code.Code.ShouldBe($"RW-{code.Id}");
        code.EventId.ShouldBe(code.Id);
        code.Domain.ShouldBe("RW");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "RoslynWorkspaceAbstractions")]
    public void RoslynWorkspaceResultCodes_ByName_SolutionFileNotFound_ReturnsCode()
    {
        var code = RoslynWorkspaceResultCodes.ByName("SolutionFileNotFound");
        code.ShouldNotBe(RoslynWorkspaceResultCodes.NotFound);
        code.Name.ShouldBe("SolutionFileNotFound");
        code.Severity.Name.ShouldBe("Error");
        code.Code.ShouldBe($"RW-{code.Id}");
        code.EventId.ShouldBe(code.Id);
        code.Domain.ShouldBe("RW");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "RoslynWorkspaceAbstractions")]
    public void RoslynWorkspaceResultCodes_ByName_WorkspaceLoadFailed_ReturnsCode()
    {
        var code = RoslynWorkspaceResultCodes.ByName("WorkspaceLoadFailed");
        code.ShouldNotBe(RoslynWorkspaceResultCodes.NotFound);
        code.Name.ShouldBe("WorkspaceLoadFailed");
        code.Severity.Name.ShouldBe("Error");
        code.Code.ShouldBe($"RW-{code.Id}");
        code.EventId.ShouldBe(code.Id);
        code.Domain.ShouldBe("RW");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "RoslynWorkspaceAbstractions")]
    public void RoslynWorkspaceResultCodes_ByName_SymbolNotFound_ReturnsCode()
    {
        var code = RoslynWorkspaceResultCodes.ByName("SymbolNotFound");
        code.ShouldNotBe(RoslynWorkspaceResultCodes.NotFound);
        code.Name.ShouldBe("SymbolNotFound");
        code.Severity.Name.ShouldBe("Error");
        code.Code.ShouldBe($"RW-{code.Id}");
        code.EventId.ShouldBe(code.Id);
        code.Domain.ShouldBe("RW");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "RoslynWorkspaceAbstractions")]
    public void RoslynWorkspaceResultCodes_ByName_InvalidSymbolId_ReturnsCode()
    {
        var code = RoslynWorkspaceResultCodes.ByName("InvalidSymbolId");
        code.ShouldNotBe(RoslynWorkspaceResultCodes.NotFound);
        code.Name.ShouldBe("InvalidSymbolId");
        code.Severity.Name.ShouldBe("Error");
        code.Code.ShouldBe($"RW-{code.Id}");
        code.EventId.ShouldBe(code.Id);
        code.Domain.ShouldBe("RW");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "RoslynWorkspaceAbstractions")]
    public void RoslynWorkspaceResultCodes_ByName_ModeRequiresLive_ReturnsCode()
    {
        var code = RoslynWorkspaceResultCodes.ByName("ModeRequiresLive");
        code.ShouldNotBe(RoslynWorkspaceResultCodes.NotFound);
        code.Name.ShouldBe("ModeRequiresLive");
        code.Severity.Name.ShouldBe("Error");
        code.Code.ShouldBe($"RW-{code.Id}");
        code.EventId.ShouldBe(code.Id);
        code.Domain.ShouldBe("RW");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "RoslynWorkspaceAbstractions")]
    public void RoslynWorkspaceResultCodes_AllFollowCatalogInvariants()
    {
        // Codes are categorized numbers (resultcode-catalog): Code == "RW-{number}",
        // Id == EventId == number, Domain == "RW". Assert the invariant rather than
        // hardcoding the (renumber-prone) per-code numbers.
        foreach (var code in RoslynWorkspaceResultCodes.All())
        {
            if (string.Equals(code.Name, "NotFound", System.StringComparison.Ordinal))
            {
                continue;
            }

            code.Code.ShouldBe($"RW-{code.Id}");
            code.EventId.ShouldBe(code.Id);
            code.Domain.ShouldBe("RW");
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "RoslynWorkspaceAbstractions")]
    public void RoslynWorkspaceResultCodes_ByName_UnknownName_ReturnsNotFound()
    {
        RoslynWorkspaceResultCodes.ByName("DoesNotExist").ShouldBe(RoslynWorkspaceResultCodes.NotFound);
    }
}
