using System;

namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>What names one description: its type, its sub-type, the thing it describes and its language. Nothing in it is defaulted.</summary>
public sealed class DescriptionKey
{
    /// <summary>Initializes a new instance of the <see cref="DescriptionKey"/> class.</summary>
    /// <param name="type">The name of a <see cref="DescriptionTypes"/> option.</param>
    /// <param name="subType">The name of a <see cref="DescriptionSubTypes"/> option.</param>
    /// <param name="thingId">The logical Id of the thing described.</param>
    /// <param name="language">The language tag of the text, such as <c>en-US</c>.</param>
    public DescriptionKey(string type, string subType, Guid thingId, string language)
    {
        Type = type;
        SubType = subType;
        ThingId = thingId;
        Language = language;
    }

    /// <summary>Gets the name of the description type.</summary>
    public string Type { get; }

    /// <summary>Gets the name of the description sub-type.</summary>
    public string SubType { get; }

    /// <summary>Gets the logical Id of the thing described.</summary>
    public Guid ThingId { get; }

    /// <summary>Gets the language tag of the text.</summary>
    public string Language { get; }

    /// <summary>Gets the key written as text, <c>Type/SubType/ThingId/Language</c>: the description as a whole, whatever number of parts it is stored in.</summary>
    /// <returns>The key as text.</returns>
    public string AsName() => string.Concat(Type, "/", SubType, "/", ThingId.ToString("D"), "/", Language);

    /// <summary>Gets the key of one part written as text, <c>Type/SubType/ThingId/Language/Ordinal</c>: the name of that part's row (FDW-6123).</summary>
    /// <param name="ordinal">The part's place in reading order, from 1.</param>
    /// <returns>The key of the part as text.</returns>
    public string AsName(int ordinal) => string.Concat(AsName(), "/", ordinal.ToString(System.Globalization.CultureInfo.InvariantCulture));
}
