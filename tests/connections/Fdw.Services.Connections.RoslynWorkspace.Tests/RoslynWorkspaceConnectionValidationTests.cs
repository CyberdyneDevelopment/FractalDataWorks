using System.Linq;
using System.Text.Json;
using Fdw.Messages;
using Fdw.Services.Connections.RoslynWorkspace.Registration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections.RoslynWorkspace.Tests;

/// <summary>
/// A Roslyn workspace connection type attaches its configuration validator, so a create or update that states no solution path is
/// refused before it is saved, with the rule's message shown to the caller (FDW-3541). The validator existed and ran nowhere.
/// </summary>
public class RoslynWorkspaceConnectionValidationTests
{
    private static JsonElement Json(string text)
    {
        using var document = JsonDocument.Parse(text);
        return document.RootElement.Clone();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ACreateThatStatesNoSolutionPathIsRefusedAndNamesTheMember()
    {
        var bound = new RoslynWorkspaceConnectionType().EndpointBehavior.Bind(
            Json("""{ "solutionPath": "", "modeName": "ReadOnly" }"""), NullLogger.Instance);

        bound.IsFailure.ShouldBeTrue();
        bound.Messages.OfType<IClientMessage>().ShouldHaveSingleItem().ClientText.ShouldContain("SolutionPath is required");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ACreateThatStatesNoModeIsRefusedAndNamesTheMember()
    {
        var bound = new RoslynWorkspaceConnectionType().EndpointBehavior.Bind(
            Json("""{ "solutionPath": "/src/app.slnx" }"""), NullLogger.Instance);

        bound.IsFailure.ShouldBeTrue();
        bound.Messages.OfType<IClientMessage>().ShouldHaveSingleItem().ClientText.ShouldContain("ModeName is required");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ACreateThatStatesBothIsAcceptedAndTheDeprecatedFieldsStillSetThem()
    {
        var named = new RoslynWorkspaceConnectionType().EndpointBehavior.Bind(
            Json("""{ "solutionPath": "/src/app.slnx", "modeName": "ReadOnly" }"""), NullLogger.Instance);
        var aliased = new RoslynWorkspaceConnectionType().EndpointBehavior.Bind(
            Json("""{ "baseUrl": "/src/app.slnx", "protocol": "ReadOnly" }"""), NullLogger.Instance);

        named.IsSuccess.ShouldBeTrue();
        aliased.IsSuccess.ShouldBeTrue();
        aliased.Value.ShouldBeOfType<RoslynWorkspaceConnectionConfiguration>().SolutionPath.ShouldBe("/src/app.slnx");
    }
}
