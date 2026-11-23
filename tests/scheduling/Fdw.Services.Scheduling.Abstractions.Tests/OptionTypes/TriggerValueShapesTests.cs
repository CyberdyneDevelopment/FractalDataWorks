using Fdw.Services.Scheduling.Abstractions.OptionTypes;
using Shouldly;
using Xunit;

namespace Fdw.Services.Scheduling.Abstractions.Tests.OptionTypes;

/// <summary>
/// FDW-4402: each trigger type keeps the set of configuration values it accepted before the readers became the TriggerValueShapes collection:
/// Interval reads a float, Window does not, and a stored trigger's own validation reads only an int or its text.
/// </summary>
public class TriggerValueShapesTests
{
    [Fact]
    public void IntervalReadsAFloatThatWindowDoesNot()
    {
        TriggerValueShapes.ReadInt(30f, out var interval).ShouldBeTrue();
        interval.ShouldBe(30);
        TriggerValueShapes.ReadWindowInt(30f, out _).ShouldBeFalse();
    }

    [Theory]
    [InlineData(30)]
    [InlineData(30L)]
    [InlineData(30.0)]
    [InlineData("30")]
    public void WindowReadsEveryOtherWholeNumber(object value)
    {
        TriggerValueShapes.ReadWindowInt(value, out var minutes).ShouldBeTrue();
        minutes.ShouldBe(30);
    }

    [Fact]
    public void WindowRefusesAFractionAndTextThatIsNotANumber()
    {
        TriggerValueShapes.ReadWindowInt(30.5, out _).ShouldBeFalse();
        TriggerValueShapes.ReadWindowInt("thirty", out _).ShouldBeFalse();
        TriggerValueShapes.ReadWindowInt(null, out _).ShouldBeFalse();
    }

    [Fact]
    public void AStoredTriggersOwnValidationReadsOnlyAnIntOrItsText()
    {
        TriggerValueShapes.ReadNarrowInt(30, out _).ShouldBeTrue();
        TriggerValueShapes.ReadNarrowInt("30", out _).ShouldBeTrue();
        TriggerValueShapes.ReadNarrowInt(30L, out _).ShouldBeFalse();
        TriggerValueShapes.ReadNarrowInt(30.0, out _).ShouldBeFalse();
    }
}
