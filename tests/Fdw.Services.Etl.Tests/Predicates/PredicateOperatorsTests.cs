using System.Linq;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Predicates;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Predicates;

/// <summary>
/// Tests for the predicate operator catalogue (FDW-1395): the help the console shows is only as good as its examples, so each
/// option's example is read by the real condition reader.
/// </summary>
public sealed class PredicateOperatorsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EveryOperatorStatesAFormAndAnExampleTheConditionReaderAccepts()
    {
        var operators = PredicateOperators.All();

        operators.ShouldNotBeEmpty();
        foreach (var option in operators)
        {
            option.Symbols.ShouldNotBeEmpty(option.Name);
            var parsed = RecordPredicates.Parse(option.Example, NullLogger.Instance);
            parsed.IsSuccess.ShouldBeTrue($"{option.Name}: {option.Example} -> {parsed.CurrentMessage}");
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void TheComparisonAndTextSignsTheReaderKnowAreAllListed()
    {
        var symbols = PredicateOperators.All().SelectMany(o => o.Symbols).ToList();

        symbols.ShouldContain("==");
        symbols.ShouldContain("!=");
        symbols.ShouldContain(">=");
        symbols.ShouldContain("<=");
        symbols.ShouldContain(">");
        symbols.ShouldContain("<");
        symbols.ShouldContain("contains");
        symbols.ShouldContain("startswith");
        symbols.ShouldContain("endswith");
        symbols.ShouldContain("and");
        symbols.ShouldContain("&&");
        symbols.ShouldContain("or");
        symbols.ShouldContain("||");
        symbols.ShouldContain("not");
        symbols.ShouldContain("!");
    }
}
