using System.Linq;
using System.Threading.Tasks;
using Fdw.Services.Etl.Flow;
using static Fdw.Services.Etl.Tests.Flow.FlowTestKit;

namespace Fdw.Services.Etl.Tests.Flow;

/// <summary>
/// Tests for the types a Map mapping converts to (FDW-2009): the platform data types by their exact names, a value read through the target type,
/// and a value the target cannot read refused rather than invented.
/// </summary>
public sealed class FieldTypeConversionTests
{
    private readonly FlowExecutor _sut = new();

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("Int32", true)]
    [InlineData("String", true)]
    [InlineData("int", false)]
    [InlineData("boolean", false)]
    [InlineData("money", false)]
    public void OnlyTheExactNameOfADataTypeIsSupported(string name, bool supported)
    {
        FieldTypeConversion.IsSupported(name).ShouldBe(supported);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TextIsReadAsTheTargetTypeReadsIt()
    {
        FieldTypeConversion.TryConvert("42", "Int32", out var converted, out var reason).ShouldBeTrue(reason);

        converted.ShouldBe(42);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ANumberOfAnotherTypeIsReadThroughItsText()
    {
        FieldTypeConversion.TryConvert(7, "Int64", out var converted, out var reason).ShouldBeTrue(reason);

        converted.ShouldBe(7L);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AValueAlreadyOfTheTypeIsKept()
    {
        FieldTypeConversion.TryConvert("abc", "String", out var converted, out _).ShouldBeTrue();

        converted.ShouldBe("abc");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AFractionIsRefusedNotRoundedIntoAWholeNumberType()
    {
        FieldTypeConversion.TryConvert(12.5m, "Int32", out var converted, out var reason).ShouldBeFalse();

        converted.ShouldBeNull();
        reason.ShouldNotBeNull().ShouldContain("Int32");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ANameThatIsNotADataTypeIsRefused()
    {
        FieldTypeConversion.TryConvert("1", "int", out _, out var reason).ShouldBeFalse();

        reason.ShouldNotBeNull().ShouldContain("int");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AMapConvertsADefaultAndAMissingSourceTakesIt()
    {
        var (sink, rows) = Sink("dst");
        var conversion = new FlowFieldConversion("Int32", "7", false, []);
        var graph = Graph(
            [Source("src", 3), Mapped("map", FlowFieldMap.From("Out", "Missing").WithConversion(conversion)), sink],
            [Link("src", "map"), Link("map", "dst")]);

        var result = await _sut.Run(graph, Options(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        rows.Select(row => row["Out"]).ShouldAllBe(value => value is int && (int)value == 7);
        rows.Count.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARequiredMappingWithNoValueFailsTheRunWhenThereIsNoErrorOutput()
    {
        var (sink, rows) = Sink("dst");
        var conversion = new FlowFieldConversion(null, null, true, []);
        var graph = Graph(
            [Source("src", 3), Mapped("map", FlowFieldMap.Of("Out", null).WithConversion(conversion)), sink],
            [Link("src", "map"), Link("map", "dst")]);

        var result = await _sut.Run(graph, Options(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        rows.ShouldBeEmpty();
    }
}
