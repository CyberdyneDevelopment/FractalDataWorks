using Fdw.Services.Pipelines.Abstractions.DataDestination;
using Fdw.Services.Pipelines.Abstractions.WriteMode;
using Shouldly;
using Xunit;

namespace Fdw.Services.Pipelines.Abstractions.Tests.DataDestination;

[Collection(nameof(PipelinesTestCollection))]
public class DataDestinationReferenceTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ToConnectionCreatesConnectionReference()
    {
        var reference = DataDestinationReference.ToConnection("TestConnection", "schema.table");

        reference.Kind.Name.ShouldBe("Connection");
        reference.Name.ShouldBe("TestConnection");
        reference.ContainerPath.ShouldBe("schema.table");
        reference.WriteMode.Name.ShouldBe("Insert");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ToConnectionWithoutContainerPathCreatesValidReference()
    {
        var reference = DataDestinationReference.ToConnection("TestConnection");

        reference.Kind.Name.ShouldBe("Connection");
        reference.Name.ShouldBe("TestConnection");
        reference.ContainerPath.ShouldBeNull();
        reference.WriteMode.Name.ShouldBe("Insert");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ToConnectionWithCustomWriteModeUsesProvidedMode()
    {
        var writeMode = WriteModes.ByName("Upsert");
        var reference = DataDestinationReference.ToConnection("TestConnection", "schema.table", writeMode);

        reference.WriteMode.Name.ShouldBe("Upsert");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ToDataSetCreatesDataSetReference()
    {
        var reference = DataDestinationReference.ToDataSet("TestDataSet");

        reference.Kind.Name.ShouldBe("DataSet");
        reference.Name.ShouldBe("TestDataSet");
        reference.ContainerPath.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void CloneCreatesDeepCopy()
    {
        var original = DataDestinationReference.ToConnection("TestConnection", "schema.table");
        original.Options = new Dictionary<string, object?> { { "key1", "value1" } };

        var clone = original.Clone();

        clone.ShouldNotBeSameAs(original);
        clone.Kind.Name.ShouldBe(original.Kind.Name);
        clone.Name.ShouldBe(original.Name);
        clone.ContainerPath.ShouldBe(original.ContainerPath);
        clone.WriteMode.Name.ShouldBe(original.WriteMode.Name);
        clone.Options.ShouldNotBeSameAs(original.Options);
        clone.Options.ShouldNotBeNull();
        clone.Options.ShouldContainKeyAndValue("key1", "value1");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void CloneWithNullOptionsCreatesValidCopy()
    {
        var original = DataDestinationReference.ToConnection("TestConnection");
        original.Options = null;

        var clone = original.Clone();

        clone.Options.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EqualsReturnsTrueForIdenticalReferences()
    {
        var ref1 = DataDestinationReference.ToConnection("TestConnection", "schema.table");
        var ref2 = DataDestinationReference.ToConnection("TestConnection", "schema.table");

        ref1.Equals(ref2).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EqualsReturnsFalseForDifferentKinds()
    {
        var ref1 = DataDestinationReference.ToConnection("Test");
        var ref2 = DataDestinationReference.ToDataSet("Test");

        ref1.Equals(ref2).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EqualsReturnsFalseForDifferentNames()
    {
        var ref1 = DataDestinationReference.ToConnection("Connection1");
        var ref2 = DataDestinationReference.ToConnection("Connection2");

        ref1.Equals(ref2).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EqualsReturnsFalseForDifferentContainerPaths()
    {
        var ref1 = DataDestinationReference.ToConnection("Test", "schema.table1");
        var ref2 = DataDestinationReference.ToConnection("Test", "schema.table2");

        ref1.Equals(ref2).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EqualsReturnsFalseForDifferentWriteModes()
    {
        var ref1 = DataDestinationReference.ToConnection("Test", writeMode: WriteModes.ByName("Insert"));
        var ref2 = DataDestinationReference.ToConnection("Test", writeMode: WriteModes.ByName("Upsert"));

        ref1.Equals(ref2).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EqualsReturnsFalseForNull()
    {
        var reference = DataDestinationReference.ToConnection("Test");

        reference.Equals(null).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EqualsReturnsTrueForSameInstance()
    {
        var reference = DataDestinationReference.ToConnection("Test");

        reference.Equals(reference).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EqualsObjectReturnsTrueForIdenticalReferences()
    {
        var ref1 = DataDestinationReference.ToConnection("TestConnection", "schema.table");
        object ref2 = DataDestinationReference.ToConnection("TestConnection", "schema.table");

        ref1.Equals(ref2).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EqualsObjectReturnsFalseForNull()
    {
        var reference = DataDestinationReference.ToConnection("Test");

        reference.Equals((object?)null).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EqualsObjectReturnsFalseForDifferentType()
    {
        var reference = DataDestinationReference.ToConnection("Test");

        reference.Equals(new object()).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void GetHashCodeReturnsSameValueForEqualReferences()
    {
        var ref1 = DataDestinationReference.ToConnection("TestConnection", "schema.table");
        var ref2 = DataDestinationReference.ToConnection("TestConnection", "schema.table");

        ref1.GetHashCode().ShouldBe(ref2.GetHashCode());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void GetHashCodeReturnsDifferentValuesForDifferentReferences()
    {
        var ref1 = DataDestinationReference.ToConnection("Connection1");
        var ref2 = DataDestinationReference.ToConnection("Connection2");

        ref1.GetHashCode().ShouldNotBe(ref2.GetHashCode());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void DefaultConstructorCreatesValidInstance()
    {
        var reference = new DataDestinationReference();

        reference.Kind.ShouldNotBeNull();
        reference.Name.ShouldBe(string.Empty);
        reference.WriteMode.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void OptionsCanBeSetAndRetrieved()
    {
        var reference = DataDestinationReference.ToConnection("Test");
        reference.Options = new Dictionary<string, object?> { { "option1", 123 }, { "option2", "value" } };

        reference.Options.ShouldContainKeyAndValue("option1", 123);
        reference.Options.ShouldContainKeyAndValue("option2", "value");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void GetHashCodeHandlesNullValues()
    {
        var reference = new DataDestinationReference
        {
            Kind = DataDestinationKinds.NotFound,
            Name = null!,
            ContainerPath = null
        };

        var hashCode = reference.GetHashCode();

        hashCode.ShouldNotBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void GetHashCodeHandlesNonNullValues()
    {
        var reference = DataDestinationReference.ToConnection("Test", "schema.table");

        var hashCode = reference.GetHashCode();

        hashCode.ShouldNotBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void CloneWithNonNullOptionsCreatesDeepCopy()
    {
        var original = DataDestinationReference.ToConnection("TestConnection", "schema.table");
        original.Options = new Dictionary<string, object?> { { "key1", "value1" } };

        var clone = original.Clone();

        clone.Options.ShouldNotBeNull();
        clone.Options.ShouldNotBeSameAs(original.Options);
        clone.Options.ShouldContainKeyAndValue("key1", "value1");
    }
}
