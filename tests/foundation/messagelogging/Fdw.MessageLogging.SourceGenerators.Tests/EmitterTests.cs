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
    public void AMethodWithAClientTemplateReturnsAClientMessageCarryingTheCallersText()
    {
        string result = CreateEmitter(CreateTestCompilation()).Emit([LogClassOf("{reason}")], CancellationToken.None);

        result.ShouldContain("return new global::Fdw.Messages.ClientMessage(");
        result.ShouldContain("var __clientText = ");
        result.ShouldContain("$\"{reason}\"");
        result.ShouldNotContain("global::Fdw.Messages.GenericMessage.Create(");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void AClientTemplateKeepsLiteralBracesAndAFormatAndBindsItsPlaceholdersToTheParameters()
    {
        string result = CreateEmitter(CreateTestCompilation()).Emit([LogClassOf("{{{Reason}}} after {count:N0} tries")], CancellationToken.None);

        result.ShouldContain("$\"{{{reason}}} after {count:N0} tries\"");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void AMethodWithoutAClientTemplateReturnsAGenericMessageAsBefore()
    {
        string result = CreateEmitter(CreateTestCompilation()).Emit([LogClassOf(clientTemplate: null)], CancellationToken.None);

        result.ShouldContain("return global::Fdw.Messages.GenericMessage.Create(");
        result.ShouldNotContain("ClientMessage");
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

    // One log class with one method over a reason and a count, with a template for the caller or none.
    private static LoggerMessageGenerator.LoggerClass LogClassOf(string? clientTemplate) => new()
    {
        Keyword = "class",
        Namespace = "TestNamespace",
        Name = "TestLog",
        ParentClass = null,
        Methods =
        {
            new LoggerMessageGenerator.LoggerMethod
            {
                Name = "Refused",
                UniqueName = "Refused",
                Message = "[Releases] Refused {operation} after {count} tries: {reason}",
                Level = 3, // Warning
                EventId = 20001,
                EventName = null,
                IsExtensionMethod = false,
                Modifiers = "public static",
                SkipEnabledCheck = false,
                ClientMessage = clientTemplate,
                TemplateParameters =
                {
                    new LoggerMessageGenerator.LoggerParameter { Name = "operation", CodeName = "operation", Type = "string" },
                    new LoggerMessageGenerator.LoggerParameter { Name = "count", CodeName = "count", Type = "int" },
                    new LoggerMessageGenerator.LoggerParameter { Name = "reason", CodeName = "reason", Type = "string" },
                },
            },
        },
    };

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
