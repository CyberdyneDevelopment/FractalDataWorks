using Fdw.Data.Abstractions.Results;
using Fdw.Data.DataStores.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.Abstractions.Tests.DataStores;

public sealed class DataLocationTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorInitializesPropertiesCorrectly()
    {
        // Arrange & Act
        var location = new DataLocation("store1", "path1", "SqlTable");

        // Assert
        location.StoreId.ShouldBe("store1");
        location.PathId.ShouldBe("path1");
        location.ContainerType.ShouldBe("SqlTable");
        location.Parameters.ShouldBeEmpty();
        location.Metadata.ShouldBeEmpty();
        location.HasParameters.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorInitializesWithParameters()
    {
        // Arrange
        var parameters = new Dictionary<string, object> { { "key1", "value1" }, { "key2", 42 } };

        // Act
        var location = new DataLocation("store1", "path1", "SqlTable", parameters);

        // Assert
        location.Parameters.Count.ShouldBe(2);
        location.Parameters["key1"].ShouldBe("value1");
        location.Parameters["key2"].ShouldBe(42);
        location.HasParameters.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorInitializesWithMetadata()
    {
        // Arrange
        var metadata = new Dictionary<string, object> { { "meta1", "value1" } };

        // Act
        var location = new DataLocation("store1", "path1", "SqlTable", null, metadata);

        // Assert
        location.Metadata.Count.ShouldBe(1);
        location.Metadata["meta1"].ShouldBe("value1");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorThrowsWhenStoreIdIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new DataLocation(null!, "path1", "SqlTable"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorThrowsWhenPathIdIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new DataLocation("store1", null!, "SqlTable"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorThrowsWhenContainerTypeIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new DataLocation("store1", "path1", null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithParametersReturnsNewLocationWithCombinedParameters()
    {
        // Arrange
        var location = new DataLocation("store1", "path1", "SqlTable",
            new Dictionary<string, object> { { "key1", "value1" } });
        var additionalParameters = new Dictionary<string, object> { { "key2", "value2" } };

        // Act
        var newLocation = location.WithParameters(additionalParameters);

        // Assert
        newLocation.ShouldNotBeSameAs(location);
        newLocation.Parameters.Count.ShouldBe(2);
        newLocation.Parameters["key1"].ShouldBe("value1");
        newLocation.Parameters["key2"].ShouldBe("value2");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithParametersOverridesExistingParameters()
    {
        // Arrange
        var location = new DataLocation("store1", "path1", "SqlTable",
            new Dictionary<string, object> { { "key1", "original" } });
        var additionalParameters = new Dictionary<string, object> { { "key1", "updated" } };

        // Act
        var newLocation = location.WithParameters(additionalParameters);

        // Assert
        newLocation.Parameters["key1"].ShouldBe("updated");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithParametersReturnsSameInstanceWhenParametersAreEmpty()
    {
        // Arrange
        var location = new DataLocation("store1", "path1", "SqlTable");
        var emptyParameters = new Dictionary<string, object>();

        // Act
        var newLocation = location.WithParameters(emptyParameters);

        // Assert
        newLocation.ShouldBeSameAs(location);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithParametersReturnsSameInstanceWhenParametersAreNull()
    {
        // Arrange
        var location = new DataLocation("store1", "path1", "SqlTable");

        // Act
        var newLocation = location.WithParameters(null!);

        // Assert
        newLocation.ShouldBeSameAs(location);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithMetadataReturnsNewLocationWithCombinedMetadata()
    {
        // Arrange
        var location = new DataLocation("store1", "path1", "SqlTable", null,
            new Dictionary<string, object> { { "meta1", "value1" } });
        var additionalMetadata = new Dictionary<string, object> { { "meta2", "value2" } };

        // Act
        var newLocation = location.WithMetadata(additionalMetadata);

        // Assert
        newLocation.ShouldNotBeSameAs(location);
        newLocation.Metadata.Count.ShouldBe(2);
        newLocation.Metadata["meta1"].ShouldBe("value1");
        newLocation.Metadata["meta2"].ShouldBe("value2");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithMetadataOverridesExistingMetadata()
    {
        // Arrange
        var location = new DataLocation("store1", "path1", "SqlTable", null,
            new Dictionary<string, object> { { "meta1", "original" } });
        var additionalMetadata = new Dictionary<string, object> { { "meta1", "updated" } };

        // Act
        var newLocation = location.WithMetadata(additionalMetadata);

        // Assert
        newLocation.Metadata["meta1"].ShouldBe("updated");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithMetadataReturnsSameInstanceWhenMetadataIsEmpty()
    {
        // Arrange
        var location = new DataLocation("store1", "path1", "SqlTable");
        var emptyMetadata = new Dictionary<string, object>();

        // Act
        var newLocation = location.WithMetadata(emptyMetadata);

        // Assert
        newLocation.ShouldBeSameAs(location);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithMetadataReturnsSameInstanceWhenMetadataIsNull()
    {
        // Arrange
        var location = new DataLocation("store1", "path1", "SqlTable");

        // Act
        var newLocation = location.WithMetadata(null!);

        // Assert
        newLocation.ShouldBeSameAs(location);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithContainerTypeReturnsNewLocationWithUpdatedType()
    {
        // Arrange
        var location = new DataLocation("store1", "path1", "SqlTable");

        // Act
        var newLocation = location.WithContainerType("JsonFile");

        // Assert
        newLocation.ShouldNotBeSameAs(location);
        newLocation.ContainerType.ShouldBe("JsonFile");
        newLocation.StoreId.ShouldBe("store1");
        newLocation.PathId.ShouldBe("path1");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithContainerTypePreservesParametersAndMetadata()
    {
        // Arrange
        var location = new DataLocation("store1", "path1", "SqlTable",
            new Dictionary<string, object> { { "key1", "value1" } },
            new Dictionary<string, object> { { "meta1", "value1" } });

        // Act
        var newLocation = location.WithContainerType("JsonFile");

        // Assert
        newLocation.Parameters.Count.ShouldBe(1);
        newLocation.Metadata.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithContainerTypeThrowsWhenTypeIsNull()
    {
        // Arrange
        var location = new DataLocation("store1", "path1", "SqlTable");

        // Act & Assert
        Should.Throw<ArgumentException>(() => location.WithContainerType(null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithContainerTypeThrowsWhenTypeIsEmpty()
    {
        // Arrange
        var location = new DataLocation("store1", "path1", "SqlTable");

        // Act & Assert
        Should.Throw<ArgumentException>(() => location.WithContainerType(""));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithContainerTypeThrowsWhenTypeIsWhitespace()
    {
        // Arrange
        var location = new DataLocation("store1", "path1", "SqlTable");

        // Act & Assert
        Should.Throw<ArgumentException>(() => location.WithContainerType("   "));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ToCanonicalStringFormatsCorrectly()
    {
        // Arrange
        var location = new DataLocation("store1", "path1", "SqlTable");

        // Act
        var result = location.ToCanonicalString();

        // Assert
        result.ShouldBe("store1://path1@SqlTable");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ToCanonicalStringIncludesParameters()
    {
        // Arrange
        var parameters = new Dictionary<string, object> { { "key1", "value1" }, { "key2", "value2" } };
        var location = new DataLocation("store1", "path1", "SqlTable", parameters);

        // Act
        var result = location.ToCanonicalString();

        // Assert
        result.ShouldStartWith("store1://path1@SqlTable?");
        result.ShouldContain("key1=value1");
        result.ShouldContain("key2=value2");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ToCanonicalStringEscapesParameterValues()
    {
        // Arrange
        var parameters = new Dictionary<string, object> { { "key", "value with spaces" } };
        var location = new DataLocation("store1", "path1", "SqlTable", parameters);

        // Act
        var result = location.ToCanonicalString();

        // Assert
        result.ShouldContain("key=value%20with%20spaces");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ToStringReturnsCanonicalString()
    {
        // Arrange
        var location = new DataLocation("store1", "path1", "SqlTable");

        // Act
        var result = location.ToString();

        // Assert
        result.ShouldBe("store1://path1@SqlTable");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ParseReconstructsLocationCorrectly()
    {
        // Arrange
        var canonicalString = "store1://path1@SqlTable";

        // Act
        var result = DataLocation.Parse(canonicalString);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.StoreId.ShouldBe("store1");
        result.Value.PathId.ShouldBe("path1");
        result.Value.ContainerType.ShouldBe("SqlTable");
        result.Value.Parameters.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ParseReconstructsLocationWithParameters()
    {
        // Arrange
        var canonicalString = "store1://path1@SqlTable?key1=value1&key2=value2";

        // Act
        var result = DataLocation.Parse(canonicalString);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.StoreId.ShouldBe("store1");
        result.Value.PathId.ShouldBe("path1");
        result.Value.ContainerType.ShouldBe("SqlTable");
        result.Value.Parameters.Count.ShouldBe(2);
        result.Value.Parameters["key1"].ShouldBe("value1");
        result.Value.Parameters["key2"].ShouldBe("value2");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ParseUnescapesParameterValues()
    {
        // Arrange
        var canonicalString = "store1://path1@SqlTable?key=value%20with%20spaces";

        // Act
        var result = DataLocation.Parse(canonicalString);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Parameters["key"].ShouldBe("value with spaces");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ParseThrowsWhenStringIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => DataLocation.Parse(null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ParseThrowsWhenStringIsEmpty()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => DataLocation.Parse(""));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ParseThrowsWhenStringIsWhitespace()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => DataLocation.Parse("   "));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ParseThrowsWhenMissingSchemeSeparator()
    {
        // Act
        var result = DataLocation.Parse("store1path1@SqlTable");

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldBe(DataStoresResultCodes.InvalidCanonicalLocationFormat);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ParseThrowsWhenMissingContainerSeparator()
    {
        // Act
        var result = DataLocation.Parse("store1://path1SqlTable");

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldBe(DataStoresResultCodes.InvalidCanonicalLocationFormat);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ToCanonicalStringAndParseAreInverses()
    {
        // Arrange
        var original = new DataLocation("store1", "path1", "SqlTable",
            new Dictionary<string, object> { { "key1", "value1" } });

        // Act
        var canonicalString = original.ToCanonicalString();
        var result = DataLocation.Parse(canonicalString);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.StoreId.ShouldBe(original.StoreId);
        result.Value.PathId.ShouldBe(original.PathId);
        result.Value.ContainerType.ShouldBe(original.ContainerType);
        result.Value.Parameters.Count.ShouldBe(original.Parameters.Count);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EqualsReturnsTrueForIdenticalLocations()
    {
        // Arrange
        var location1 = new DataLocation("store1", "path1", "SqlTable");
        var location2 = new DataLocation("store1", "path1", "SqlTable");

        // Act & Assert
        location1.Equals(location2).ShouldBeTrue();
        (location1 == location2).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EqualsReturnsTrueForSameInstance()
    {
        // Arrange
        var location = new DataLocation("store1", "path1", "SqlTable");

        // Act & Assert
        location.Equals(location).ShouldBeTrue();
#pragma warning disable CS1718 // Comparison made to same variable - intentional test of == operator reflexivity
        (location == location).ShouldBeTrue();
#pragma warning restore CS1718
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EqualsReturnsFalseForDifferentStoreIds()
    {
        // Arrange
        var location1 = new DataLocation("store1", "path1", "SqlTable");
        var location2 = new DataLocation("store2", "path1", "SqlTable");

        // Act & Assert
        location1.Equals(location2).ShouldBeFalse();
        (location1 != location2).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EqualsReturnsFalseForDifferentPathIds()
    {
        // Arrange
        var location1 = new DataLocation("store1", "path1", "SqlTable");
        var location2 = new DataLocation("store1", "path2", "SqlTable");

        // Act & Assert
        location1.Equals(location2).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EqualsReturnsFalseForDifferentContainerTypes()
    {
        // Arrange
        var location1 = new DataLocation("store1", "path1", "SqlTable");
        var location2 = new DataLocation("store1", "path1", "JsonFile");

        // Act & Assert
        location1.Equals(location2).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EqualsReturnsFalseForDifferentParameters()
    {
        // Arrange
        var location1 = new DataLocation("store1", "path1", "SqlTable",
            new Dictionary<string, object> { { "key1", "value1" } });
        var location2 = new DataLocation("store1", "path1", "SqlTable",
            new Dictionary<string, object> { { "key1", "value2" } });

        // Act & Assert
        location1.Equals(location2).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EqualsReturnsFalseForNull()
    {
        // Arrange
        var location = new DataLocation("store1", "path1", "SqlTable");

        // Act & Assert
        location.Equals(null).ShouldBeFalse();
        (location == null).ShouldBeFalse();
        (null == location).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EqualsReturnsTrueForNullParameters()
    {
        // Arrange
        var location1 = new DataLocation("store1", "path1", "SqlTable");
        var location2 = new DataLocation("store1", "path1", "SqlTable");

        // Act & Assert
        location1.Equals(location2).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetHashCodeIsConsistentForSameInstance()
    {
        // Arrange
        var location = new DataLocation("store1", "path1", "SqlTable");

        // Act
        var hash1 = location.GetHashCode();
        var hash2 = location.GetHashCode();

        // Assert
        hash1.ShouldBe(hash2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetHashCodeIsSameForEqualLocations()
    {
        // Arrange
        var location1 = new DataLocation("store1", "path1", "SqlTable");
        var location2 = new DataLocation("store1", "path1", "SqlTable");

        // Act & Assert
        location1.GetHashCode().ShouldBe(location2.GetHashCode());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetHashCodeIncludesParameters()
    {
        // Arrange
        var location1 = new DataLocation("store1", "path1", "SqlTable");
        var location2 = new DataLocation("store1", "path1", "SqlTable",
            new Dictionary<string, object> { { "key1", "value1" } });

        // Act & Assert
        location1.GetHashCode().ShouldNotBe(location2.GetHashCode());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void HasParametersReturnsFalseForEmptyParameters()
    {
        // Arrange
        var location = new DataLocation("store1", "path1", "SqlTable");

        // Act & Assert
        location.HasParameters.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void HasParametersReturnsTrueWhenParametersExist()
    {
        // Arrange
        var location = new DataLocation("store1", "path1", "SqlTable",
            new Dictionary<string, object> { { "key1", "value1" } });

        // Act & Assert
        location.HasParameters.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ParametersAreReadOnly()
    {
        // Arrange
        var parameters = new Dictionary<string, object> { { "key1", "value1" } };
        var location = new DataLocation("store1", "path1", "SqlTable", parameters);

        // Act
        parameters["key1"] = "modified";

        // Assert
        location.Parameters["key1"].ShouldBe("value1");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MetadataAreReadOnly()
    {
        // Arrange
        var metadata = new Dictionary<string, object> { { "meta1", "value1" } };
        var location = new DataLocation("store1", "path1", "SqlTable", null, metadata);

        // Act
        metadata["meta1"] = "modified";

        // Assert
        location.Metadata["meta1"].ShouldBe("value1");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithParametersPreservesExistingMetadata()
    {
        // Arrange
        var location = new DataLocation("store1", "path1", "SqlTable",
            new Dictionary<string, object> { { "param1", "value1" } },
            new Dictionary<string, object> { { "meta1", "metavalue" } });
        var additionalParameters = new Dictionary<string, object> { { "param2", "value2" } };

        // Act
        var newLocation = location.WithParameters(additionalParameters);

        // Assert
        newLocation.Metadata.Count.ShouldBe(1);
        newLocation.Metadata["meta1"].ShouldBe("metavalue");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithMetadataPreservesExistingParameters()
    {
        // Arrange
        var location = new DataLocation("store1", "path1", "SqlTable",
            new Dictionary<string, object> { { "param1", "value1" } },
            new Dictionary<string, object> { { "meta1", "metavalue" } });
        var additionalMetadata = new Dictionary<string, object> { { "meta2", "metavalue2" } };

        // Act
        var newLocation = location.WithMetadata(additionalMetadata);

        // Assert
        newLocation.Parameters.Count.ShouldBe(1);
        newLocation.Parameters["param1"].ShouldBe("value1");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EqualsReturnsFalseWhenParameterCountsDiffer()
    {
        // Arrange
        var location1 = new DataLocation("store1", "path1", "SqlTable",
            new Dictionary<string, object> { { "key1", "value1" } });
        var location2 = new DataLocation("store1", "path1", "SqlTable",
            new Dictionary<string, object> { { "key1", "value1" }, { "key2", "value2" } });

        // Act & Assert
        location1.Equals(location2).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EqualsReturnsFalseWhenParameterKeyMissing()
    {
        // Arrange
        var location1 = new DataLocation("store1", "path1", "SqlTable",
            new Dictionary<string, object> { { "key1", "value1" } });
        var location2 = new DataLocation("store1", "path1", "SqlTable",
            new Dictionary<string, object> { { "key2", "value1" } });

        // Act & Assert
        location1.Equals(location2).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EqualsReturnsFalseWhenParameterValuesDiffer()
    {
        // Arrange
        var location1 = new DataLocation("store1", "path1", "SqlTable",
            new Dictionary<string, object> { { "key1", "value1" } });
        var location2 = new DataLocation("store1", "path1", "SqlTable",
            new Dictionary<string, object> { { "key1", "value2" } });

        // Act & Assert
        location1.Equals(location2).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ToCanonicalStringHandlesNullParameterValue()
    {
        // Arrange
        var parameters = new Dictionary<string, object> { { "key1", null! } };
        var location = new DataLocation("store1", "path1", "SqlTable", parameters);

        // Act
        var result = location.ToCanonicalString();

        // Assert
        result.ShouldContain("key1=");
        result.ShouldStartWith("store1://path1@SqlTable?");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void EqualsReturnsTrueWhenBothParameterValuesAreNull()
    {
        // Arrange
        var location1 = new DataLocation("store1", "path1", "SqlTable",
            new Dictionary<string, object> { { "key1", null! } });
        var location2 = new DataLocation("store1", "path1", "SqlTable",
            new Dictionary<string, object> { { "key1", null! } });

        // Act & Assert
        location1.Equals(location2).ShouldBeTrue();
    }
}
