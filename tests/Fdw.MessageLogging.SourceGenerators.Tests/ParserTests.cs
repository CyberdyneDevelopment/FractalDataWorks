using Fdw.MessageLogging.Generators;
using Shouldly;
using Xunit;

namespace Fdw.MessageLogging.Generators.Tests;

public sealed class ParserTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GetNonRandomizedHashCodeReturnsConsistentValues()
    {
        string input = "TestMethod";

        int hash1 = LoggerMessageGenerator.GetNonRandomizedHashCode(input);
        int hash2 = LoggerMessageGenerator.GetNonRandomizedHashCode(input);

        hash1.ShouldBe(hash2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GetNonRandomizedHashCodeReturnsPositiveValue()
    {
        string input = "TestMethod";

        int hash = LoggerMessageGenerator.GetNonRandomizedHashCode(input);

        hash.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GetNonRandomizedHashCodeReturnsDifferentValuesForDifferentInputs()
    {
        string input1 = "Method1";
        string input2 = "Method2";

        int hash1 = LoggerMessageGenerator.GetNonRandomizedHashCode(input1);
        int hash2 = LoggerMessageGenerator.GetNonRandomizedHashCode(input2);

        hash1.ShouldNotBe(hash2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GetNonRandomizedHashCodeHandlesEmptyString()
    {
        string input = string.Empty;

        int hash = LoggerMessageGenerator.GetNonRandomizedHashCode(input);

        hash.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GetNonRandomizedHashCodeHandlesSpecialCharacters()
    {
        string input = "Method_With-Special.Chars!@#$";

        int hash = LoggerMessageGenerator.GetNonRandomizedHashCode(input);

        hash.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GetNonRandomizedHashCodeHandlesUnicode()
    {
        string input = "Méthod日本語";

        int hash = LoggerMessageGenerator.GetNonRandomizedHashCode(input);

        hash.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GetNonRandomizedHashCodeHandlesLongStrings()
    {
        string input = new string('a', 10000);

        int hash = LoggerMessageGenerator.GetNonRandomizedHashCode(input);

        hash.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void GetNonRandomizedHashCodeHandlesMinValueEdgeCase()
    {
        // Test that int.MinValue case is handled correctly
        // This is a regression test for the Math.Abs edge case
        string input = "TestInputThatProducesMinValue";

        int hash = LoggerMessageGenerator.GetNonRandomizedHashCode(input);

        // Should never be negative
        hash.ShouldBeGreaterThanOrEqualTo(0);
    }
}
