using Fdw.Services.Pipelines.Abstractions.Output;
using Shouldly;
using Xunit;

namespace Fdw.Services.Pipelines.Abstractions.Tests.Output;

public class OutputColumnTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void CloneCreatesDeepCopy()
    {
        var original = new OutputColumn
        {
            Name = "TestColumn",
            Alias = "TestAlias",
            DataType = "VARCHAR(50)",
            IsRequired = false
        };

        var clone = original.Clone();

        clone.ShouldNotBeSameAs(original);
        clone.Name.ShouldBe(original.Name);
        clone.Alias.ShouldBe(original.Alias);
        clone.DataType.ShouldBe(original.DataType);
        clone.IsRequired.ShouldBe(original.IsRequired);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EqualsReturnsTrueForIdenticalColumns()
    {
        var col1 = new OutputColumn { Name = "Col1", Alias = "Alias1", DataType = "INT", IsRequired = true };
        var col2 = new OutputColumn { Name = "Col1", Alias = "Alias1", DataType = "INT", IsRequired = true };

        col1.Equals(col2).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EqualsReturnsFalseForDifferentNames()
    {
        var col1 = new OutputColumn { Name = "Col1" };
        var col2 = new OutputColumn { Name = "Col2" };

        col1.Equals(col2).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EqualsReturnsFalseForDifferentAliases()
    {
        var col1 = new OutputColumn { Name = "Col1", Alias = "Alias1" };
        var col2 = new OutputColumn { Name = "Col1", Alias = "Alias2" };

        col1.Equals(col2).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EqualsReturnsFalseForDifferentDataTypes()
    {
        var col1 = new OutputColumn { Name = "Col1", DataType = "INT" };
        var col2 = new OutputColumn { Name = "Col1", DataType = "VARCHAR" };

        col1.Equals(col2).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EqualsReturnsFalseForDifferentIsRequired()
    {
        var col1 = new OutputColumn { Name = "Col1", IsRequired = true };
        var col2 = new OutputColumn { Name = "Col1", IsRequired = false };

        col1.Equals(col2).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EqualsReturnsFalseForNull()
    {
        var column = new OutputColumn { Name = "Test" };

        column.Equals(null).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EqualsReturnsTrueForSameInstance()
    {
        var column = new OutputColumn { Name = "Test" };

        column.Equals(column).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EqualsObjectReturnsTrueForIdenticalColumns()
    {
        var col1 = new OutputColumn { Name = "Col1" };
        object col2 = new OutputColumn { Name = "Col1" };

        col1.Equals(col2).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EqualsObjectReturnsFalseForNull()
    {
        var column = new OutputColumn { Name = "Test" };

        column.Equals((object?)null).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EqualsObjectReturnsFalseForDifferentType()
    {
        var column = new OutputColumn { Name = "Test" };

        column.Equals(new object()).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void GetHashCodeReturnsSameValueForEqualColumns()
    {
        var col1 = new OutputColumn { Name = "Col1", Alias = "Alias1", DataType = "INT", IsRequired = true };
        var col2 = new OutputColumn { Name = "Col1", Alias = "Alias1", DataType = "INT", IsRequired = true };

        col1.GetHashCode().ShouldBe(col2.GetHashCode());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void GetHashCodeReturnsDifferentValuesForDifferentColumns()
    {
        var col1 = new OutputColumn { Name = "Col1" };
        var col2 = new OutputColumn { Name = "Col2" };

        col1.GetHashCode().ShouldNotBe(col2.GetHashCode());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void DefaultConstructorCreatesValidInstance()
    {
        var column = new OutputColumn();

        column.Name.ShouldBe(string.Empty);
        column.Alias.ShouldBeNull();
        column.DataType.ShouldBeNull();
        column.IsRequired.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AllPropertiesCanBeSetAndRetrieved()
    {
        var column = new OutputColumn
        {
            Name = "TestColumn",
            Alias = "TestAlias",
            DataType = "VARCHAR(100)",
            IsRequired = false
        };

        column.Name.ShouldBe("TestColumn");
        column.Alias.ShouldBe("TestAlias");
        column.DataType.ShouldBe("VARCHAR(100)");
        column.IsRequired.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void GetHashCodeHandlesNullValues()
    {
        var column = new OutputColumn
        {
            Name = null!,
            Alias = null,
            DataType = null
        };

        var hashCode = column.GetHashCode();

        hashCode.ShouldNotBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void GetHashCodeHandlesNonNullValues()
    {
        var column = new OutputColumn
        {
            Name = "TestColumn",
            Alias = "TestAlias",
            DataType = "VARCHAR(100)"
        };

        var hashCode = column.GetHashCode();

        hashCode.ShouldNotBe(0);
    }
}
