using System.Data;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.Abstractions.Tests.Mappers;

public sealed class DefaultDataMapperTests
{
    private readonly TestDataTypeConverter _sourceConverter = new(1, "Source");
    private readonly TestDataTypeConverter _targetConverter = new(2, "Target");

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorCreatesIdFromConverterNames()
    {
        // Arrange & Act
        var mapper = new DefaultDataMapper<TestDataTypeConverter, TestDataTypeConverter>(
            _sourceConverter,
            _targetConverter);

        // Assert
        mapper.Id.ShouldBe("Default_Source_to_Target");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorCreatesNameFromConverterNames()
    {
        // Arrange & Act
        var mapper = new DefaultDataMapper<TestDataTypeConverter, TestDataTypeConverter>(
            _sourceConverter,
            _targetConverter);

        // Assert
        mapper.Name.ShouldBe("Source → Target");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MapDelegatesToMapViaClr()
    {
        // Arrange
        var mapper = new DefaultDataMapper<TestDataTypeConverter, TestDataTypeConverter>(
            _sourceConverter,
            _targetConverter);
        var sourceValue = "test";

        // Act
        var result = mapper.Map(sourceValue);

        // Assert
        result.ShouldBe("TARGET_CLR_test");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MapHandlesNullInput()
    {
        // Arrange
        var mapper = new DefaultDataMapper<TestDataTypeConverter, TestDataTypeConverter>(
            _sourceConverter,
            _targetConverter);

        // Act
        var result = mapper.Map(null);

        // Assert
        result.ShouldBe("TARGET_CLR_");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MapPerformsTwoStepConversion()
    {
        // Arrange
        var mapper = new DefaultDataMapper<TestDataTypeConverter, TestDataTypeConverter>(
            _sourceConverter,
            _targetConverter);
        var sourceValue = "value";

        // Act
        var result = mapper.Map(sourceValue);

        // Assert - Should go through ToClr then ToDb
        result.ShouldNotBeNull();
        result.ToString()!.ShouldContain("TARGET");
        result.ToString()!.ShouldContain("CLR");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void InheritsFromDataMapperBase()
    {
        // Arrange
        var mapper = new DefaultDataMapper<TestDataTypeConverter, TestDataTypeConverter>(
            _sourceConverter,
            _targetConverter);

        // Act & Assert
        mapper.ShouldBeAssignableTo<DataMapperBase<TestDataTypeConverter, TestDataTypeConverter>>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SourceConverterIsSet()
    {
        // Arrange & Act
        var mapper = new DefaultDataMapper<TestDataTypeConverter, TestDataTypeConverter>(
            _sourceConverter,
            _targetConverter);

        // Assert
        mapper.SourceConverter.ShouldBe(_sourceConverter);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TargetConverterIsSet()
    {
        // Arrange & Act
        var mapper = new DefaultDataMapper<TestDataTypeConverter, TestDataTypeConverter>(
            _sourceConverter,
            _targetConverter);

        // Assert
        mapper.TargetConverter.ShouldBe(_targetConverter);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CanMapIsTrue()
    {
        // Arrange
        var mapper = new DefaultDataMapper<TestDataTypeConverter, TestDataTypeConverter>(
            _sourceConverter,
            _targetConverter);

        // Act & Assert
        mapper.CanMap.ShouldBeTrue();
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestDataTypeConverter : DataTypeConverterBase
    {
        public TestDataTypeConverter(int id, string name)
            : base(id, name, "test", typeof(string), DbType.String)
        {
        }

        public override object? ToClr(object? dbValue)
        {
            return $"CLR_{dbValue}";
        }

        public override object? ToDb(object? clrValue)
        {
            return $"TARGET_{clrValue}";
        }
    }
}
