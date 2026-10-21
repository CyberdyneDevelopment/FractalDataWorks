using System.Linq;
using System.Text.Json;
using Fdw.Operations.Endpoints.TypeCollections;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Messaging;
using Shouldly;
using Xunit;

namespace Fdw.Operations.Endpoints.Tests.TypeCollections;

/// <summary>
/// FDW-2142: the type-collection value listing gives an option's colour, rank and acknowledgement rule when the option states them, so a console draws and
/// orders the UI comment urgencies and tags it has never heard of; an option that states none lists none.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "Operations")]
public class TypeCollectionColorRankTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    [Fact]
    public void AnUrgencyListsItsColourRankAndWhetherItNeedsAcknowledgement()
    {
        var summary = TypeCollectionResolver.ToSummary(new SecurityIssueUiFeedbackUrgency());

        summary.Name.ShouldBe("SecurityIssue");
        summary.DisplayName.ShouldBe("Security Issue");
        summary.Color.ShouldBe("error");
        summary.Rank.ShouldBe(4);
        summary.NeedsAcknowledgement.ShouldBe(true);
    }

    [Fact]
    public void ATagListsItsColourAndNoRankOrRule()
    {
        var summary = TypeCollectionResolver.ToSummary(new CalculationIssueUiFeedbackTag());

        summary.Color.ShouldBe("error");
        summary.Rank.ShouldBeNull();
        summary.NeedsAcknowledgement.ShouldBeNull();
    }

    [Fact]
    public void AnOptionThatStatesNoneOfThemListsNone()
    {
        var summary = TypeCollectionResolver.ToSummary(new MergeJoinComponentType());

        summary.Color.ShouldBeNull();
        summary.Rank.ShouldBeNull();
        summary.NeedsAcknowledgement.ShouldBeNull();
    }

    [Fact]
    public void TheFactsAreListedUnderTheNamesTheConsoleReads()
    {
        var json = JsonSerializer.Serialize(TypeCollectionResolver.ToSummary(new HighUiFeedbackUrgency()), Web);

        json.ShouldContain("\"color\":\"warning\"");
        json.ShouldContain("\"rank\":3");
        json.ShouldContain("\"needsAcknowledgement\":false");
    }

    [Fact]
    public void TheCollectionsAreFoundByNameAndEveryOptionIsListed()
    {
        // Why the options are read first: the resolver scans the loaded assemblies, and reading them loads this one.
        UiFeedbackUrgencies.All().Select(urgency => urgency.Name).Order().ShouldBe(["High", "Low", "Normal", "SecurityIssue"]);
        UiFeedbackTags.All().Select(tag => tag.Name).Order().ShouldBe(["CalculationIssue", "FeatureRequest", "UsabilityIssue"]);

        TypeCollectionResolver.FindCollectionType("UiFeedbackUrgencies").ShouldNotBeNull();
        TypeCollectionResolver.FindCollectionType("UiFeedbackTags").ShouldNotBeNull();
    }
}
