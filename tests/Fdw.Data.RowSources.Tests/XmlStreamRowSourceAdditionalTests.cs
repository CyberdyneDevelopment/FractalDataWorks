using System.Text;
using System.Xml;
using Fdw.Data.Abstractions;
using Fdw.Data.RowSources.Xml.Abstractions;

namespace Fdw.Data.RowSources.Tests;

/// <summary>
/// Additional tests for XmlStreamRowSource covering boundary and edge case paths.
/// </summary>
public sealed class XmlStreamRowSourceAdditionalTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorWithNullXmlReaderThrows()
    {
        Should.Throw<ArgumentNullException>(() => new XmlStreamRowSource((XmlReader)null!));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void GetFieldNameReturnsEmptyForNegativeOrdinal()
    {
        var xml = """<root><row><id>1</id></row></root>""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        using var source = new XmlStreamRowSource(stream);

        source.GetFieldName(-1).ShouldBe(string.Empty);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void GetFieldNameReturnsEmptyForOutOfRangeOrdinal()
    {
        var xml = """<root><row><id>1</id></row></root>""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        using var source = new XmlStreamRowSource(stream);

        source.GetFieldName(999).ShouldBe(string.Empty);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void GetFieldOrdinalReturnsMinusOneForNull()
    {
        var xml = """<root><row><id>1</id></row></root>""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        using var source = new XmlStreamRowSource(stream);

        source.GetFieldOrdinal(null!).ShouldBe(-1);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void GetFieldOrdinalReturnsMinusOneForEmpty()
    {
        var xml = """<root><row><id>1</id></row></root>""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        using var source = new XmlStreamRowSource(stream);

        source.GetFieldOrdinal("").ShouldBe(-1);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task IsNullReturnsTrueForNegativeOrdinal()
    {
        var xml = """<root><row><id>1</id></row></root>""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var options = new XmlRowSourceOptions { RowElementName = "row", UseElementContent = true };
        using var source = new XmlStreamRowSource(stream, options);
        await source.Read(TestContext.Current.CancellationToken);

        source.IsNull(-1).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task IsNullReturnsTrueForOutOfRangeOrdinal()
    {
        var xml = """<root><row><id>1</id></row></root>""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var options = new XmlRowSourceOptions { RowElementName = "row", UseElementContent = true };
        using var source = new XmlStreamRowSource(stream, options);
        await source.Read(TestContext.Current.CancellationToken);

        source.IsNull(999).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetValueReturnsNullForNegativeOrdinal()
    {
        var xml = """<root><row><id>1</id></row></root>""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var options = new XmlRowSourceOptions { RowElementName = "row", UseElementContent = true };
        using var source = new XmlStreamRowSource(stream, options);
        await source.Read(TestContext.Current.CancellationToken);

        source.GetValue(-1).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetValueReturnsNullForOutOfRangeOrdinal()
    {
        var xml = """<root><row><id>1</id></row></root>""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var options = new XmlRowSourceOptions { RowElementName = "row", UseElementContent = true };
        using var source = new XmlStreamRowSource(stream, options);
        await source.Read(TestContext.Current.CancellationToken);

        source.GetValue(999).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetConvertedValueReturnsNullForNullValue()
    {
        var xml = """<root><row><id>1</id></row></root>""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var options = new XmlRowSourceOptions { RowElementName = "row", UseElementContent = true };
        using var source = new XmlStreamRowSource(stream, options);
        await source.Read(TestContext.Current.CancellationToken);

        var mockConverter = new Mock<IDataTypeConverter>();
        // Ordinal for a field that doesn't exist results in null GetValue
        source.GetConvertedValue(999, mockConverter.Object).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetConvertedValueCallsConverterForNonNullValue()
    {
        var xml = """<root><row><id>42</id></row></root>""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var options = new XmlRowSourceOptions { RowElementName = "row", UseElementContent = true };
        using var source = new XmlStreamRowSource(stream, options);
        await source.Read(TestContext.Current.CancellationToken);

        var mockConverter = new Mock<IDataTypeConverter>();
        mockConverter.Setup(c => c.ToClr("42")).Returns(42);
        var ordinal = source.GetFieldOrdinal("id");
        source.GetConvertedValue(ordinal, mockConverter.Object).ShouldBe(42);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task ReadWithAttributeOnlyRowsWorksCorrectly()
    {
        var xml = """
            <root>
                <item id="1" name="Alice" />
                <item id="2" name="Bob" />
            </root>
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var options = new XmlRowSourceOptions
        {
            RowElementName = "item",
            IncludeAttributes = true,
            UseElementContent = false
        };
        using var source = new XmlStreamRowSource(stream, options);

        (await source.Read(TestContext.Current.CancellationToken)).ShouldBeTrue();
        source.FieldCount.ShouldBe(2);
        source.GetValue(source.GetFieldOrdinal("id")).ShouldBe("1");
        source.GetValue(source.GetFieldOrdinal("name")).ShouldBe("Alice");

        (await source.Read(TestContext.Current.CancellationToken)).ShouldBeTrue();
        source.GetValue(source.GetFieldOrdinal("id")).ShouldBe("2");
        source.GetValue(source.GetFieldOrdinal("name")).ShouldBe("Bob");

        (await source.Read(TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task ResetIsNoOp()
    {
        var xml = """<root><row><id>1</id></row></root>""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var options = new XmlRowSourceOptions { RowElementName = "row", UseElementContent = true };
        using var source = new XmlStreamRowSource(stream, options);
        await source.Read(TestContext.Current.CancellationToken);

        source.Reset();
        source.HasCurrentRow.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void DoubleDisposeDoesNotThrow()
    {
        var xml = """<root><row><id>1</id></row></root>""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var source = new XmlStreamRowSource(stream);
        source.Dispose();
        source.Dispose();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task DisposeAsyncDisposesCorrectly()
    {
        var xml = """<root><row><id>1</id></row></root>""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var source = new XmlStreamRowSource(stream);
        await source.DisposeAsync();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void FieldCountIsZeroBeforeRead()
    {
        var xml = """<root><row><id>1</id></row></root>""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        using var source = new XmlStreamRowSource(stream);

        source.FieldCount.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task ReadWithoutRowElementNameUsesAnyElementAtDepthOne()
    {
        var xml = """
            <root>
                <item><name>Test</name></item>
            </root>
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var options = new XmlRowSourceOptions { UseElementContent = true };
        using var source = new XmlStreamRowSource(stream, options);

        // Without a RowElementName set, it should pick up any element at depth >= 1
        (await source.Read(TestContext.Current.CancellationToken)).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task ConstructorWithXmlReaderWorks()
    {
        var xml = """
            <root>
                <row><id>1</id></row>
            </root>
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null
        };
        var reader = XmlReader.Create(stream, settings);
        var options = new XmlRowSourceOptions { RowElementName = "row", UseElementContent = true };
        using var source = new XmlStreamRowSource(reader, options);

        (await source.Read(TestContext.Current.CancellationToken)).ShouldBeTrue();
        source.GetValue(source.GetFieldOrdinal("id")).ShouldBe("1");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task ReadWithAttributesDisabledSkipsAttributes()
    {
        var xml = """
            <root>
                <row id="1"><name>Alice</name></row>
            </root>
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var options = new XmlRowSourceOptions
        {
            RowElementName = "row",
            IncludeAttributes = false,
            UseElementContent = true
        };
        using var source = new XmlStreamRowSource(stream, options);
        await source.Read(TestContext.Current.CancellationToken);

        source.GetFieldOrdinal("id").ShouldBe(-1);
        source.GetValue(source.GetFieldOrdinal("name")).ShouldBe("Alice");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void CreateSecureSettingsAppliesOptions()
    {
        var options = new XmlRowSourceOptions
        {
            DtdProcessing = DtdProcessing.Ignore,
            MaxCharactersFromEntities = 5_000_000
        };

        var settings = options.CreateSecureSettings();

        settings.DtdProcessing.ShouldBe(DtdProcessing.Ignore);
        settings.MaxCharactersFromEntities.ShouldBe(5_000_000L);
        settings.IgnoreWhitespace.ShouldBeTrue();
        settings.IgnoreComments.ShouldBeTrue();
        settings.IgnoreProcessingInstructions.ShouldBeTrue();
    }
}
