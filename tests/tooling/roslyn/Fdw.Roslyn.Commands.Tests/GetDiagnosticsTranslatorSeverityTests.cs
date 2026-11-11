using System.Threading.Tasks;
using Fdw.Roslyn.Commands.Analysis.Commands;
using Fdw.Roslyn.Commands.Analysis.Translators;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Tests;

/// <summary>
/// GetDiagnostics reads the severity the request names (FDW-5194): a name that is not a level is refused with its own code, never
/// read as Warning.
/// </summary>
public sealed class GetDiagnosticsTranslatorSeverityTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ASeverityThatIsNotALevelIsRefusedWithTheAcceptedNames()
    {
        var result = await new GetDiagnosticsTranslator().Translate(
            new GetDiagnosticsCommand { Severity = "Loud" }, new AdhocWorkspace().CurrentSolution, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        var message = result.CurrentMessage.ShouldNotBeNull();
        message.ShouldContain("'Loud' is not a severity");
        message.ShouldContain("Warning");
        message.ShouldContain("Error");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task ALevelIsMatchedIgnoringCase()
    {
        var result = await new GetDiagnosticsTranslator().Translate(
            new GetDiagnosticsCommand { Severity = "error" }, new AdhocWorkspace().CurrentSolution, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
    }
}
