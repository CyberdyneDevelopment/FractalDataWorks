using System.Linq;
using Fdw.Operations.Endpoints.TypeCollections;
using Fdw.Services.Pipelines;
using Fdw.Services.Pipelines.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Operations.Endpoints.Tests.TypeCollections;

/// <summary>
/// FDW-6738: what the console's pipeline editor reads from the host. The type-collection route lists each pipeline type with the facts a pipeline of it must state, in the
/// summary's existing RequiredProperties (the one answer to "what must I state" the HTTP and SQL authentication options already give), taken from the type's
/// ConfigurationRequirements, and lists the half-done rules the host allows from the same route.
/// </summary>
[Trait("Category", "PipelineTypeRequirementsSummary")]
public sealed class PipelineTypeRequirementsSummaryTests
{
    [Fact]
    [Trait("Priority", "P0")]
    public void TheSummaryOfInteractiveListsTheFourFactsAPipelineOfItMustState() =>
        TypeCollectionResolver.ToSummary(PipelineTypes.ByName("Interactive")).RequiredProperties
            .ShouldBe(["HalfDoneRule", "MaxRunSeconds", "MaxSteps", "ClaimKeyTemplate"], ignoreOrder: true);

    [Fact]
    [Trait("Priority", "P0")]
    public void TheSummaryOfGraphListsNoFact() =>
        TypeCollectionResolver.ToSummary(PipelineTypes.ByName("Graph")).RequiredProperties.ShouldBeEmpty();

    [Fact]
    [Trait("Priority", "P0")]
    public void TheRequiredPropertiesOfATypeAreItsConfigurationRequirementsAndCannotDiffer()
    {
        foreach (var type in PipelineTypes.All().Values)
        {
            TypeCollectionResolver.ToSummary(type).RequiredProperties
                .ShouldBe([.. type.ConfigurationRequirements.Select(requirement => requirement.FactName)], ignoreOrder: true, customMessage: type.Name);
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void TheRouteListsBothPipelineTypesAndTheHalfDoneRulesTheHostAllows()
    {
        var types = TypeCollectionResolver.GetValues(TypeCollectionResolver.FindCollectionType("PipelineTypes").ShouldNotBeNull());
        var rules = TypeCollectionResolver.GetValues(TypeCollectionResolver.FindCollectionType("HalfDoneRules").ShouldNotBeNull());

        types.Select(type => type.Name).ShouldContain("Graph");
        types.Select(type => type.Name).ShouldContain("Interactive");
        rules.Select(rule => rule.Name).ShouldContain("End");
    }
}
