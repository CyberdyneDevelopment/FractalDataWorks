using Fdw.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.Abstractions.Tests.Formats;

public sealed class FormatTypesTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AllReturnsAllFormatTypes()
    {
        // Act
        var all = FormatTypes.All();

        // Assert
        all.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ByIdReturnsCorrectFormatType()
    {
        // Arrange
        var all = FormatTypes.All();
        var first = all.First();

        // Act
        var result = FormatTypes.ById(first.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(first.Id);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ByIdReturnsNullForUnknownId()
    {
        // Act
        var result = FormatTypes.ById(99999);

        // Assert
        result.ShouldBe(FormatTypes.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ByNameIsCaseSensitive()
    {
        // Arrange
        var all = FormatTypes.All();
        if (all.Count == 0) return; // Skip if no formats registered

        var first = all.First();

        // Act & Assert
        FormatTypes.ByName(first.Name).ShouldNotBeNull();
        FormatTypes.ByName(first.Name.ToLowerInvariant()).ShouldBe(FormatTypes.NotFound);
        FormatTypes.ByName(first.Name.ToUpperInvariant()).ShouldBe(FormatTypes.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void NotFoundReturnsEmptyInstance()
    {
        // Act
        var result = FormatTypes.NotFound;

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CsvFormatTypeIsRegistered()
    {
        // Act
        var csv = FormatTypes.ByName("Csv");

        // Assert
        csv.ShouldNotBeNull();
        csv.Name.ShouldBe("Csv");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void JsonFormatTypeIsRegistered()
    {
        // Act
        var json = FormatTypes.ByName("Json");

        // Assert
        json.ShouldNotBeNull();
        json.Name.ShouldBe("Json");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void XmlFormatTypeIsRegistered()
    {
        // Act
        var xml = FormatTypes.ByName("Xml");

        // Assert
        xml.ShouldNotBeNull();
        xml.Name.ShouldBe("Xml");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TabularFormatTypeIsRegistered()
    {
        // Act
        var tabular = FormatTypes.ByName("Tabular");

        // Assert
        tabular.ShouldNotBeNull();
        tabular.Name.ShouldBe("Tabular");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AllFormatTypesImplementIFormatType()
    {
        // Arrange
        var all = FormatTypes.All();

        // Act & Assert
        foreach (var formatType in all)
        {
            formatType.ShouldBeAssignableTo<IFormatType>();
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AllFormatTypesHaveUniqueIds()
    {
        // Arrange
        var all = FormatTypes.All();

        // Act
        var ids = all.Select(f => f.Id).ToHashSet();

        // Assert
        ids.Count.ShouldBe(all.Count);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AllFormatTypesHaveUniqueNames()
    {
        // Arrange
        var all = FormatTypes.All();

        // Act
        var names = all.Select(f => f.Name).ToHashSet();

        // Assert
        names.Count.ShouldBe(all.Count);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ByNameReturnsNotFoundForUnknownName()
    {
        // Act
        var result = FormatTypes.ByName("NonExistentFormat");

        // Assert
        result.ShouldBe(FormatTypes.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CsvExtensionPropertyWorks()
    {
        // Act
        var csv = FormatTypes.Csv;

        // Assert
        csv.ShouldNotBeNull();
        csv.Name.ShouldBe("Csv");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void JsonExtensionPropertyWorks()
    {
        // Act
        var json = FormatTypes.Json;

        // Assert
        json.ShouldNotBeNull();
        json.Name.ShouldBe("Json");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void XmlExtensionPropertyWorks()
    {
        // Act
        var xml = FormatTypes.Xml;

        // Assert
        xml.ShouldNotBeNull();
        xml.Name.ShouldBe("Xml");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TabularExtensionPropertyWorks()
    {
        // Act
        var tabular = FormatTypes.Tabular;

        // Assert
        tabular.ShouldNotBeNull();
        tabular.Name.ShouldBe("Tabular");
    }
}
