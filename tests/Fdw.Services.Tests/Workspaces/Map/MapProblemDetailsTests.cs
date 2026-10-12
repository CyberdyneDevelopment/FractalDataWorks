using Fdw.Results;
using Fdw.Services.Workspaces.Endpoints;
using Fdw.Services.Workspaces.Results;
using Microsoft.AspNetCore.Http;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Workspaces.Map;

/// <summary>FDW-1200: every refusal the map gives answers its own status with its own code, never a 500 from a code that cannot be looked up.</summary>
[Collection(nameof(ServicesTestCollection))]
public class MapProblemDetailsTests
{
    [Theory]
    [InlineData("MapRequestInvalid", 400)]
    [InlineData("MapNodeNotFound", 404)]
    [InlineData("WorkspaceNotVisible", 404)]
    [InlineData("MapExplodeNotPermitted", 403)]
    [InlineData("MapSearchUnavailable", 500)]
    public void ARefusalAnswersTheStatusOfItsCategory(string codeName, int status)
    {
        var refused = GenericResult.Failure(
            WorkspacesResultCodes.ByName(codeName), ResultDetails.Create("reason", "x", "node", "x", "name", "x"));

        var problem = MapProblemDetails.From(refused, new DefaultHttpContext());

        problem.Status.ShouldBe(status);
        problem.Errors.ShouldHaveSingleItem();
    }
}
