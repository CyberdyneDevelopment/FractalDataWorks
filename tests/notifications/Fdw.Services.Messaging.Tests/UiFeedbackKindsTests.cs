using System.Linq;

namespace Fdw.Services.Messaging.Tests;

/// <summary>
/// FDW-2142: the urgencies and tags a UI comment can carry. The console names none of them, so what matters is what each states: a colour, a rank
/// that orders the urgencies, and the one rule an alert follows, whether it must be acknowledged.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "Messaging")]
public class UiFeedbackKindsTests
{
    [Fact]
    public void TheUrgenciesAreOrderedByRankWithSecurityIssueMostUrgent()
    {
        UiFeedbackUrgencies.All().OrderByDescending(urgency => urgency.Rank).Select(urgency => urgency.Name)
            .ShouldBe(["SecurityIssue", "High", "Normal", "Low"]);
    }

    [Fact]
    public void OnlyASecurityIssueMustBeAcknowledged()
    {
        UiFeedbackUrgencies.All().Where(urgency => urgency.NeedsAcknowledgement).Select(urgency => urgency.Name).ShouldBe(["SecurityIssue"]);
    }

    [Theory]
    [InlineData("Low", "neutral")]
    [InlineData("Normal", "info")]
    [InlineData("High", "warning")]
    [InlineData("SecurityIssue", "error")]
    public void EachUrgencyStatesItsColour(string name, string colour)
    {
        UiFeedbackUrgencies.ByName(name).Color.ShouldBe(colour);
    }

    [Theory]
    [InlineData("Low", "Info")]
    [InlineData("Normal", "Info")]
    [InlineData("High", "Warning")]
    [InlineData("SecurityIssue", "Error")]
    public void EachUrgencyStatesTheSeverityOfTheMessageThatTellsAReader(string name, string severity)
    {
        UiFeedbackUrgencies.ByName(name).Severity.ShouldBe(severity);
    }

    [Fact]
    public void NoTwoUrgenciesShareARank()
    {
        UiFeedbackUrgencies.All().Select(urgency => urgency.Rank).Distinct().Count().ShouldBe(UiFeedbackUrgencies.All().Count());
    }

    [Fact]
    public void AnUrgencyNobodyDeclaredIsNotFoundAndHasNoDefault()
    {
        UiFeedbackUrgencies.ByName("Whenever").ShouldBeSameAs(UiFeedbackUrgencies.NotFound);
        UiFeedbackTags.ByName("Whenever").ShouldBeSameAs(UiFeedbackTags.NotFound);
    }

    [Fact]
    public void EachTagHasAWordingAndAColour()
    {
        UiFeedbackTags.All().ShouldAllBe(tag => !string.IsNullOrWhiteSpace(tag.DisplayName) && !string.IsNullOrWhiteSpace(tag.Color));
        UiFeedbackTags.ByName("UsabilityIssue").DisplayName.ShouldBe("Usability Issue");
    }
}
