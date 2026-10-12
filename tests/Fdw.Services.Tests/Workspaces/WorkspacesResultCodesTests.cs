using System.Linq;
using Fdw.Services.Workspaces.Results;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Workspaces;

/// <summary>
/// FDW-904: a result code's severity decides IsSuccess on every result built from it, so a code the
/// workspace services pass to GenericResult.Failure must carry a failure severity. A Warning-severity
/// refusal reads as a success, and every IsFailure gate lets it through with a null value.
/// </summary>
[Collection(nameof(ServicesTestCollection))]
public class WorkspacesResultCodesTests
{
    [Fact]
    public void EveryWorkspaceCodeCarriesAFailureSeverity()
    {
        WorkspacesResultCodes.All()
            .Where(code => code.Severity.IsSuccess)
            .Select(code => code.Name)
            .ShouldBeEmpty();
    }
}
