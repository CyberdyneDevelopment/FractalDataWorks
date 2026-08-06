using Fdw.Services.Pipelines.Abstractions.DataSource;
using Shouldly;
using Xunit;

namespace Fdw.Services.Pipelines.Abstractions.Tests.DataSource;

[Collection(nameof(PipelinesTestCollection))]
public class DataSourceReferenceTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void FromConnectionCreatesConnectionReference()
    {
        var reference = DataSourceReference.FromConnection("TestConnection", "schema.table");

        reference.Kind.Name.ShouldBe("Connection");
        reference.Name.ShouldBe("TestConnection");
        reference.ContainerPath.ShouldBe("schema.table");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void FromConnectionWithoutContainerPathCreatesValidReference()
    {
        var reference = DataSourceReference.FromConnection("TestConnection");

        reference.Kind.Name.ShouldBe("Connection");
        reference.Name.ShouldBe("TestConnection");
        reference.ContainerPath.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void FromDataSetCreatesDataSetReference()
    {
        var reference = DataSourceReference.FromDataSet("TestDataSet");

        reference.Kind.Name.ShouldBe("DataSet");
        reference.Name.ShouldBe("TestDataSet");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void CloneCreatesDeepCopy()
    {
        var original = DataSourceReference.FromConnection("TestConnection", "schema.table");
        original.Alias = "TestAlias";
        original.Options = new Dictionary<string, object?> { { "key1", "value1" } };

        var clone = original.Clone();

        clone.ShouldNotBeSameAs(original);
        clone.Kind.Name.ShouldBe(original.Kind.Name);
        clone.Name.ShouldBe(original.Name);
        clone.ContainerPath.ShouldBe(original.ContainerPath);
        clone.Alias.ShouldBe(original.Alias);
        clone.Options.ShouldNotBeSameAs(original.Options);
        clone.Options.ShouldNotBeNull();
        clone.Options.ShouldContainKeyAndValue("key1", "value1");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void CloneWithNullOptionsCreatesValidCopy()
    {
        var original = DataSourceReference.FromConnection("TestConnection");
        original.Options = null;

        var clone = original.Clone();

        clone.Options.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EqualsReturnsTrueForIdenticalReferences()
    {
        var ref1 = DataSourceReference.FromConnection("TestConnection", "schema.table");
        ref1.Alias = "TestAlias";
        var ref2 = DataSourceReference.FromConnection("TestConnection", "schema.table");
        ref2.Alias = "TestAlias";

        ref1.Equals(ref2).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EqualsReturnsFalseForDifferentKinds()
    {
        var ref1 = DataSourceReference.FromConnection("Test");
        var ref2 = DataSourceReference.FromDataSet("Test");

        ref1.Equals(ref2).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EqualsReturnsFalseForDifferentNames()
    {
        var ref1 = DataSourceReference.FromConnection("Connection1");
        var ref2 = DataSourceReference.FromConnection("Connection2");

        ref1.Equals(ref2).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EqualsReturnsFalseForDifferentContainerPaths()
    {
        var ref1 = DataSourceReference.FromConnection("Test", "schema.table1");
        var ref2 = DataSourceReference.FromConnection("Test", "schema.table2");

        ref1.Equals(ref2).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EqualsReturnsFalseForDifferentAliases()
    {
        // Ensure TypeCollection is initialized
        _ = DataSourceKinds.All();

        var ref1 = DataSourceReference.FromConnection("Test");
        ref1.Alias = "Alias1";
        var ref2 = DataSourceReference.FromConnection("Test");
        ref2.Alias = "Alias2";

        ref1.Equals(ref2).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EqualsReturnsFalseForNull()
    {
        var reference = DataSourceReference.FromConnection("Test");

        reference.Equals(null).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EqualsReturnsTrueForSameInstance()
    {
        var reference = DataSourceReference.FromConnection("Test");

        reference.Equals(reference).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EqualsObjectReturnsTrueForIdenticalReferences()
    {
        var ref1 = DataSourceReference.FromConnection("TestConnection", "schema.table");
        object ref2 = DataSourceReference.FromConnection("TestConnection", "schema.table");

        ref1.Equals(ref2).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EqualsObjectReturnsFalseForNull()
    {
        var reference = DataSourceReference.FromConnection("Test");

        reference.Equals((object?)null).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EqualsObjectReturnsFalseForDifferentType()
    {
        var reference = DataSourceReference.FromConnection("Test");

        reference.Equals(new object()).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void GetHashCodeReturnsSameValueForEqualReferences()
    {
        var ref1 = DataSourceReference.FromConnection("TestConnection", "schema.table");
        var ref2 = DataSourceReference.FromConnection("TestConnection", "schema.table");

        ref1.GetHashCode().ShouldBe(ref2.GetHashCode());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void GetHashCodeReturnsDifferentValuesForDifferentReferences()
    {
        var ref1 = DataSourceReference.FromConnection("Connection1");
        var ref2 = DataSourceReference.FromConnection("Connection2");

        ref1.GetHashCode().ShouldNotBe(ref2.GetHashCode());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void DefaultConstructorCreatesValidInstance()
    {
        var reference = new DataSourceReference();

        reference.Kind.ShouldNotBeNull();
        reference.Name.ShouldBe(string.Empty);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AliasCanBeSetAndRetrieved()
    {
        var reference = DataSourceReference.FromConnection("Test");
        reference.Alias = "TestAlias";

        reference.Alias.ShouldBe("TestAlias");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void OptionsCanBeSetAndRetrieved()
    {
        var reference = DataSourceReference.FromConnection("Test");
        reference.Options = new Dictionary<string, object?> { { "option1", 123 }, { "option2", "value" } };

        reference.Options.ShouldContainKeyAndValue("option1", 123);
        reference.Options.ShouldContainKeyAndValue("option2", "value");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void GetHashCodeHandlesNullValues()
    {
        var reference = new DataSourceReference
        {
            Kind = DataSourceKinds.NotFound,
            Name = null!,
            ContainerPath = null,
            Alias = null
        };

        var hashCode = reference.GetHashCode();

        hashCode.ShouldNotBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void GetHashCodeHandlesNonNullValues()
    {
        var reference = DataSourceReference.FromConnection("Test", "schema.table");
        reference.Alias = "TestAlias";

        var hashCode = reference.GetHashCode();

        hashCode.ShouldNotBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void CloneWithNonNullOptionsCreatesDeepCopy()
    {
        var original = DataSourceReference.FromConnection("TestConnection", "schema.table");
        original.Alias = "TestAlias";
        original.Options = new Dictionary<string, object?> { { "key1", "value1" } };

        var clone = original.Clone();

        clone.Options.ShouldNotBeNull();
        clone.Options.ShouldNotBeSameAs(original.Options);
        clone.Options.ShouldContainKeyAndValue("key1", "value1");
    }
}
