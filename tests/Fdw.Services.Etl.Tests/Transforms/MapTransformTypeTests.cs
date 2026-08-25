using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Transforms;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Transforms;

/// <summary>
/// Tests for <see cref="MapTransformType"/> — the field-mapping / type-conversion engine.
/// This is the highest-risk transform for silent data corruption: every <c>TargetType</c>
/// conversion branch, the named-transformer seam, the default-value/required-field branches,
/// and null handling are asserted against EXACT output values so a regression fails loud.
/// </summary>
public sealed class MapTransformTypeTests
{
    private readonly MapTransformType _sut = new();

    private static TransformContext CreateContext() =>
        new(Guid.NewGuid(), NullLogger.Instance, new Dictionary<string, object?>());

    private static PipelineTransformConfiguration CreateConfig(params PipelineTransformFieldMappingConfiguration[] mappings) =>
        new() { Id = Guid.NewGuid(), Name = "Map1", OperationType = "Map", FieldMappings = [.. mappings] };

    private static PipelineTransformFieldMappingConfiguration CreateMapping(
        string sourceField,
        string destinationField,
        string? targetType = null,
        string? defaultValue = null,
        bool isRequired = false,
        bool isEnabled = true,
        string? transformExpression = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            SourceField = sourceField,
            DestinationField = destinationField,
            TargetType = targetType,
            DefaultValue = defaultValue,
            IsRequired = isRequired,
            IsEnabled = isEnabled,
            TransformExpression = transformExpression,
        };

    // ── Pass-through / structural branches ──────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task TransformPassesThroughAllFieldsWhenConfigurationIsNotPipelineTransformConfiguration()
    {
        // Arrange
        var input = new Dictionary<string, object?> { ["A"] = 1, ["B"] = "x" };
        var configuration = Mock.Of<IGenericConfiguration>();

        // Act
        var result = await _sut.Transform(input, configuration, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeSameAs(input);
        result.Value!["A"].ShouldBe(1);
        result.Value!["B"].ShouldBe("x");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task TransformPassesThroughAllFieldsWhenFieldMappingsIsEmpty()
    {
        // Arrange
        var input = new Dictionary<string, object?> { ["A"] = 1 };

        // Act
        var result = await _sut.Transform(input, CreateConfig(), CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!["A"].ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task TransformSkipsDisabledMapping()
    {
        // Arrange
        var input = new Dictionary<string, object?> { ["A"] = 1, ["B"] = 2 };
        var config = CreateConfig(
            CreateMapping("A", "A", isEnabled: false),
            CreateMapping("B", "B"));

        // Act
        var result = await _sut.Transform(input, config, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.Value!.ContainsKey("A").ShouldBeFalse();
        result.Value!["B"].ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task TransformRenamesFieldToDestination()
    {
        // Arrange — output uses an OrdinalIgnoreCase dictionary, so the destination name must differ
        // by more than case from the source name or the two keys collide and "rename" is untestable.
        var input = new Dictionary<string, object?> { ["first"] = "John" };
        var config = CreateConfig(CreateMapping("first", "FirstName"));

        // Act
        var result = await _sut.Transform(input, config, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.Value!.ContainsKey("first").ShouldBeFalse();
        result.Value!["FirstName"].ShouldBe("John");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task TransformDefaultsDestinationFieldToSourceFieldWhenDestinationIsNull()
    {
        // Arrange — DestinationField is a non-nullable string on the POCO but the source can
        // still hand back a runtime null (e.g. via a forgiving cast off a DB-mapped row); the
        // `?? sourceField` fallback in MapField covers that case.
        var input = new Dictionary<string, object?> { ["first"] = "John" };
        var mapping = CreateMapping("first", "first");
        mapping.DestinationField = null!;
        var config = CreateConfig(mapping);

        // Act
        var result = await _sut.Transform(input, config, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.Value!["first"].ShouldBe("John");
    }

    // ── Default value / required field branches ─────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformAppliesDefaultValueWhenSourceValueIsNull()
    {
        // Arrange
        var input = new Dictionary<string, object?> { ["A"] = null };
        var config = CreateConfig(CreateMapping("A", "B", defaultValue: "fallback"));

        // Act
        var result = await _sut.Transform(input, config, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.Value!["B"].ShouldBe("fallback");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformAppliesDefaultValueWhenSourceFieldMissing()
    {
        // Arrange
        var input = new Dictionary<string, object?>();
        var config = CreateConfig(CreateMapping("A", "B", defaultValue: "fallback"));

        // Act
        var result = await _sut.Transform(input, config, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.Value!["B"].ShouldBe("fallback");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformConvertsDefaultValueToTargetTypeWhenSourceMissing()
    {
        // Arrange
        var input = new Dictionary<string, object?>();
        var config = CreateConfig(CreateMapping("A", "B", targetType: "int", defaultValue: "42"));

        // Act
        var result = await _sut.Transform(input, config, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.Value!["B"].ShouldBe(42);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformReportsErrorWhenRequiredFieldMissingWithoutDefault()
    {
        // Arrange
        var input = new Dictionary<string, object?>();
        var config = CreateConfig(CreateMapping("A", "B", isRequired: true));
        var context = CreateContext();

        // Act
        var result = await _sut.Transform(input, config, context, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.ContainsKey("B").ShouldBeFalse();
        context.Errors.Count.ShouldBe(1);
        context.Errors[0].Message.ShouldContain("Required field 'A' is missing");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task TransformSkipsFieldWhenSourceMissingNotRequiredAndNoDefault()
    {
        // Arrange
        var input = new Dictionary<string, object?>();
        var config = CreateConfig(CreateMapping("A", "B"));
        var context = CreateContext();

        // Act
        var result = await _sut.Transform(input, config, context, TestContext.Current.CancellationToken);

        // Assert
        result.Value!.ContainsKey("B").ShouldBeFalse();
        context.Errors.ShouldBeEmpty();
    }

    // ── Type conversion: success branches (silent-corruption risk) ─────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformConvertsAllSupportedTargetTypesFromStringSource()
    {
        // Arrange
        var input = new Dictionary<string, object?>
        {
            ["sString"] = 123,
            ["sInt"] = "123",
            ["sLong"] = "123",
            ["sDecimal"] = "123.45",
            ["sDouble"] = "123.45",
            ["sFloat"] = "123.45",
            ["sBool"] = "true",
        };
        var config = CreateConfig(
            CreateMapping("sString", "dString", targetType: "string"),
            CreateMapping("sInt", "dInt", targetType: "int"),
            CreateMapping("sLong", "dLong", targetType: "int64"),
            CreateMapping("sDecimal", "dDecimal", targetType: "decimal"),
            CreateMapping("sDouble", "dDouble", targetType: "float64"),
            CreateMapping("sFloat", "dFloat", targetType: "single"),
            CreateMapping("sBool", "dBool", targetType: "boolean"));

        // Act
        var result = await _sut.Transform(input, config, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.Value!["dString"].ShouldBe("123");
        result.Value!["dInt"].ShouldBe(123);
        result.Value!["dLong"].ShouldBe(123L);
        result.Value!["dDecimal"].ShouldBe(123.45m);
        result.Value!["dDouble"].ShouldBe(123.45d);
        result.Value!["dFloat"].ShouldBe(123.45f);
        result.Value!["dBool"].ShouldBe(true);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformConvertsNativeNumericTypesViaCheckedCast()
    {
        // Arrange
        var input = new Dictionary<string, object?>
        {
            ["fromLong"] = 100L,
            ["fromDouble"] = 100.0,
            ["fromDecimal"] = 100m,
        };
        var config = CreateConfig(
            CreateMapping("fromLong", "toInt", targetType: "int32"),
            CreateMapping("fromDouble", "toLong", targetType: "long"),
            CreateMapping("fromDecimal", "toDouble", targetType: "double"));

        // Act
        var result = await _sut.Transform(input, config, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.Value!["toInt"].ShouldBe(100);
        result.Value!["toLong"].ShouldBe(100L);
        result.Value!["toDouble"].ShouldBe(100.0d);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformConvertsUsingConvertFallbackForUnhandledSourceTypes()
    {
        // Arrange — a `short`/`byte` source isn't matched by any explicit switch arm on
        // ConvertToInt32/ConvertToBoolean, so it must fall through to the Convert.ToXxx default.
        var input = new Dictionary<string, object?>
        {
            ["sShort"] = (short)7,
            ["bByte"] = (byte)1,
        };
        var config = CreateConfig(
            CreateMapping("sShort", "dInt", targetType: "int"),
            CreateMapping("bByte", "dBool", targetType: "bool"));

        // Act
        var result = await _sut.Transform(input, config, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.Value!["dInt"].ShouldBe(7);
        result.Value!["dBool"].ShouldBe(true);
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("yes", true)]
    [InlineData("no", false)]
    [InlineData("1", true)]
    [InlineData("0", false)]
    [InlineData("true", true)]
    [InlineData("false", false)]
    public async Task TransformBooleanConversionHandlesSpecialStringValues(string raw, bool expected)
    {
        // Arrange
        var input = new Dictionary<string, object?> { ["A"] = raw };
        var config = CreateConfig(CreateMapping("A", "B", targetType: "bool"));

        // Act
        var result = await _sut.Transform(input, config, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.Value!["B"].ShouldBe(expected);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformDateTimeConversionHandlesAllSupportedFormats()
    {
        // Arrange
        var input = new Dictionary<string, object?>
        {
            ["iso"] = "2024-03-15T10:30:00",
            ["dateOnly"] = "2024-03-15",
            ["fromOffset"] = new DateTimeOffset(2024, 3, 15, 10, 30, 0, TimeSpan.Zero),
            ["fromTicks"] = new DateTime(2024, 3, 15, 0, 0, 0, DateTimeKind.Utc).Ticks,
        };
        var config = CreateConfig(
            CreateMapping("iso", "dIso", targetType: "datetime"),
            CreateMapping("dateOnly", "dDateOnly", targetType: "date"),
            CreateMapping("fromOffset", "dFromOffset", targetType: "datetime"),
            CreateMapping("fromTicks", "dFromTicks", targetType: "datetime"));

        // Act
        var result = await _sut.Transform(input, config, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.Value!["dIso"].ShouldBe(new DateTime(2024, 3, 15, 10, 30, 0));
        result.Value!["dDateOnly"].ShouldBe(new DateTime(2024, 3, 15));
        result.Value!["dFromOffset"].ShouldBe(new DateTime(2024, 3, 15, 10, 30, 0, DateTimeKind.Utc));
        ((DateTime)result.Value!["dFromTicks"]!).Date.ShouldBe(new DateTime(2024, 3, 15));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformDateTimeConversionOfZSuffixedIsoStringIsDeterministicUtcRegardlessOfHostTimeZone()
    {
        // Why: regression guard for the fixed ConvertToDateTime arm ordering (MapTransformType.cs
        // ~289-310). The exact 'Z'-suffixed `TryParseExact(..., "yyyy-MM-ddTHH:mm:ssZ", ...,
        // DateTimeStyles.AdjustToUniversal, ...)` arm is now checked BEFORE the generic
        // `DateTime.TryParse(..., DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, ...)`
        // fallback, so a "Z"-suffixed timestamp always yields the same wall-clock value with Kind=Utc —
        // never the host-local, non-deterministic Kind that a plain DateTimeStyles.None parse would
        // produce. This host runs America/Chicago (CDT, -05:00); asserting both the value AND
        // DateTimeKind.Utc here would fail under the old bug on this very host.
        var input = new Dictionary<string, object?> { ["z"] = "2024-03-15T10:30:00Z" };
        var config = CreateConfig(CreateMapping("z", "dZ", targetType: "datetime"));

        // Act
        var result = await _sut.Transform(input, config, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        var converted = (DateTime)result.Value!["dZ"]!;
        converted.ShouldBe(new DateTime(2024, 3, 15, 10, 30, 0, DateTimeKind.Utc));
        converted.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformGuidConversionHandlesStringAndByteArraySources()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var input = new Dictionary<string, object?>
        {
            ["fromString"] = guid.ToString(),
            ["fromBytes"] = guid.ToByteArray(),
        };
        var config = CreateConfig(
            CreateMapping("fromString", "dFromString", targetType: "guid"),
            CreateMapping("fromBytes", "dFromBytes", targetType: "uuid"));

        // Act
        var result = await _sut.Transform(input, config, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.Value!["dFromString"].ShouldBe(guid);
        result.Value!["dFromBytes"].ShouldBe(guid);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformByteArrayConversionDecodesBase64String()
    {
        // Arrange
        var bytes = new byte[] { 1, 2, 3, 4 };
        var input = new Dictionary<string, object?> { ["A"] = Convert.ToBase64String(bytes) };
        var config = CreateConfig(CreateMapping("A", "B", targetType: "byte[]"));

        // Act
        var result = await _sut.Transform(input, config, CreateContext(), TestContext.Current.CancellationToken);

        // Assert
        result.Value!["B"].ShouldBe(bytes);
    }

    // ── Type conversion: failure branches (value preserved, error reported) ────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformLeavesValueUnconvertedAndReportsErrorWhenGuidConversionFails()
    {
        // Arrange
        var input = new Dictionary<string, object?> { ["A"] = "not-a-guid" };
        var config = CreateConfig(CreateMapping("A", "B", targetType: "guid"));
        var context = CreateContext();

        // Act
        var result = await _sut.Transform(input, config, context, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!["B"].ShouldBe("not-a-guid");
        context.Errors.Count.ShouldBe(1);
        context.Errors[0].Message.ShouldContain("Type conversion failed for field 'A' to type 'guid'");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformLeavesValueUnconvertedAndReportsErrorWhenByteArrayConversionFails()
    {
        // Arrange
        var input = new Dictionary<string, object?> { ["A"] = "not-base64!!" };
        var config = CreateConfig(CreateMapping("A", "B", targetType: "binary"));
        var context = CreateContext();

        // Act
        var result = await _sut.Transform(input, config, context, TestContext.Current.CancellationToken);

        // Assert
        result.Value!["B"].ShouldBe("not-base64!!");
        context.Errors.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformLeavesValueUnconvertedAndReportsErrorWhenDateTimeConversionFails()
    {
        // Arrange — an int source matches no DateTime switch arm and Convert.ToDateTime(int) throws.
        var input = new Dictionary<string, object?> { ["A"] = 12345 };
        var config = CreateConfig(CreateMapping("A", "B", targetType: "datetime"));
        var context = CreateContext();

        // Act
        var result = await _sut.Transform(input, config, context, TestContext.Current.CancellationToken);

        // Assert
        result.Value!["B"].ShouldBe(12345);
        context.Errors.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TransformReportsErrorForUnsupportedTargetType()
    {
        // Arrange
        var input = new Dictionary<string, object?> { ["A"] = "value" };
        var config = CreateConfig(CreateMapping("A", "B", targetType: "not-a-real-type"));
        var context = CreateContext();

        // Act
        var result = await _sut.Transform(input, config, context, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!["B"].ShouldBe("value");
        context.Errors.Count.ShouldBe(1);
        context.Errors[0].Message.ShouldContain("Type conversion failed for field 'A' to type 'not-a-real-type'");
    }

    // ── Named transformer seam (TransformExpression) ────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task TransformSkipsNamedTransformerWhenValueIsNull()
    {
        // Arrange
        var input = new Dictionary<string, object?> { ["A"] = null };
        var config = CreateConfig(CreateMapping("A", "B", transformExpression: "AnyTransformer"));
        var context = CreateContext();

        // Act
        var result = await _sut.Transform(input, config, context, TestContext.Current.CancellationToken);

        // Assert
        result.Value!.ContainsKey("B").ShouldBeTrue();
        result.Value!["B"].ShouldBeNull();
        context.Errors.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task TransformReportsErrorWhenNamedTransformerIsNotRegistered()
    {
        // Arrange — Fdw.Data.DataSets (which holds the concrete TransformationTypes options)
        // is not referenced by this test assembly, so any name is unregistered by construction.
        var input = new Dictionary<string, object?> { ["A"] = "raw" };
        var config = CreateConfig(CreateMapping("A", "B", transformExpression: "NotRegisteredTransformerXyz"));
        var context = CreateContext();

        // Act
        var result = await _sut.Transform(input, config, context, TestContext.Current.CancellationToken);

        // Assert
        result.Value!["B"].ShouldBe("raw");
        context.Errors.Count.ShouldBe(1);
        context.Errors[0].Message.ShouldContain("'NotRegisteredTransformerXyz' is not a registered DataTransformerType for field 'A'");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task TransformAppliesTargetTypeConversionAfterUnregisteredTransformerPassesValueThrough()
    {
        // Arrange
        var input = new Dictionary<string, object?> { ["A"] = "42" };
        var config = CreateConfig(CreateMapping("A", "B", targetType: "int", transformExpression: "NotRegistered"));
        var context = CreateContext();

        // Act
        var result = await _sut.Transform(input, config, context, TestContext.Current.CancellationToken);

        // Assert
        result.Value!["B"].ShouldBe(42);
        context.Errors.Count.ShouldBe(1);
    }

    // ── MapSpecToConfiguration: request-spec → typed config dispatch (FDW-556 Part 2.2) ─

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void MapSpecToConfigurationPopulatesFieldMappings()
    {
        // Arrange
        var spec = new FakeTransformOperationSpec
        {
            Name = "Map1",
            OperationType = "Map",
            FieldMappings = [CreateMapping("A", "B", targetType: "int")],
        };
        var target = new PipelineTransformConfiguration { Id = Guid.NewGuid(), Name = "Map1", OperationType = "Map" };

        // Act
        var result = _sut.MapSpecToConfiguration(spec, target, NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        target.FieldMappings.Count.ShouldBe(1);
        target.FieldMappings[0].SourceField.ShouldBe("A");
        target.FieldMappings[0].DestinationField.ShouldBe("B");
        target.FieldMappings[0].TargetType.ShouldBe("int");
        target.FieldMappings[0].PipelineTransformId.ShouldBe(target.Id);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void MapSpecToConfigurationFailsLoudWhenFieldMappingsEmpty()
    {
        // Arrange
        var spec = new FakeTransformOperationSpec { Name = "Map1", OperationType = "Map" };
        var target = new PipelineTransformConfiguration { Id = Guid.NewGuid(), Name = "Map1", OperationType = "Map" };

        // Act
        var result = _sut.MapSpecToConfiguration(spec, target, NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("ETL-11053");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void MapSpecToConfigurationFailsLoudWhenTargetIsWrongType()
    {
        // Arrange
        var spec = new FakeTransformOperationSpec
        {
            Name = "Map1",
            OperationType = "Map",
            FieldMappings = [CreateMapping("A", "B")],
        };
        var target = Mock.Of<IGenericConfiguration>();

        // Act
        var result = _sut.MapSpecToConfiguration(spec, target, NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("ETL-11052");
    }
}
