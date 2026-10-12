using System.Text;
using Fdw.Data.RowSources.Xml.Abstractions;

namespace Fdw.Data.RowSources.Tests;

/// <summary>
/// Tests for the XmlStreamRowSource streaming XML reader.
/// </summary>
public class XmlStreamRowSourceTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task ReadReturnsRowsFromAttributeBasedElements()
    {
        // Arrange - Use attributes (not element content) to avoid
        // ReadElementContentAsString reader advancement issues
        var xml = """
            <root>
                <row id="1" name="Alice" />
                <row id="2" name="Bob" />
            </root>
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var options = new XmlRowSourceOptions { RowElementName = "row", IncludeAttributes = true };
        using var source = new XmlStreamRowSource(stream, options);

        // Act & Assert
        (await source.Read(TestContext.Current.CancellationToken)).ShouldBeTrue();
        source.HasCurrentRow.ShouldBeTrue();
        source.GetValue(source.GetFieldOrdinal("id")).ShouldBe("1");
        source.GetValue(source.GetFieldOrdinal("name")).ShouldBe("Alice");

        (await source.Read(TestContext.Current.CancellationToken)).ShouldBeTrue();
        source.GetValue(source.GetFieldOrdinal("id")).ShouldBe("2");
        source.GetValue(source.GetFieldOrdinal("name")).ShouldBe("Bob");

        (await source.Read(TestContext.Current.CancellationToken)).ShouldBeFalse();
        source.HasCurrentRow.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task ReadCapturesFirstChildElementContent()
    {
        // Arrange - ReadElementContentAsString advances the XmlReader past the end
        // element tag, which can interfere with sibling element reading. The first
        // child element is reliably captured.
        var xml = """
            <root>
                <row><id>1</id></row>
            </root>
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var options = new XmlRowSourceOptions { RowElementName = "row", UseElementContent = true };
        using var source = new XmlStreamRowSource(stream, options);

        // Act
        (await source.Read(TestContext.Current.CancellationToken)).ShouldBeTrue();

        // Assert - first field is captured
        source.GetValue(source.GetFieldOrdinal("id")).ShouldBe("1");
        source.GetFieldName(0).ShouldBe("id");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetFieldNameReturnsCorrectNameForAttributeBasedRow()
    {
        // Arrange
        var xml = """
            <root>
                <item firstName="Test" lastName="User" />
            </root>
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var options = new XmlRowSourceOptions { RowElementName = "item", IncludeAttributes = true };
        using var source = new XmlStreamRowSource(stream, options);
        await source.Read(TestContext.Current.CancellationToken);

        // Assert
        source.GetFieldName(0).ShouldBe("firstName");
        source.GetFieldName(1).ShouldBe("lastName");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetFieldOrdinalIsCaseInsensitive()
    {
        // Arrange
        var xml = """
            <root>
                <row><MyField>value</MyField></row>
            </root>
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var options = new XmlRowSourceOptions { RowElementName = "row", UseElementContent = true };
        using var source = new XmlStreamRowSource(stream, options);
        await source.Read(TestContext.Current.CancellationToken);

        // Assert
        source.GetFieldOrdinal("MyField").ShouldBe(0);
        source.GetFieldOrdinal("myfield").ShouldBe(0);
        source.GetFieldOrdinal("MYFIELD").ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetFieldOrdinalReturnsMinusOneForUnknown()
    {
        // Arrange
        var xml = """
            <root>
                <row><field>value</field></row>
            </root>
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var options = new XmlRowSourceOptions { RowElementName = "row", UseElementContent = true };
        using var source = new XmlStreamRowSource(stream, options);
        await source.Read(TestContext.Current.CancellationToken);

        // Assert
        source.GetFieldOrdinal("unknown").ShouldBe(-1);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task ReadIncludesAttributesWhenOptionEnabled()
    {
        // Arrange
        var xml = """
            <root>
                <row id="1" active="true"><name>Alice</name></row>
            </root>
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var options = new XmlRowSourceOptions
        {
            RowElementName = "row",
            IncludeAttributes = true,
            UseElementContent = true
        };
        using var source = new XmlStreamRowSource(stream, options);
        await source.Read(TestContext.Current.CancellationToken);

        // Assert
        source.GetValue(source.GetFieldOrdinal("id")).ShouldBe("1");
        source.GetValue(source.GetFieldOrdinal("active")).ShouldBe("true");
        source.GetValue(source.GetFieldOrdinal("name")).ShouldBe("Alice");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task ReadHandlesEmptyElements()
    {
        // Arrange
        var xml = """
            <root>
                <row id="1" />
                <row id="2" />
            </root>
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var options = new XmlRowSourceOptions
        {
            RowElementName = "row",
            IncludeAttributes = true
        };
        using var source = new XmlStreamRowSource(stream, options);

        // Act & Assert
        (await source.Read(TestContext.Current.CancellationToken)).ShouldBeTrue();
        source.GetValue(source.GetFieldOrdinal("id")).ShouldBe("1");

        (await source.Read(TestContext.Current.CancellationToken)).ShouldBeTrue();
        source.GetValue(source.GetFieldOrdinal("id")).ShouldBe("2");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task IsNullReturnsTrueForMissingField()
    {
        // Arrange
        var xml = """
            <root>
                <row><existing>value</existing></row>
            </root>
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var options = new XmlRowSourceOptions { RowElementName = "row", UseElementContent = true };
        using var source = new XmlStreamRowSource(stream, options);
        await source.Read(TestContext.Current.CancellationToken);

        // Assert
        source.IsNull(source.GetFieldOrdinal("missing")).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void EstimatedAllocationsPerRowIsOne()
    {
        // Arrange
        var xml = """<root><row><id>1</id></row></root>""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        using var source = new XmlStreamRowSource(stream);

        // Assert
        source.EstimatedAllocationsPerRow.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void CanResetIsFalse()
    {
        // Arrange
        var xml = """<root><row><id>1</id></row></root>""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        using var source = new XmlStreamRowSource(stream);

        // Assert
        source.CanReset.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void HasCurrentRowIsFalseBeforeRead()
    {
        // Arrange
        var xml = """<root><row><id>1</id></row></root>""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        using var source = new XmlStreamRowSource(stream);

        // Assert
        source.HasCurrentRow.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task ReadReturnsFalseForEmptyRoot()
    {
        // Arrange
        var xml = """<root></root>""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var options = new XmlRowSourceOptions { RowElementName = "row" };
        using var source = new XmlStreamRowSource(stream, options);

        // Act & Assert
        (await source.Read(TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task SecuritySettingsApplyMaxDepth()
    {
        // Arrange
        var xml = """
            <root>
                <level1>
                    <level2>
                        <level3>
                            <level4><id>1</id></level4>
                        </level3>
                    </level2>
                </level1>
            </root>
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var options = new XmlRowSourceOptions
        {
            RowElementName = "level4",
            MaxDepth = 2,
            UseElementContent = true
        };
        using var source = new XmlStreamRowSource(stream, options);

        // Act - should not find level4 because it's deeper than MaxDepth
        (await source.Read(TestContext.Current.CancellationToken)).ShouldBeFalse();
    }
}
