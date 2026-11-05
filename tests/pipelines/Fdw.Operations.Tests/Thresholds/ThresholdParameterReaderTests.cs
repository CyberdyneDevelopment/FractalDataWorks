using Fdw.Operations.Abstractions.Thresholds;
using Fdw.Operations.Configuration;
using Fdw.Operations.Thresholds;

namespace Fdw.Operations.Tests.Thresholds;

/// <summary>Tests for reading a threshold's parameters (FDW-3935): they are checked against what the type declares and refused, not repaired, when they do not fit.</summary>
public sealed class ThresholdParameterReaderTests
{
    private sealed class RangeType : ThresholdTypeBase
    {
        public RangeType() : base("TestRange", "A range check in a test.")
        {
        }

        public override string SubjectKind => "DataSet";

        public override IReadOnlyList<ThresholdField> Fields { get; } = [new("violations", typeof(long), "rows", "Rows outside the range.")];

        public override IReadOnlyList<ThresholdParameterDeclaration> Parameters { get; } =
        [
            new("FieldName", typeof(string), true, false, "The field read."),
            new("MinValue", typeof(double), false, false, "The lowest allowed value."),
            new("AllowedValues", typeof(string), false, true, "The allowed values."),
        ];
    }

    private static ThresholdParameterConfiguration Row(string name, string? value, string dataType, int ordinal = 1) =>
        new() { Id = Guid.NewGuid(), Name = name, Value = value, DataType = dataType, Ordinal = ordinal };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void StatedParametersAreReadTypedAndAListKeepsItsOrder()
    {
        var read = ThresholdParameterReader.From(new RangeType(), "T", [Row("FieldName", "Age", "String"), Row("MinValue", "1.5", "Double"), Row("AllowedValues", "b", "String", 2), Row("AllowedValues", "a", "String", 1)]);

        read.IsSuccess.ShouldBeTrue();
        read.Value!["FieldName"].ShouldBe("Age");
        read.Value["MinValue"].ShouldBe(1.5);
        read.Value["AllowedValues"].ShouldBe(new List<object?> { "a", "b" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AParameterTheTypeDoesNotDeclareIsRefused()
    {
        ThresholdParameterReader.From(new RangeType(), "T", [Row("FieldName", "Age", "String"), Row("Nope", "1", "Int64")]).IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ARequiredParameterThatIsMissingIsRefused()
    {
        ThresholdParameterReader.From(new RangeType(), "T", [Row("MinValue", "1", "Double")]).IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AValueOfTheWrongTypeOrMissingOrStatedTwiceIsRefused()
    {
        ThresholdParameterReader.From(new RangeType(), "T", [Row("FieldName", "Age", "String"), Row("MinValue", "low", "Double")]).IsFailure.ShouldBeTrue();
        ThresholdParameterReader.From(new RangeType(), "T", [Row("FieldName", "Age", "Int64")]).IsFailure.ShouldBeTrue();
        ThresholdParameterReader.From(new RangeType(), "T", [Row("FieldName", null, "String")]).IsFailure.ShouldBeTrue();
        ThresholdParameterReader.From(new RangeType(), "T", [Row("FieldName", "a", "String", 1), Row("FieldName", "b", "String", 2)]).IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ATypeThatDeclaresNoParameterAcceptsNone()
    {
        ThresholdTypes.ByName("ServerWaits").Parameters.ShouldBeEmpty();
        ThresholdParameterReader.From(ThresholdTypes.ByName("ServerWaits"), "T", []).Value.ShouldBeEmpty();
    }
}
