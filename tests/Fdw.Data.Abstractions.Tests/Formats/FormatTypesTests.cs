using Fdw.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.Abstractions.Tests.Formats;

public sealed class FormatTypesTests
{
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
