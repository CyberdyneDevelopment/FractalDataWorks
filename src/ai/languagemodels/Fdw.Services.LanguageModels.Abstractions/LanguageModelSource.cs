
namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>A source a reply cites.</summary>
public sealed class LanguageModelSource
{
    /// <summary>Initializes a new instance of the <see cref="LanguageModelSource"/> class.</summary>
    /// <param name="title">The title of the source.</param>
    /// <param name="address">Where the source is.</param>
    /// <param name="citedText">The text the reply cites, or <see langword="null"/> when the provider does not give it.</param>
    public LanguageModelSource(string title, string address, string? citedText)
    {
        Title = title;
        Address = address;
        CitedText = citedText;
    }

    /// <summary>Gets the title of the source.</summary>
    public string Title { get; }

    /// <summary>Gets where the source is.</summary>
    public string Address { get; }

    /// <summary>Gets the text the reply cites, or <see langword="null"/> when the provider does not give it.</summary>
    public string? CitedText { get; }
}
