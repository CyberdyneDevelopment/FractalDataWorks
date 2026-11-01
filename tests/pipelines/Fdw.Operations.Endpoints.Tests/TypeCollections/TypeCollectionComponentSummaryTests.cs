using System.Linq;
using Fdw.Operations.Endpoints.TypeCollections;
using Fdw.Services.Etl.Abstractions.Flow;
using Shouldly;
using Xunit;

namespace Fdw.Operations.Endpoints.Tests.TypeCollections;

/// <summary>
/// The catalogue says which settings of an action a client shows to say what the action does (RUI-547), so a client reads it from the
/// listing and does not switch on the kind's name.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "Operations")]
public class TypeCollectionComponentSummaryTests
{
    [Fact]
    public void ASetVariableListsItsVariableAndItsValueAsWhatItDoes()
    {
        var summarising = TypeCollectionResolver.ToSummary(new SetVariableComponentType()).Parameters.Where(p => p.SummarisesComponent).Select(p => p.Name);

        summarising.ShouldBe(["variable", "value"]);
    }

    [Fact]
    public void AnExecutePipelineListsThePipelineItStartsAsWhatItDoes()
    {
        var summarising = TypeCollectionResolver.ToSummary(new ExecutePipelineComponentType()).Parameters.Where(p => p.SummarisesComponent).Select(p => p.Name);

        summarising.ShouldBe(["pipeline"]);
    }

    [Fact]
    public void AnExecuteSqlListsItsScriptAsWhatItDoes()
    {
        var summarising = TypeCollectionResolver.ToSummary(new ExecuteSqlComponentType()).Parameters.Where(p => p.SummarisesComponent).Select(p => p.Name);

        summarising.ShouldBe(["script"]);
    }

    [Fact]
    public void AKindThatStatesNoSummarisingSettingListsNone()
    {
        TypeCollectionResolver.ToSummary(new DataSetSourceComponentType()).Parameters.ShouldNotContain(p => p.SummarisesComponent);
        TypeCollectionResolver.ToSummary(new NotifyComponentType()).Parameters.ShouldNotContain(p => p.SummarisesComponent);
    }

    [Fact]
    public void ASetVariableListsTheEqualsAsTheTextBetweenItsSummarisingSettings()
    {
        TypeCollectionResolver.ToSummary(new SetVariableComponentType()).SummarySeparator.ShouldBe(" = ");
    }

    [Fact]
    public void EveryKindThatSummarisesWithTwoOrMoreSettingsStatesASeparatorAndNoOtherKindDoes()
    {
        foreach (var kind in ComponentTypes.All())
        {
            var separator = TypeCollectionResolver.ToSummary(kind).SummarySeparator;

            if (kind.Settings.Count(setting => setting.SummarisesComponent) >= 2)
                separator.ShouldNotBeNullOrEmpty(kind.Name);
            else
                separator.ShouldBeNull(kind.Name);
        }
    }
}
