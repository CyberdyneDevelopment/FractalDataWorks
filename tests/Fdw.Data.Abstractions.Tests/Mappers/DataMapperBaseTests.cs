using System.Data;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.Abstractions.Tests.Mappers;

public sealed class DataMapperBaseTests
{
    private readonly TestDataTypeConverter _sourceConverter = new(1, "Source");
    private readonly TestDataTypeConverter _targetConverter = new(2, "Target");

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsId()
    {
        // Arrange & Act
        var mapper = new TestDataMapper(_sourceConverter, _targetConverter);

        // Assert
        mapper.Id.ShouldBe("Source_to_Target");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsName()
    {
        // Arrange & Act
        var mapper = new TestDataMapper(_sourceConverter, _targetConverter);

        // Assert
        mapper.Name.ShouldBe("Source → Target");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SourceConverterIsSet()
    {
        // Arrange & Act
        var mapper = new TestDataMapper(_sourceConverter, _targetConverter);

        // Assert
        mapper.SourceConverter.ShouldBe(_sourceConverter);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TargetConverterIsSet()
    {
        // Arrange & Act
        var mapper = new TestDataMapper(_sourceConverter, _targetConverter);

        // Assert
        mapper.TargetConverter.ShouldBe(_targetConverter);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CanMapDefaultsToTrue()
    {
        // Arrange
        var mapper = new TestDataMapper(_sourceConverter, _targetConverter);

        // Act
        var canMap = mapper.CanMap;

        // Assert
        canMap.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MapViaClrPerformsTwoStepConversion()
    {
        // Arrange
        var mapper = new TestDataMapper(_sourceConverter, _targetConverter);
        var sourceValue = "test";

        // Act
        var result = mapper.MapViaClr(sourceValue);

        // Assert
        result.ShouldBe("TARGET_CLR_test");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MapViaClrHandlesNullInput()
    {
        // Arrange
        var mapper = new TestDataMapper(_sourceConverter, _targetConverter);

        // Act
        var result = mapper.MapViaClr(null);

        // Assert
        result.ShouldBe("TARGET_CLR_");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MapCallsAbstractImplementation()
    {
        // Arrange
        var mapper = new TestDataMapper(_sourceConverter, _targetConverter);
        var sourceValue = "test";

        // Act
        var result = mapper.Map(sourceValue);

        // Assert
        result.ShouldBe("MAPPED_test");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void InheritsFromTypeOptionBase()
    {
        // Arrange
        var mapper = new TestDataMapper(_sourceConverter, _targetConverter);

        // Act & Assert
        mapper.ShouldBeAssignableTo<DataMapperBase<TestDataTypeConverter, TestDataTypeConverter>>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ImplementsIDataMapper()
    {
        // Arrange
        var mapper = new TestDataMapper(_sourceConverter, _targetConverter);

        // Act & Assert
        mapper.ShouldBeAssignableTo<IDataMapper<TestDataTypeConverter, TestDataTypeConverter>>();
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestDataMapper : DataMapperBase<TestDataTypeConverter, TestDataTypeConverter>
    {
        public TestDataMapper(TestDataTypeConverter source, TestDataTypeConverter target)
            : base("Source_to_Target", "Source → Target", source, target)
        {
        }

        public override object? Map(object? sourceValue)
        {
            return $"MAPPED_{sourceValue}";
        }
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
