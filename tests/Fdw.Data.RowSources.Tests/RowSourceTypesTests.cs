using Fdw.Data.RowSources;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Data.RowSources.DataReader;
using Fdw.Data.RowSources.Json;
using Fdw.Data.RowSources.Xml;
using Fdw.Data.RowSources.Http;

namespace Fdw.Data.RowSources.Tests;

/// <summary>
/// Tests for the RecordSourceTypes TypeCollection and RowSourceType instances.
/// Note: All RecordSourceTypes use RestrictToCurrentCompilation = true,
/// so they are NOT auto-registered in the test assembly. Tests verify
/// the type instances directly rather than through the frozen collection.
/// </summary>
public class RowSourceTypesTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ByNameReturnsNotFoundForUnknownType()
    {
        // Act
        var type = RecordSourceTypes.ByName("Unknown");

        // Assert
        type.ShouldNotBeNull();
        type.Name.ShouldBe("_Empty");
        type.ShouldBeSameAs(RecordSourceTypes.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ByNameReturnsNotFoundForNullName()
    {
        // Act
        var type = RecordSourceTypes.ByName(null);

        // Assert
        type.ShouldBeSameAs(RecordSourceTypes.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ByNameReturnsNotFoundForEmptyName()
    {
        // Act
        var type = RecordSourceTypes.ByName(string.Empty);

        // Assert
        type.ShouldBeSameAs(RecordSourceTypes.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void NotFoundHasExpectedSentinelValues()
    {
        // Act
        var notFound = RecordSourceTypes.NotFound;

        // Assert
        notFound.ShouldNotBeNull();
        notFound.Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DataReaderTypeInstanceHasCorrectProperties()
    {
        // Arrange - create instance directly (RestrictToCurrentCompilation prevents auto-registration)
        var type = new DataReaderRowSourceType();

        // Assert
        type.Name.ShouldBe("DataReader");
        type.SupportsSync.ShouldBeTrue();
        type.SupportsAsync.ShouldBeFalse();
        type.SupportsReset.ShouldBeFalse();
        type.TypicalAllocationsPerRow.ShouldBe(0);
        type.Format.ShouldBe("Tabular");
        type.ShouldBeAssignableTo<IRecordSourceType>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void JsonTypeInstanceHasCorrectProperties()
    {
        // Arrange
        var type = new JsonRowSourceType();

        // Assert
        type.Name.ShouldBe("Json");
        type.SupportsSync.ShouldBeTrue();
        type.SupportsAsync.ShouldBeTrue();
        type.SupportsReset.ShouldBeFalse();
        type.TypicalAllocationsPerRow.ShouldBe(1);
        type.Format.ShouldBe("Json");
        type.ShouldBeAssignableTo<IRecordSourceType>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void XmlTypeInstanceHasCorrectProperties()
    {
        // Arrange
        var type = new XmlRowSourceType();

        // Assert
        type.Name.ShouldBe("Xml");
        type.SupportsSync.ShouldBeTrue();
        type.SupportsAsync.ShouldBeTrue();
        type.SupportsReset.ShouldBeFalse();
        type.TypicalAllocationsPerRow.ShouldBe(1);
        type.Format.ShouldBe("Xml");
        type.ShouldBeAssignableTo<IRecordSourceType>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void HttpTypeInstanceHasCorrectProperties()
    {
        // Arrange
        var type = new HttpRowSourceType();

        // Assert
        type.Name.ShouldBe("Http");
        type.SupportsSync.ShouldBeFalse();
        type.SupportsAsync.ShouldBeTrue();
        type.SupportsReset.ShouldBeFalse();
        type.TypicalAllocationsPerRow.ShouldBe(1);
        type.Format.ShouldBe("Json");
        type.ShouldBeAssignableTo<IRecordSourceType>();
    }
}
