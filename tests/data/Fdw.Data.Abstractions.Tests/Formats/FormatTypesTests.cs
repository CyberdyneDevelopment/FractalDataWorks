using Fdw.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.Abstractions.Tests.Formats;

public sealed class FormatTypesTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DelimitedFormatTypeIsRegistered()
    {
        // Act
        var delimited = FormatTypes.ByName("Delimited");

        // Assert
        delimited.ShouldNotBeNull();
        delimited.Name.ShouldBe("Delimited");
        delimited.CanonicalFileExtension.ShouldBe(".csv");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FixedWidthFormatTypeIsRegistered()
    {
        // Act
        var fixedWidth = FormatTypes.ByName("FixedWidth");

        // Assert
        fixedWidth.Name.ShouldBe("FixedWidth");
        fixedWidth.IsBinary.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CsvAndTsvAreFormatsOfTheDelimitedFamily()
    {
        // Act
        var csv = FormatTypes.ByName("Csv");
        var tsv = FormatTypes.ByName("Tsv");

        // Assert: delimited is a family; the named formats are what a file is, and each defines its separator.
        csv.Family.ShouldBe("Delimited");
        csv.Separator.ShouldBe(",");
        tsv.Family.ShouldBe("Delimited");
        tsv.Separator.ShouldBe("\t");
        FormatTypes.ByName("Delimited").Separator.ShouldBeNull();
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
    public void DelimitedExtensionPropertyWorks()
    {
        // Act
        var delimited = FormatTypes.Delimited;

        // Assert
        delimited.ShouldNotBeNull();
        delimited.Name.ShouldBe("Delimited");
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
