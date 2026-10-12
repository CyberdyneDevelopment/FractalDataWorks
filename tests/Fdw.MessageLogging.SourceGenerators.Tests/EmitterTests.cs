using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using System.Reflection;
using Xunit;

namespace Fdw.MessageLogging.Generators.Tests;

public sealed class EmitterTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void ConvertEndOfLineAndQuotationCharactersConvertsNewLine()
    {
        string input = "Line1\nLine2";

        string result = InvokeConvertEndOfLineAndQuotationCharacters(input);

        result.ShouldBe("Line1\\nLine2");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void ConvertEndOfLineAndQuotationCharactersConvertsCarriageReturn()
    {
        string input = "Line1\rLine2";

        string result = InvokeConvertEndOfLineAndQuotationCharacters(input);

        result.ShouldBe("Line1\\rLine2");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void ConvertEndOfLineAndQuotationCharactersConvertsQuotes()
    {
        string input = "Value \"quoted\"";

        string result = InvokeConvertEndOfLineAndQuotationCharacters(input);

        result.ShouldBe("Value \\\"quoted\\\"");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void ConvertEndOfLineAndQuotationCharactersConvertsAllSpecialCharacters()
    {
        string input = "Line1\r\nLine2 \"quoted\"";

        string result = InvokeConvertEndOfLineAndQuotationCharacters(input);

        result.ShouldBe("Line1\\r\\nLine2 \\\"quoted\\\"");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void ConvertEndOfLineAndQuotationCharactersHandlesEmptyString()
    {
        string input = string.Empty;

        string result = InvokeConvertEndOfLineAndQuotationCharacters(input);

        result.ShouldBe(string.Empty);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void ConvertEndOfLineAndQuotationCharactersHandlesNoSpecialCharacters()
    {
        string input = "Simple text without special characters";

        string result = InvokeConvertEndOfLineAndQuotationCharacters(input);

        result.ShouldBe(input);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void ConvertEndOfLineAndQuotationCharactersHandlesOnlySpecialCharacters()
    {
        string input = "\n\r\"";

        string result = InvokeConvertEndOfLineAndQuotationCharacters(input);

        result.ShouldBe("\\n\\r\\\"");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void ConvertEndOfLineAndQuotationCharactersHandlesMultipleNewLines()
    {
        string input = "Line1\n\n\nLine2";

        string result = InvokeConvertEndOfLineAndQuotationCharacters(input);

        result.ShouldBe("Line1\\n\\n\\nLine2");
    }

    // Note: ContainsSpecialSymbol uses ReadOnlySpan which cannot be tested via reflection.
    // These cases are covered by integration tests.

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void NormalizeSpecialSymbolKeepsAtSignParameters()
    {
        string input = "@parameter";

        string result = InvokeNormalizeSpecialSymbol(input);

        result.ShouldBe("@parameter");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void NormalizeSpecialSymbolAddsUnderscoreForNormalParameters()
    {
        string input = "parameter";

        string result = InvokeNormalizeSpecialSymbol(input);

        result.ShouldBe("_parameter");
    }

    // Note: RemoveSpecialSymbol uses ReadOnlySpan which cannot be tested via reflection.
    // These cases are covered by integration tests.

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void EmitterGeneratesCodeWithCorrectStructure()
    {
        var compilation = CreateTestCompilation();
        var emitter = CreateEmitter(compilation);

        var logClasses = new List<LoggerMessageGenerator.LoggerClass>
        {
            new()
            {
                Keyword = "class",
                Namespace = "TestNamespace",
                Name = "TestLog",
                ParentClass = null,
                Methods =
                {
                    new LoggerMessageGenerator.LoggerMethod
                    {
                        Name = "TestMethod",
                        UniqueName = "TestMethod",
                        Message = "Test message",
                        Level = 2, // Information
                        EventId = 1,
                        EventName = null,
                        IsExtensionMethod = false,
                        Modifiers = "public static",
                        SkipEnabledCheck = false
                    }
                }
            }
        };

        string result = emitter.Emit(logClasses, CancellationToken.None);

        result.ShouldNotBeNull();
        result.ShouldContain("namespace TestNamespace");
        result.ShouldContain("partial class TestLog");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void EmitterHandlesEmptyClassList()
    {
        var compilation = CreateTestCompilation();
        var emitter = CreateEmitter(compilation);

        var logClasses = new List<LoggerMessageGenerator.LoggerClass>();

        string result = emitter.Emit(logClasses, CancellationToken.None);

        result.ShouldNotBeNull();
        result.ShouldContain("// <auto-generated/>");
        result.ShouldContain("#nullable enable");
    }

    // Note: EmitterGeneratesEnumerationHelperWhenNeeded requires complex setup of LoggerClass/LoggerMethod objects.
    // The enumeration helper generation is tested via actual generator usage in the framework.

    private static string InvokeConvertEndOfLineAndQuotationCharacters(string input)
    {
        var method = typeof(LoggerMessageGenerator).GetMethod(
            "ConvertEndOfLineAndQuotationCharactersToEscapeForm",
            BindingFlags.NonPublic | BindingFlags.Static);

        method.ShouldNotBeNull();

        return (string)method.Invoke(null, new object[] { input })!;
    }

    private static string InvokeNormalizeSpecialSymbol(string input)
    {
        var method = typeof(LoggerMessageGenerator).GetMethod(
            "NormalizeSpecialSymbol",
            BindingFlags.NonPublic | BindingFlags.Static);

        method.ShouldNotBeNull();

        return (string)method.Invoke(null, new object[] { input })!;
    }

    private static CSharpCompilation CreateTestCompilation()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText("// Empty");
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location)
        };

        return CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static LoggerMessageGenerator.Emitter CreateEmitter(Compilation compilation)
    {
        var constructorInfo = typeof(LoggerMessageGenerator.Emitter).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
            null,
            new[] { typeof(Compilation) },
            null);

        constructorInfo.ShouldNotBeNull();

        return (LoggerMessageGenerator.Emitter)constructorInfo.Invoke(new object[] { compilation });
    }
}
