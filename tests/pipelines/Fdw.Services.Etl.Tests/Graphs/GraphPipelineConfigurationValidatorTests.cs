using System.Linq;
using Fdw.Services.Etl.Graphs;
using FluentValidation.TestHelper;
using Shouldly;
using Xunit;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// FDW-6141 slice 1: a graph pipeline row is checked when it is written against what its pipeline type requires (FDW-3541's recorded decision, no validator, ends here):
/// an Interactive row that leaves out a fact its type requires is refused naming the fact, a Graph row may state any of them and need none.
/// </summary>
[Trait("Category", "InteractivePipelineType")]
public sealed class GraphPipelineConfigurationValidatorTests
{
    private readonly GraphPipelineConfigurationValidator _validator = new();

    private static GraphPipelineConfiguration Row(string implementation, int? runSeconds = null, int? steps = null, string? halfDone = null, string? claimKey = null)
        => new() { Name = "Turn", Domain = "Pipeline", Implementation = implementation, LinkCapacity = 1, MaxRunSeconds = runSeconds, MaxSteps = steps, HalfDoneRule = halfDone, ClaimKeyTemplate = claimKey };

    [Fact]
    [Trait("Priority", "P0")]
    public void AnInteractiveRowThatStatesNothingIsRefusedOnceForEachRequiredFact()
    {
        var result = _validator.TestValidate(Row("Interactive"));

        result.Errors.Select(error => error.PropertyName).ShouldBe(["HalfDoneRule", "MaxRunSeconds", "MaxSteps", "ClaimKeyTemplate"], ignoreOrder: true);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AnInteractiveRowThatStatesEverythingIsValid()
        => _validator.TestValidate(Row("Interactive", runSeconds: 60, steps: 25, halfDone: "End", claimKey: "{conversation}")).IsValid.ShouldBeTrue();

    [Fact]
    [Trait("Priority", "P0")]
    public void AGraphRowThatStatesNothingIsValidAndOneThatStatesTheFactsToo()
    {
        _validator.TestValidate(Row("Graph")).IsValid.ShouldBeTrue();
        _validator.TestValidate(Row("Graph", runSeconds: 60, steps: 25, halfDone: "End", claimKey: "{conversation}")).IsValid.ShouldBeTrue();
    }
}
