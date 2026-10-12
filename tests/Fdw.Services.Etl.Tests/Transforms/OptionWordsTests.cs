using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Etl.Abstractions.OptionTypes;

namespace Fdw.Services.Etl.Tests.Transforms;

/// <summary>
/// The stage collections of FDW-1120 (run points, validate failing actions, unmatched actions) carry the words the console
/// shows: a display name and a description of their own, never the default made from the name.
/// </summary>
public sealed class OptionWordsTests
{
    private static void ShouldCarryItsOwnWords(IEnumerable<(string Name, string DisplayName, string Description)> options, params string[] names)
    {
        var list = options.ToList();
        list.Select(option => option.Name).ShouldBe(names, ignoreOrder: true);
        foreach (var option in list)
        {
            option.DisplayName.ShouldNotBe(option.Name);
            option.Description.ShouldNotStartWith("Type option:");
            option.Description.ShouldNotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheRunPointsCarryTheirOwnWords() =>
        ShouldCarryItsOwnWords(RunPoints.All().Cast<RunPointBase>().Select(option => (option.Name, option.DisplayName, option.Description)), "AfterStage", "AfterLoad", "OnFailure");

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheValidateFailingActionsCarryTheirOwnWords() =>
        ShouldCarryItsOwnWords(ValidateFailActions.All().Cast<ValidateFailActionBase>().Select(option => (option.Name, option.DisplayName, option.Description)), "Reject", "Fail");

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheUnmatchedActionsCarryTheirOwnWords() =>
        ShouldCarryItsOwnWords(UnmatchedActions.All().Cast<UnmatchedActionBase>().Select(option => (option.Name, option.DisplayName, option.Description)), "Pass", "Reject");
}
