using System.Collections.Generic;
using System.Globalization;
using Fdw.Services.Calculations.Abstractions.Lineage;
using Shouldly;
using Xunit;

namespace Fdw.Services.Calculations.Tests;

/// <summary>
/// Covers the one agreed representation for values recorded in a calculation trace. A persisted
/// trace is only evidence if it reads back the same way it was written, which means the rendering
/// has to be culture-independent and deterministic rather than whatever each consumer chose.
/// </summary>
[Trait("Priority", "P0")]
[Trait("Category", "CoreFramework")]
public class CalculationTraceValueTests
{
    [Fact]
    public void FromRecordsANullValueAsAbsentRatherThanAsText()
    {
        var value = CalculationTraceValue.From(null);

        // No type to record, and no text standing in for the value that was not there.
        value.RuntimeType.ShouldBeEmpty();
        value.Text.ShouldBeNull();
    }

    [Fact]
    public void FromRecordsTheRuntimeTypeAlongsideTheText()
    {
        CalculationTraceValue.From(125.50m).RuntimeType.ShouldBe("Decimal");
        CalculationTraceValue.From(42).RuntimeType.ShouldBe("Int32");
        CalculationTraceValue.From("kwh").RuntimeType.ShouldBe("String");
    }

    /// <summary>
    /// The property the whole representation exists for: the same decimal traced on a machine with
    /// a comma decimal separator must render identically to one with a period, or two readers of
    /// the same trace disagree about the amount.
    /// </summary>
    [Fact]
    public void FromRendersNumbersInInvariantCultureRegardlessOfAmbientCulture()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            CalculationTraceValue.From(1234.56m).Text.ShouldBe("1234.56");
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void FromRendersAStringAsItsOwnText()
    {
        CalculationTraceValue.From("1000").Text.ShouldBe("1000");
    }

    /// <summary>
    /// Rows are what input operands narrow a field out of, so a trace that records the whole row
    /// has to render it the same way every time — hence field order by name, not hash order.
    /// </summary>
    [Fact]
    public void FromRendersARowWithItsFieldsOrderedByName()
    {
        var row = new Dictionary<string, object?>
        {
            ["kwh"] = 250m,
            ["account"] = "A-1",
            ["adjustment"] = null,
        };

        CalculationTraceValue.From(row).Text.ShouldBe("{account=A-1, adjustment=null, kwh=250}");
    }

    [Fact]
    public void FromRendersAMutableRowTheSameWayAsAReadOnlyOne()
    {
        var readOnly = new Dictionary<string, object?> { ["kwh"] = 250m };
        IDictionary<string, object> mutable = new Dictionary<string, object> { ["kwh"] = 250m };

        CalculationTraceValue.From(mutable).Text.ShouldBe(CalculationTraceValue.From(readOnly).Text);
    }

    [Fact]
    public void FromRendersASequenceInIterationOrder()
    {
        CalculationTraceValue.From(new[] { 3m, 1m, 2m }).Text.ShouldBe("[3, 1, 2]");
    }

    /// <summary>
    /// A null inside a composite has no absence flag of its own to set, so it is marked in the text
    /// rather than elided — dropping it would silently change the shape of what was recorded.
    /// </summary>
    [Fact]
    public void FromMarksNullElementsInsideASequence()
    {
        CalculationTraceValue.From(new object?[] { 1m, null, 3m }).Text.ShouldBe("[1, null, 3]");
    }

    /// <summary>
    /// A type that renders no text is recorded as what it was, not as blank — the reader can tell
    /// something was present and what type it had.
    /// </summary>
    [Fact]
    public void FromMarksAnUnrenderableElementByItsTypeName()
    {
        CalculationTraceValue.From(new object?[] { new NullRendering() }).Text
            .ShouldBe("[<NullRendering>]");
    }

    [Fact]
    public void FromRecordsNoTextForATopLevelValueThatRendersNothing()
    {
        var value = CalculationTraceValue.From(new NullRendering());

        // Populated type with absent text is the case that distinguishes "rendered nothing" from
        // "was null", which records an empty type.
        value.RuntimeType.ShouldBe("NullRendering");
        value.Text.ShouldBeNull();
    }

    private sealed class NullRendering
    {
        public override string? ToString() => null;
    }
}
