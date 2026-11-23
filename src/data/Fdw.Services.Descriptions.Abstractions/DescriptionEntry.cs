namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>One description as it is read: the key that names it, the names of the thing and its owner, and its text.</summary>
public sealed class DescriptionEntry
{
    /// <summary>Initializes a new instance of the <see cref="DescriptionEntry"/> class.</summary>
    /// <param name="key">What names the description.</param>
    /// <param name="thingName">The name of the thing described.</param>
    /// <param name="owner">The thing's owner, or nothing when the type has no owner.</param>
    /// <param name="text">The text.</param>
    public DescriptionEntry(DescriptionKey key, string thingName, DescriptionOwner? owner, string text)
    {
        Key = key;
        ThingName = thingName;
        Owner = owner;
        Text = text;
    }

    /// <summary>Gets what names the description.</summary>
    public DescriptionKey Key { get; }

    /// <summary>Gets the name of the thing described.</summary>
    public string ThingName { get; }

    /// <summary>Gets the thing's owner, or nothing when the type has no owner (a data set has none; a field's owner is its data set).</summary>
    public DescriptionOwner? Owner { get; }

    /// <summary>Gets the text.</summary>
    public string Text { get; }
}
