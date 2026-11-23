using System;
using System.Collections.Generic;
using System.Linq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Descriptions.Tests;

/// <summary>
/// A description longer than the part length is split into parts, and the parts joined in Ordinal order are the text that was written, character for character (FDW-6123).
/// The part length is a number a host sets, so every case here states its own.
/// </summary>
[Trait("Category", "Descriptions")]
public sealed class DescriptionPartsTests
{
    [Fact]
    [Trait("Priority", "P0")]
    public void ATextWithinThePartLengthIsOnePartAndIsNotChanged()
    {
        DescriptionParts.Split("Orders placed", 13).ShouldBe(["Orders placed"]);
        DescriptionParts.Split("Orders placed", 4000).ShouldBe(["Orders placed"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ALongerTextIsCutBeforeAWhitespaceRunSoNoWordIsBrokenAndTheWhitespaceStartsTheNextPart()
    {
        var parts = DescriptionParts.Split("alpha beta gamma delta", 10);

        parts.ShouldBe(["alpha beta", " gamma", " delta"]);
        string.Concat(parts).ShouldBe("alpha beta gamma delta");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ARunLongerThanThePartLengthWithNoWhitespaceIsCutAtThePartLengthInTheMiddleOfTheRun()
    {
        var parts = DescriptionParts.Split("abcdefghijklmnopqrstuvwxyz", 10);

        parts.ShouldBe(["abcdefghij", "klmnopqrst", "uvwxyz"]);
        string.Concat(parts).ShouldBe("abcdefghijklmnopqrstuvwxyz");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AWhitespaceRunAsLongAsThePartLengthNeverMakesAPartOfWhitespaceAlone()
    {
        // The cut falls after "abcde", and what is left starts with 20 spaces, more than a part. A part of spaces alone is refused by the database, so the run is not cut: it is
        // taken with the first character after it, and that part is longer than the part length by the run.
        var text = "abcde" + new string(' ', 20) + "fgh";

        var parts = DescriptionParts.Split(text, 10);

        parts.ShouldBe(["abcde", new string(' ', 20) + "f", "gh"]);
        string.Concat(parts).ShouldBe(text);
        parts.ShouldNotContain(part => string.IsNullOrWhiteSpace(part));
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ACutNeverLeavesAPartEndingInWhitespaceBecauseTheWhitespaceGoesToTheNextPart()
    {
        // A cut at the part length would end the first part in spaces ("abcdefghi  " is 11 characters) ; the cut is before the run instead.
        var text = "abcdefghi   jklmnop";

        var parts = DescriptionParts.Split(text, 10);

        parts.ShouldBe(["abcdefghi", "   jklmnop"]);
        string.Concat(parts).ShouldBe(text);
        parts.Take(parts.Count - 1).All(part => !char.IsWhiteSpace(part[^1])).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void WhitespaceAtTheEndOfTheTextIsNeverCutIntoAPartOfItsOwnAndStaysWithTheTextBeforeIt()
    {
        // 13 characters with a part length of 10: a cut after "abcdefghij" would leave "   ", a part the database refuses. The tail stays on the part before it.
        var text = "abcdefghij   ";

        var parts = DescriptionParts.Split(text, 10);

        parts.ShouldBe([text]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ACutDoesNotFallBetweenTheTwoHalvesOfOneCharacter()
    {
        // "😀" is two UTF-16 characters; a part length of 3 would cut after the first half of the second one.
        var text = "ab😀😀cd";

        var parts = DescriptionParts.Split(text, 3);

        string.Concat(parts).ShouldBe(text);
        parts.All(part => part.Length == 0 || !char.IsHighSurrogate(part[^1])).ShouldBeTrue();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [InlineData("alpha beta gamma delta", 10)]
    [InlineData("abcdefghijklmnopqrstuvwxyz", 10)]
    [InlineData("abcdefghi   jklmnop", 10)]
    [InlineData("abcdefghij   ", 10)]
    [InlineData("  leading and trailing  ", 7)]
    [InlineData("line one\r\nline two\r\n\r\nline three", 9)]
    [InlineData("a b c d e f g h i j", 1)]
    [InlineData("x", 1)]
    public void ThePartsJoinedInOrdinalOrderAreTheTextWrittenCharacterForCharacter(string text, int partLength)
    {
        var parts = DescriptionParts.Split(text, partLength);

        DescriptionParts.Join(parts.Select((part, index) => (index + 1, part))).ShouldBe(text);
        DescriptionParts.Join(parts.Select((part, index) => (index + 1, part)).Reverse()).ShouldBe(text);
        parts.ShouldNotContain(part => string.IsNullOrWhiteSpace(part));
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ThePartsKeyIsTheDescriptionsKeyEndingInItsOrdinal()
    {
        var key = new Fdw.Services.Descriptions.Abstractions.DescriptionKey("DataSet", "Text", Guid.Parse("00000000-0000-0000-0000-0000000000a1"), "en-US");

        key.AsName().ShouldBe("DataSet/Text/00000000-0000-0000-0000-0000000000a1/en-US");
        key.AsName(1).ShouldBe("DataSet/Text/00000000-0000-0000-0000-0000000000a1/en-US/1");
        key.AsName(12).ShouldBe("DataSet/Text/00000000-0000-0000-0000-0000000000a1/en-US/12");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void APartLengthBelowOneAndATextOfNothingButWhitespaceAreRefused()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => DescriptionParts.Split("text", 0));
        Should.Throw<ArgumentException>(() => DescriptionParts.Split("   ", 10));
        Should.Throw<ArgumentException>(() => DescriptionParts.Split(string.Empty, 10));
    }

    [Theory]
    [Trait("Priority", "P0")]
    [InlineData(new[] { 1 }, true)]
    [InlineData(new[] { 3, 1, 2 }, true)]
    [InlineData(new[] { 1, 3 }, false)]
    [InlineData(new[] { 2, 3 }, false)]
    [InlineData(new[] { 1, 1, 2 }, false)]
    [InlineData(new int[0], false)]
    public void PartsAreWholeOnlyWhenTheirOrdinalsAreOneToNWithNoGapAndNoRepeat(int[] ordinals, bool whole)
        => DescriptionParts.AreContiguous(ordinals).ShouldBe(whole);
}
