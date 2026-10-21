using System;
using System.Linq;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Transforms;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Transforms;

/// <summary>
/// Tests for <see cref="NotifyVariables"/> (FDW-1120): the variables a Notify message may use are data, each with the words
/// the console shows, and the validator that refuses an unknown <c>{{variable}}</c> reads that same list.
/// </summary>
public sealed class NotifyVariablesTests
{
    private static NotifyRunFacts Facts() =>
        new(Guid.NewGuid(), "NightlyCopy", Guid.Parse("11111111-2222-3333-4444-555555555555"), rowsExtracted: 120, rowsRejected: 7, rowsLoaded: 113,
            startedAt: new DateTimeOffset(2026, 10, 14, 19, 53, 57, TimeSpan.Zero),
            endedAt: new DateTimeOffset(2026, 10, 14, 19, 54, 2, TimeSpan.Zero),
            outcome: "Succeeded");

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheCollectionListsEveryVariableByBareName()
    {
        NotifyVariables.All().Select(variable => variable.Name)
            .ShouldBe(new[] { "EndedAt", "ExecutionId", "Outcome", "PipelineName", "RowsExtracted", "RowsLoaded", "RowsRejected", "StartedAt" }, ignoreOrder: true);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void EveryVariableCarriesItsOwnDisplayNameAndDescription()
    {
        foreach (var variable in NotifyVariables.All().Cast<NotifyVariableBase>())
        {
            variable.DisplayName.ShouldNotBe(variable.Name);
            variable.Description.ShouldNotStartWith("Type option:");
            variable.Description.ShouldNotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void EveryVariableReadsItsValueFromTheRunsFacts()
    {
        var facts = Facts();

        NotifyVariables.ByName("PipelineName").ValueOf(facts).ShouldBe("NightlyCopy");
        NotifyVariables.ByName("ExecutionId").ValueOf(facts).ShouldBe("11111111-2222-3333-4444-555555555555");
        NotifyVariables.ByName("RowsExtracted").ValueOf(facts).ShouldBe("120");
        NotifyVariables.ByName("RowsRejected").ValueOf(facts).ShouldBe("7");
        NotifyVariables.ByName("RowsLoaded").ValueOf(facts).ShouldBe("113");
        NotifyVariables.ByName("StartedAt").ValueOf(facts).ShouldBe("2026-10-14T19:53:57.0000000+00:00");
        NotifyVariables.ByName("EndedAt").ValueOf(facts).ShouldBe("2026-10-14T19:54:02.0000000+00:00");
        NotifyVariables.ByName("Outcome").ValueOf(facts).ShouldBe("Succeeded");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TheValidatorAcceptsEveryVariableTheCollectionListsAndRefusesTheRest()
    {
        // Arrange: a message that uses every listed variable, and one that uses a name no option carries
        var everyVariable = string.Join(' ', NotifyVariables.All().Select(variable => "{{" + variable.Name + "}}"));

        // Act
        var accepted = NotifyTemplate.Expand("Alert", everyVariable, Facts(), NullLogger.Instance);
        var refused = NotifyTemplate.Expand("Alert", "{{PipelineName}} {{Mood}}", Facts(), NullLogger.Instance);

        // Assert
        NotifyTemplate.Unknown(everyVariable).ShouldBeEmpty();
        accepted.IsSuccess.ShouldBeTrue(accepted.CurrentMessage);
        refused.IsFailure.ShouldBeTrue();
        refused.CurrentMessage.ShouldNotBeNull().ShouldContain("Mood");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void TheRefusalNamesTheVariablesTheCollectionListsAsTheOnesARunHas()
    {
        // Act
        var refused = NotifyTemplate.Expand("Alert", "{{Mood}}", Facts(), NullLogger.Instance);

        // Assert
        foreach (var variable in NotifyVariables.All())
        {
            refused.CurrentMessage.ShouldNotBeNull().ShouldContain(variable.Name);
        }
    }
}
