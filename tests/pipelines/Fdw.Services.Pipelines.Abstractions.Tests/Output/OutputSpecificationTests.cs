using Fdw.Services.Pipelines.Abstractions.Output;
using Shouldly;
using Xunit;

namespace Fdw.Services.Pipelines.Abstractions.Tests.Output;

public class OutputSpecificationTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AllCreatesOutputAllColumnsSpecification()
    {
        var spec = OutputSpecification.All();

        spec.OutputAllColumns.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void WithColumnsCreatesSpecificationWithColumns()
    {
        var spec = OutputSpecification.WithColumns("col1", "col2", "col3");

        spec.Columns.Count.ShouldBe(3);
        spec.Columns[0].Name.ShouldBe("col1");
        spec.Columns[1].Name.ShouldBe("col2");
        spec.Columns[2].Name.ShouldBe("col3");
        spec.OutputAllColumns.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void CloneCreatesDeepCopy()
    {
        var original = new OutputSpecification
        {
            OutputAllColumns = true,
            IncludeMetadata = true
        };
        original.Columns.Add(new OutputColumn { Name = "col1" });
        original.Columns.Add(new OutputColumn { Name = "col2" });

        var clone = original.Clone();

        clone.ShouldNotBeSameAs(original);
        clone.OutputAllColumns.ShouldBe(original.OutputAllColumns);
        clone.IncludeMetadata.ShouldBe(original.IncludeMetadata);
        clone.Columns.ShouldNotBeSameAs(original.Columns);
        clone.Columns.Count.ShouldBe(2);
        clone.Columns[0].Name.ShouldBe("col1");
        clone.Columns[1].Name.ShouldBe("col2");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EqualsReturnsTrueForIdenticalSpecifications()
    {
        var spec1 = new OutputSpecification
        {
            OutputAllColumns = true,
            IncludeMetadata = false
        };
        spec1.Columns.Add(new OutputColumn { Name = "col1" });

        var spec2 = new OutputSpecification
        {
            OutputAllColumns = true,
            IncludeMetadata = false
        };
        spec2.Columns.Add(new OutputColumn { Name = "col1" });

        spec1.Equals(spec2).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EqualsReturnsFalseForDifferentOutputAllColumns()
    {
        var spec1 = new OutputSpecification { OutputAllColumns = true };
        var spec2 = new OutputSpecification { OutputAllColumns = false };

        spec1.Equals(spec2).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EqualsReturnsFalseForDifferentIncludeMetadata()
    {
        var spec1 = new OutputSpecification { IncludeMetadata = true };
        var spec2 = new OutputSpecification { IncludeMetadata = false };

        spec1.Equals(spec2).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EqualsReturnsFalseForDifferentColumnCounts()
    {
        var spec1 = new OutputSpecification();
        spec1.Columns.Add(new OutputColumn { Name = "col1" });

        var spec2 = new OutputSpecification();
        spec2.Columns.Add(new OutputColumn { Name = "col1" });
        spec2.Columns.Add(new OutputColumn { Name = "col2" });

        spec1.Equals(spec2).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EqualsReturnsFalseForNull()
    {
        var spec = OutputSpecification.All();

        spec.Equals(null).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EqualsReturnsTrueForSameInstance()
    {
        var spec = OutputSpecification.All();

        spec.Equals(spec).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EqualsObjectReturnsTrueForIdenticalSpecifications()
    {
        var spec1 = OutputSpecification.All();
        object spec2 = OutputSpecification.All();

        spec1.Equals(spec2).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EqualsObjectReturnsFalseForNull()
    {
        var spec = OutputSpecification.All();

        spec.Equals((object?)null).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EqualsObjectReturnsFalseForDifferentType()
    {
        var spec = OutputSpecification.All();

        spec.Equals(new object()).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void GetHashCodeReturnsSameValueForEqualSpecifications()
    {
        var spec1 = new OutputSpecification { OutputAllColumns = true, IncludeMetadata = false };
        var spec2 = new OutputSpecification { OutputAllColumns = true, IncludeMetadata = false };

        spec1.GetHashCode().ShouldBe(spec2.GetHashCode());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void GetHashCodeReturnsDifferentValuesForDifferentSpecifications()
    {
        var spec1 = new OutputSpecification { OutputAllColumns = true };
        var spec2 = new OutputSpecification { OutputAllColumns = false };

        spec1.GetHashCode().ShouldNotBe(spec2.GetHashCode());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void DefaultConstructorCreatesValidInstance()
    {
        var spec = new OutputSpecification();

        spec.Columns.ShouldNotBeNull();
        spec.OutputAllColumns.ShouldBeFalse();
        spec.IncludeMetadata.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ColumnsCanBeModified()
    {
        var spec = new OutputSpecification();
        spec.Columns.Add(new OutputColumn { Name = "col1" });
        spec.Columns.Add(new OutputColumn { Name = "col2" });

        spec.Columns.Count.ShouldBe(2);
        spec.Columns[0].Name.ShouldBe("col1");
        spec.Columns[1].Name.ShouldBe("col2");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void OutputAllColumnsCanBeSetAndRetrieved()
    {
        var spec = new OutputSpecification { OutputAllColumns = true };

        spec.OutputAllColumns.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void IncludeMetadataCanBeSetAndRetrieved()
    {
        var spec = new OutputSpecification { IncludeMetadata = true };

        spec.IncludeMetadata.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void GetHashCodeIncludesColumnCount()
    {
        var spec1 = new OutputSpecification();
        spec1.Columns.Add(new OutputColumn { Name = "col1" });

        var spec2 = new OutputSpecification();
        spec2.Columns.Add(new OutputColumn { Name = "col1" });
        spec2.Columns.Add(new OutputColumn { Name = "col2" });

        spec1.GetHashCode().ShouldNotBe(spec2.GetHashCode());
    }
}
