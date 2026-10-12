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
