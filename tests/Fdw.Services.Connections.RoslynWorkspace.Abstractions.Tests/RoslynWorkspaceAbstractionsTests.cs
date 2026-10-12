using Fdw.Services.Connections.RoslynWorkspace.Abstractions;
using Fdw.Services.Connections.RoslynWorkspace.Abstractions.Results;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.RoslynWorkspace.Abstractions.Tests;

public class RoslynWorkspaceModesTests
{
}

public class RoslynWorkspaceResultCodesTests
{
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
}
