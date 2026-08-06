using Fdw.CodeBuilder.CSharp.Parsing;

namespace Fdw.CodeBuilder.CSharp.Tests.Parsing;

public class RoslynCSharpParserTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void Language_ReturnsCSharp()
    {
        // Arrange
        var parser = new RoslynCSharpParser();

        // Act
        var language = parser.Language;

        // Assert
        language.ShouldBe("csharp");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task Parse_WithValidCode_ReturnsSuccess()
    {
        // Arrange
        var parser = new RoslynCSharpParser();
        var code = "class Test { }";

        // Act
        var result = await parser.Parse(code, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task Parse_WithNullCode_ReturnsFailure()
    {
        // Arrange
        var parser = new RoslynCSharpParser();

        // Act
        var result = await parser.Parse(null!, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage?.ShouldContain("null or empty");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task Parse_WithEmptyCode_ReturnsFailure()
    {
        // Arrange
        var parser = new RoslynCSharpParser();

        // Act
        var result = await parser.Parse(string.Empty, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task Parse_WithFilePath_SetsFilePath()
    {
        // Arrange
        var parser = new RoslynCSharpParser();
        var code = "class Test { }";
        var filePath = "Test.cs";

        // Act
        var result = await parser.Parse(code, filePath, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.FilePath.ShouldBe(filePath);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task Parse_WithSyntaxError_ParsesButHasErrors()
    {
        // Arrange
        var parser = new RoslynCSharpParser();
        var code = "class Test { invalid syntax }";

        // Act
        var result = await parser.Parse(code, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.HasErrors.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task Parse_ReturnsTreeWithCorrectLanguage()
    {
        // Arrange
        var parser = new RoslynCSharpParser();
        var code = "class Test { }";

        // Act
        var result = await parser.Parse(code, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Value!.Language.ShouldBe("csharp");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task Parse_ReturnsTreeWithSourceText()
    {
        // Arrange
        var parser = new RoslynCSharpParser();
        var code = "class Test { }";

        // Act
        var result = await parser.Parse(code, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Value!.SourceText.ShouldBe(code);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task Validate_WithValidCode_ReturnsSuccess()
    {
        // Arrange
        var parser = new RoslynCSharpParser();
        var code = "class Test { }";

        // Act
        var result = await parser.Validate(code, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task Validate_WithInvalidCode_ReturnsFailure()
    {
        // Arrange
        var parser = new RoslynCSharpParser();
        var code = "class Test { invalid }";

        // Act
        var result = await parser.Validate(code, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task Validate_WithNullCode_ReturnsFailure()
    {
        // Arrange
        var parser = new RoslynCSharpParser();

        // Act
        var result = await parser.Validate(null!, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task Validate_WithSyntaxErrors_IncludesErrorCount()
    {
        // Arrange
        var parser = new RoslynCSharpParser();
        var code = "class Test { invalid syntax here }";

        // Act
        var result = await parser.Validate(code, TestContext.Current.CancellationToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage?.ShouldContain("error");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task Parse_WithComplexCode_ParsesSuccessfully()
    {
        // Arrange
        var parser = new RoslynCSharpParser();
        var code = @"
            using System;

            namespace Test
            {
                public class MyClass
                {
                    public int Property { get; set; }

                    public void Method()
                    {
                        Console.WriteLine(""Test"");
                    }
                }
            }";

        // Act
        var result = await parser.Parse(code, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.HasErrors.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public async Task Parse_WithCancellation_ReturnsFailure()
    {
        // Arrange
        var parser = new RoslynCSharpParser();
        var code = "class Test { }";
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await parser.Parse(code, cancellationToken: cts.Token);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage?.ShouldContain("cancelled");
    }
}
