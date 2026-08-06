using Fdw.Services.Pipelines.Abstractions.Output;
using Shouldly;
using Xunit;

namespace Fdw.Services.Pipelines.Abstractions.Tests.Output;

public class ColumnDisposalTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void DropCreatesDropColumnsDisposal()
    {
        var disposal = ColumnDisposal.Drop("col1", "col2", "col3");

        disposal.DropColumns.ShouldContain("col1");
        disposal.DropColumns.ShouldContain("col2");
        disposal.DropColumns.ShouldContain("col3");
        disposal.UseKeepList.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void KeepOnlyCreatesKeepColumnsDisposal()
    {
        var disposal = ColumnDisposal.KeepOnly("col1", "col2");

        disposal.KeepColumns.ShouldContain("col1");
        disposal.KeepColumns.ShouldContain("col2");
        disposal.UseKeepList.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AutoCreatesAutoDisposeDisposal()
    {
        var disposal = ColumnDisposal.Auto();

        disposal.AutoDispose.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void CloneCreatesDeepCopy()
    {
        var original = ColumnDisposal.Drop("col1", "col2");
        original.KeepColumns.Add("col3");
        original.AutoDispose = false;

        var clone = original.Clone();

        clone.ShouldNotBeSameAs(original);
        clone.DropColumns.ShouldNotBeSameAs(original.DropColumns);
        clone.DropColumns.ShouldContain("col1");
        clone.DropColumns.ShouldContain("col2");
        clone.KeepColumns.ShouldNotBeSameAs(original.KeepColumns);
        clone.KeepColumns.ShouldContain("col3");
        clone.UseKeepList.ShouldBe(original.UseKeepList);
        clone.AutoDispose.ShouldBe(original.AutoDispose);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EqualsReturnsTrueForIdenticalDisposals()
    {
        var disposal1 = ColumnDisposal.Drop("col1");
        var disposal2 = ColumnDisposal.Drop("col1");

        disposal1.Equals(disposal2).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EqualsReturnsFalseForDifferentUseKeepList()
    {
        var disposal1 = ColumnDisposal.Drop("col1");
        var disposal2 = ColumnDisposal.KeepOnly("col1");

        disposal1.Equals(disposal2).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EqualsReturnsFalseForDifferentAutoDispose()
    {
        var disposal1 = new ColumnDisposal { AutoDispose = true };
        var disposal2 = new ColumnDisposal { AutoDispose = false };

        disposal1.Equals(disposal2).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EqualsReturnsFalseForNull()
    {
        var disposal = ColumnDisposal.Auto();

        disposal.Equals(null).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EqualsReturnsTrueForSameInstance()
    {
        var disposal = ColumnDisposal.Auto();

        disposal.Equals(disposal).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EqualsObjectReturnsTrueForIdenticalDisposals()
    {
        var disposal1 = ColumnDisposal.Drop("col1");
        object disposal2 = ColumnDisposal.Drop("col1");

        disposal1.Equals(disposal2).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EqualsObjectReturnsFalseForNull()
    {
        var disposal = ColumnDisposal.Auto();

        disposal.Equals((object?)null).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EqualsObjectReturnsFalseForDifferentType()
    {
        var disposal = ColumnDisposal.Auto();

        disposal.Equals(new object()).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void GetHashCodeReturnsSameValueForEqualDisposals()
    {
        var disposal1 = new ColumnDisposal { UseKeepList = true, AutoDispose = false };
        var disposal2 = new ColumnDisposal { UseKeepList = true, AutoDispose = false };

        disposal1.GetHashCode().ShouldBe(disposal2.GetHashCode());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void GetHashCodeReturnsDifferentValuesForDifferentDisposals()
    {
        var disposal1 = new ColumnDisposal { UseKeepList = true };
        var disposal2 = new ColumnDisposal { UseKeepList = false };

        disposal1.GetHashCode().ShouldNotBe(disposal2.GetHashCode());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void DefaultConstructorCreatesValidInstance()
    {
        var disposal = new ColumnDisposal();

        disposal.DropColumns.ShouldNotBeNull();
        disposal.KeepColumns.ShouldNotBeNull();
        disposal.UseKeepList.ShouldBeFalse();
        disposal.AutoDispose.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void DropColumnsCanBeModified()
    {
        var disposal = new ColumnDisposal();
        disposal.DropColumns.Add("col1");
        disposal.DropColumns.Add("col2");

        disposal.DropColumns.Count.ShouldBe(2);
        disposal.DropColumns.ShouldContain("col1");
        disposal.DropColumns.ShouldContain("col2");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void KeepColumnsCanBeModified()
    {
        var disposal = new ColumnDisposal();
        disposal.KeepColumns.Add("col1");
        disposal.KeepColumns.Add("col2");

        disposal.KeepColumns.Count.ShouldBe(2);
        disposal.KeepColumns.ShouldContain("col1");
        disposal.KeepColumns.ShouldContain("col2");
    }
}
