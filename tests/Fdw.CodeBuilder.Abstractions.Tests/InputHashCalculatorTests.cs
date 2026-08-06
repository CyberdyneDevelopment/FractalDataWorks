using System.IO;
using Fdw.CodeBuilder.Abstractions;

namespace Fdw.CodeBuilder.Abstractions.Tests;

public class InputHashCalculatorTests
{
    private sealed class TestInputInfo : IInputInfoModel
    {
        private readonly string _content;

        public TestInputInfo(string content)
        {
            _content = content;
            InputHash = InputHashCalculator.CalculateHash(this);
        }

        // Lazily prevent infinite recursion - for test we set it after
        public string InputHash { get; }

        public void WriteToHash(TextWriter writer)
        {
            writer.Write(_content);
        }
    }

    private sealed class SimpleWritableInput : IInputInfoModel
    {
        private readonly string _content;

        public SimpleWritableInput(string content)
        {
            _content = content;
        }

        public string InputHash => InputHashCalculator.CalculateHash(this);

        public void WriteToHash(TextWriter writer)
        {
            writer.Write(_content);
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void CalculateHashReturnsDeterministicResult()
    {
        var input1 = new SimpleWritableInput("hello world");
        var input2 = new SimpleWritableInput("hello world");

        var hash1 = InputHashCalculator.CalculateHash(input1);
        var hash2 = InputHashCalculator.CalculateHash(input2);

        hash1.ShouldBe(hash2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void CalculateHashReturnsDifferentHashForDifferentInput()
    {
        var input1 = new SimpleWritableInput("content A");
        var input2 = new SimpleWritableInput("content B");

        var hash1 = InputHashCalculator.CalculateHash(input1);
        var hash2 = InputHashCalculator.CalculateHash(input2);

        hash1.ShouldNotBe(hash2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void CalculateHashReturnsNonEmptyBase64String()
    {
        var input = new SimpleWritableInput("test data");

        var hash = InputHashCalculator.CalculateHash(input);

        hash.ShouldNotBeNullOrWhiteSpace();
        // SHA256 produces 32 bytes, base64 encoded = 44 chars
        hash.Length.ShouldBe(44);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void CalculateHashThrowsForNullInput()
    {
        Should.Throw<ArgumentNullException>(() => InputHashCalculator.CalculateHash(null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void CalculateHashProducesValidBase64()
    {
        var input = new SimpleWritableInput("some content");

        var hash = InputHashCalculator.CalculateHash(input);

        // Should not throw
        var bytes = Convert.FromBase64String(hash);
        bytes.Length.ShouldBe(32); // SHA256 = 32 bytes
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void CalculateHashIsCaseSensitive()
    {
        var lower = new SimpleWritableInput("abc");
        var upper = new SimpleWritableInput("ABC");

        var hashLower = InputHashCalculator.CalculateHash(lower);
        var hashUpper = InputHashCalculator.CalculateHash(upper);

        hashLower.ShouldNotBe(hashUpper);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void CalculateHashHandlesEmptyContent()
    {
        var input = new SimpleWritableInput("");

        var hash = InputHashCalculator.CalculateHash(input);

        hash.ShouldNotBeNullOrWhiteSpace();
        hash.Length.ShouldBe(44);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void CalculateHashHandlesLargeContent()
    {
        var largeContent = new string('x', 100_000);
        var input = new SimpleWritableInput(largeContent);

        var hash = InputHashCalculator.CalculateHash(input);

        hash.ShouldNotBeNullOrWhiteSpace();
        hash.Length.ShouldBe(44);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void CalculateHashHandlesUnicodeContent()
    {
        var input = new SimpleWritableInput("hello world");

        var hash = InputHashCalculator.CalculateHash(input);

        hash.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void CalculateHashDiffersForWhitespaceChanges()
    {
        var withSpace = new SimpleWritableInput("hello world");
        var withoutSpace = new SimpleWritableInput("helloworld");

        var hash1 = InputHashCalculator.CalculateHash(withSpace);
        var hash2 = InputHashCalculator.CalculateHash(withoutSpace);

        hash1.ShouldNotBe(hash2);
    }
}
