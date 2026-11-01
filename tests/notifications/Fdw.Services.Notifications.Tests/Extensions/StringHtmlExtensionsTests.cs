using Fdw.Services.Notifications.Extensions;

namespace Fdw.Services.Notifications.Tests.Extensions;

/// <summary>
/// Tests for <see cref="StringHtmlExtensions.StripHtmlTags"/>.
/// </summary>
public sealed class StringHtmlExtensionsTests
{
    [Fact]
    [Trait("Priority", "P3")]
    [Trait("Category", "CoreFramework")]
    public void StripHtmlTagsReturnsEmptyStringForNull()
    {
        // Arrange
        string? html = null;

        // Act
        var result = html.StripHtmlTags();

        // Assert
        result.ShouldBe(string.Empty);
    }

    [Fact]
    [Trait("Priority", "P3")]
    [Trait("Category", "CoreFramework")]
    public void StripHtmlTagsReturnsEmptyStringForEmptyInput()
    {
        // Act
        var result = string.Empty.StripHtmlTags();

        // Assert
        result.ShouldBe(string.Empty);
    }

    [Fact]
    [Trait("Priority", "P3")]
    [Trait("Category", "CoreFramework")]
    public void StripHtmlTagsReturnsInputUnchangedWhenThereAreNoTags()
    {
        // Arrange
        const string plain = "Pipeline XYZ failed at 03:00";

        // Act
        var result = plain.StripHtmlTags();

        // Assert
        result.ShouldBe(plain);
    }

    [Fact]
    [Trait("Priority", "P3")]
    [Trait("Category", "CoreFramework")]
    public void StripHtmlTagsRemovesSimpleTags()
    {
        // Act
        var result = "<b>Pipeline failed</b>".StripHtmlTags();

        // Assert
        result.ShouldBe("Pipeline failed");
    }

    [Fact]
    [Trait("Priority", "P3")]
    [Trait("Category", "CoreFramework")]
    public void StripHtmlTagsRemovesMultipleAndNestedTags()
    {
        // Act
        var result = "<div><p>Pipeline <b>XYZ</b> failed</p></div>".StripHtmlTags();

        // Assert
        result.ShouldBe("Pipeline XYZ failed");
    }

    [Fact]
    [Trait("Priority", "P3")]
    [Trait("Category", "CoreFramework")]
    public void StripHtmlTagsRemovesTagsWithAttributes()
    {
        // Act
        var result = "<a href=\"https://example.com\" target=\"_blank\">link</a>".StripHtmlTags();

        // Assert
        result.ShouldBe("link");
    }
}
