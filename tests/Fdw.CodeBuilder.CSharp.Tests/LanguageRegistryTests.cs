namespace Fdw.CodeBuilder.CSharp.Tests;

public class LanguageRegistryTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void Constructor_RegistersCSharpParser()
    {
        // Arrange & Act
        var registry = new LanguageRegistry();

        // Assert
        registry.IsSupported("csharp").ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void SupportedLanguages_IncludesCSharp()
    {
        // Arrange
        var registry = new LanguageRegistry();

        // Act
        var languages = registry.SupportedLanguages;

        // Assert
        languages.ShouldContain("csharp");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void SupportedLanguages_ReturnsSortedList()
    {
        // Arrange
        var registry = new LanguageRegistry();
        var mockParser = new Mock<Fdw.CodeBuilder.Abstractions.ICodeParser>();
        mockParser.SetupGet(p => p.Language).Returns("test");
        registry.RegisterParser("zebra", mockParser.Object);
        registry.RegisterParser("alpha", mockParser.Object);

        // Act
        var languages = registry.SupportedLanguages;

        // Assert
        languages[0].ShouldBe("alpha");
        languages[^1].ShouldBe("zebra");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void IsSupported_WithRegisteredLanguage_ReturnsTrue()
    {
        // Arrange
        var registry = new LanguageRegistry();

        // Act
        var result = registry.IsSupported("csharp");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void IsSupported_WithUnregisteredLanguage_ReturnsFalse()
    {
        // Arrange
        var registry = new LanguageRegistry();

        // Act
        var result = registry.IsSupported("python");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void IsSupported_WithNull_ReturnsFalse()
    {
        // Arrange
        var registry = new LanguageRegistry();

        // Act
        var result = registry.IsSupported(null!);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void IsSupported_WithEmptyString_ReturnsFalse()
    {
        // Arrange
        var registry = new LanguageRegistry();

        // Act
        var result = registry.IsSupported(string.Empty);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void IsSupported_IsCaseInsensitive()
    {
        // Arrange
        var registry = new LanguageRegistry();

        // Act
        var result = registry.IsSupported("CSHARP");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GetExtensions_ForCSharp_ReturnsCsAndCsx()
    {
        // Arrange
        var registry = new LanguageRegistry();

        // Act
        var extensions = registry.GetExtensions("csharp");

        // Assert
        extensions.ShouldContain(".cs");
        extensions.ShouldContain(".csx");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GetExtensions_WithNull_ReturnsEmpty()
    {
        // Arrange
        var registry = new LanguageRegistry();

        // Act
        var extensions = registry.GetExtensions(null!);

        // Assert
        extensions.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GetExtensions_WithUnregisteredLanguage_ReturnsEmpty()
    {
        // Arrange
        var registry = new LanguageRegistry();

        // Act
        var extensions = registry.GetExtensions("python");

        // Assert
        extensions.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GetLanguageByExtension_ForCs_ReturnsCSharp()
    {
        // Arrange
        var registry = new LanguageRegistry();

        // Act
        var language = registry.GetLanguageByExtension(".cs");

        // Assert
        language.ShouldBe("csharp");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GetLanguageByExtension_WithoutDot_AddsDotAndFinds()
    {
        // Arrange
        var registry = new LanguageRegistry();

        // Act
        var language = registry.GetLanguageByExtension("cs");

        // Assert
        language.ShouldBe("csharp");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GetLanguageByExtension_WithNull_ReturnsNull()
    {
        // Arrange
        var registry = new LanguageRegistry();

        // Act
        var language = registry.GetLanguageByExtension(null!);

        // Assert
        language.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GetLanguageByExtension_WithUnknownExtension_ReturnsNull()
    {
        // Arrange
        var registry = new LanguageRegistry();

        // Act
        var language = registry.GetLanguageByExtension(".py");

        // Assert
        language.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task GetParser_ForCSharp_ReturnsParser()
    {
        // Arrange
        var registry = new LanguageRegistry();

        // Act
        var parser = await registry.GetParser("csharp", TestContext.Current.CancellationToken);

        // Assert
        parser.ShouldNotBeNull();
        parser!.Language.ShouldBe("csharp");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task GetParser_WithNull_ReturnsNull()
    {
        // Arrange
        var registry = new LanguageRegistry();

        // Act
        var parser = await registry.GetParser(null!, TestContext.Current.CancellationToken);

        // Assert
        parser.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task GetParser_WithUnregisteredLanguage_ReturnsNull()
    {
        // Arrange
        var registry = new LanguageRegistry();

        // Act
        var parser = await registry.GetParser("python", TestContext.Current.CancellationToken);

        // Assert
        parser.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void RegisterParser_WithNewLanguage_AddsParser()
    {
        // Arrange
        var registry = new LanguageRegistry();
        var mockParser = new Mock<Fdw.CodeBuilder.Abstractions.ICodeParser>();
        mockParser.SetupGet(p => p.Language).Returns("test");

        // Act
        registry.RegisterParser("test", mockParser.Object, ".test");

        // Assert
        registry.IsSupported("test").ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void RegisterParser_WithNullLanguage_ThrowsArgumentException()
    {
        // Arrange
        var registry = new LanguageRegistry();
        var mockParser = new Mock<Fdw.CodeBuilder.Abstractions.ICodeParser>();

        // Act & Assert
        Should.Throw<ArgumentException>(() => registry.RegisterParser(null!, mockParser.Object));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void RegisterParser_WithEmptyLanguage_ThrowsArgumentException()
    {
        // Arrange
        var registry = new LanguageRegistry();
        var mockParser = new Mock<Fdw.CodeBuilder.Abstractions.ICodeParser>();

        // Act & Assert
        Should.Throw<ArgumentException>(() => registry.RegisterParser(string.Empty, mockParser.Object));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void RegisterParser_WithExtensions_RegistersExtensions()
    {
        // Arrange
        var registry = new LanguageRegistry();
        var mockParser = new Mock<Fdw.CodeBuilder.Abstractions.ICodeParser>();
        mockParser.SetupGet(p => p.Language).Returns("test");

        // Act
        registry.RegisterParser("test", mockParser.Object, ".test", ".tst");

        // Assert
        var extensions = registry.GetExtensions("test");
        extensions.ShouldContain(".test");
        extensions.ShouldContain(".tst");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void RegisterParser_WithExtensionWithoutDot_AddsDot()
    {
        // Arrange
        var registry = new LanguageRegistry();
        var mockParser = new Mock<Fdw.CodeBuilder.Abstractions.ICodeParser>();
        mockParser.SetupGet(p => p.Language).Returns("test");

        // Act
        registry.RegisterParser("test", mockParser.Object, "test");

        // Assert
        var language = registry.GetLanguageByExtension(".test");
        language.ShouldBe("test");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task RegisterParser_OverwritesExisting()
    {
        // Arrange
        var registry = new LanguageRegistry();
        var mockParser1 = new Mock<Fdw.CodeBuilder.Abstractions.ICodeParser>();
        mockParser1.SetupGet(p => p.Language).Returns("test");
        var mockParser2 = new Mock<Fdw.CodeBuilder.Abstractions.ICodeParser>();
        mockParser2.SetupGet(p => p.Language).Returns("test2");

        // Act
        registry.RegisterParser("test", mockParser1.Object);
        registry.RegisterParser("test", mockParser2.Object);

        // Assert
        var result = await registry.GetParser("test", TestContext.Current.CancellationToken);
        result!.Language.ShouldBe("test2");
    }
}
