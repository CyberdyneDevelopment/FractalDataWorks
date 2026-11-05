using Fdw.Operations.Abstractions.Thresholds;
using Fdw.Services.Etl.Thresholds;

namespace Fdw.Services.Etl.Tests.Thresholds;

/// <summary>Tests for the threshold type of a component's input and output (FDW-3865): the type is in ThresholdTypes and reports the four figures with their types.</summary>
public sealed class ComponentIoThresholdTypeTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheTypeIsRegisteredUnderTheComponentSubject()
    {
        var type = ThresholdTypes.ByName("ComponentIo");

        ReferenceEquals(type, ThresholdTypes.NotFound).ShouldBeFalse();
        type.SubjectKind.ShouldBe("Component");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheTypeReportsRowsInRowsOutRejectedAsCountsAndRunTimeAsATimeSpan()
    {
        var fields = new ComponentIoThresholdType().Fields.ToDictionary(field => field.Name, field => field.ValueType);

        fields.ShouldBe(new Dictionary<string, Type>
        {
            ["rowsIn"] = typeof(long),
            ["rowsOut"] = typeof(long),
            ["rejected"] = typeof(long),
            ["runTime"] = typeof(TimeSpan),
        });
    }
}
