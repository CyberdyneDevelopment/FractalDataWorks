using System.Diagnostics.CodeAnalysis;
using System.Text;
using Fdw.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.Abstractions.Tests.Formats;

public sealed class FormatBaseTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsId()
    {
        // Arrange & Act
        var format = new TestFormat(1, "TestFormat");

        // Assert
        format.Id.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsName()
    {
        // Arrange & Act
        var format = new TestFormat(1, "TestFormat");

        // Assert
        format.Name.ShouldBe("TestFormat");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CategoryDefaultsToFormat()
    {
        // Arrange & Act
        var format = new TestFormat(1, "TestFormat");

        // Assert
        format.Category.ShouldBe("Format");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CategoryCanBeCustomized()
    {
        // Arrange & Act
        var format = new TestFormat(2, "CustomFormat", category: "CustomCategory");

        // Assert
        format.Category.ShouldBe("CustomCategory");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FormatNameReturnsImplementationValue()
    {
        // Arrange
        var format = new TestFormat(1, "TestFormat");

        // Act
        var formatName = format.FormatName;

        // Assert
        formatName.ShouldBe("Test Format");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MimeTypeReturnsImplementationValue()
    {
        // Arrange
        var format = new TestFormat(1, "TestFormat");

        // Act
        var mimeType = format.MimeType;

        // Assert
        mimeType.ShouldBe("application/test");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IsBinaryReturnsImplementationValue()
    {
        // Arrange
        var format = new TestFormat(1, "TestFormat", isBinary: true);

        // Act
        var isBinary = format.IsBinary;

        // Assert
        isBinary.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SupportsStreamingReturnsImplementationValue()
    {
        // Arrange
        var format = new TestFormat(1, "TestFormat", supportsStreaming: true);

        // Act
        var supportsStreaming = format.SupportsStreaming;

        // Assert
        supportsStreaming.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task SerializeWritesToStream()
    {
        // Arrange
        var format = new TestFormat(1, "TestFormat");
        using var output = new MemoryStream();
        var data = "test data";
        var schema = new ContainerSchema { Fields = [] };

        // Act
        await format.Serialize(output, data, schema, TestContext.Current.CancellationToken);

        // Assert
        output.Position = 0;
        using var reader = new StreamReader(output);
        var result = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
        result.ShouldBe("test data");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task DeserializeReadsFromStream()
    {
        // Arrange
        var format = new TestFormat(1, "TestFormat");
        using var input = new MemoryStream(Encoding.UTF8.GetBytes("test data"));
        var schema = new ContainerSchema { Fields = [] };

        // Act
        var result = await format.Deserialize<string>(input, schema, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBe("test data");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ImplementsIFormat()
    {
        // Arrange
        var format = new TestFormat(1, "TestFormat");

        // Act & Assert
        format.ShouldBeAssignableTo<IFormat>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void InheritsFromTypeOptionBase()
    {
        // Arrange
        var format = new TestFormat(1, "TestFormat");

        // Act & Assert
        format.ShouldBeAssignableTo<FormatBase>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void BinaryFormatConfiguration()
    {
        // Arrange & Act
        var format = new TestFormat(
            2,
            "BinaryFormat",
            isBinary: true,
            supportsStreaming: false);

        // Assert
        format.IsBinary.ShouldBeTrue();
        format.SupportsStreaming.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TextFormatConfiguration()
    {
        // Arrange & Act
        var format = new TestFormat(
            3,
            "TextFormat",
            isBinary: false,
            supportsStreaming: true);

        // Assert
        format.IsBinary.ShouldBeFalse();
        format.SupportsStreaming.ShouldBeTrue();
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestFormat : FormatBase
    {
        private readonly bool _isBinary;
        private readonly bool _supportsStreaming;

        public TestFormat(
            int id,
            string name,
            bool isBinary = false,
            bool supportsStreaming = false,
            string? category = "Format")
            : base(id, name, category)
        {
            _isBinary = isBinary;
            _supportsStreaming = supportsStreaming;
        }

        public override string FormatName => "Test Format";
        public override string MimeType => "application/test";
        public override bool IsBinary => _isBinary;
        public override bool SupportsStreaming => _supportsStreaming;

        public override async Task Serialize(
            Stream output,
            object data,
            IContainerSchema schema,
            CancellationToken cancellationToken = default)
        {
            var bytes = Encoding.UTF8.GetBytes(data.ToString()!);
            await output.WriteAsync(bytes, cancellationToken);
        }

        public override async Task<T?> Deserialize<T>(
            Stream input,
            IContainerSchema schema,
            CancellationToken cancellationToken = default) where T : default
        {
            using var reader = new StreamReader(input);
            var result = await reader.ReadToEndAsync(cancellationToken);
            return (T?)(object?)result;
        }
    }
}
